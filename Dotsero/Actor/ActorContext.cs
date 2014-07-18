// Copyright 2012-2014 Vaughn Vernon
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Dotsero.Actor
{
    using Retlang.Channels;
    using Retlang.Fibers;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;

    /// <summary>
    /// Defines an Actor's context.
    /// </summary>
    public class ActorContext
    {
        /// <summary>
        /// Gets and sets (privately) my Children.
        /// </summary>
        public List<ActorRef> Children { get; private set; }

        /// <summary>
        /// Gets and sets (privately) my Parent.
        /// </summary>
        public ActorRef Parent { get; private set; }

        /// <summary>
        /// Gets and sets (privately) my Path.
        /// </summary>
        public ActorPath Path { get; private set; }

        /// <summary>
        /// Gets and sets (privately) my Props.
        /// </summary>
        public Props Props { get; private set; }

        /// <summary>
        /// Gets and sets (privately) my Self.
        /// </summary>
        public ActorRef Self { get; private set; }

        /// <summary>
        /// Gets and sets (privately) my Sender.
        /// </summary>
        public ActorRef Sender { get; internal set; }

        /// <summary>
        /// Gets and sets (privately) my System.
        /// </summary>
        public ActorSystem System { get; internal set; }

        /// <summary>
        /// Gets and sets (privately) my Terminated.
        /// </summary>
        private volatile bool _Terminated;
        public bool Terminated { get { return _Terminated; } }

        /// <summary>
        /// Creates a new Actor and returns its ActorRef. The new Actor
        /// is of type actorType and will be instantiated with the class
        /// arguments found in props.
        /// </summary>
        /// <param name="actorType">the Type of the Actor to create</param>
        /// <param name="props">the Props to pass as class arguments</param>
        /// <returns>ActorRef</returns>
        public ActorRef ActorOf(Type actorType, Props props)
        {
            return ActorOf(actorType, props, GenerateActorName());
        }

        /// <summary>
        /// Creates a new Actor and returns its ActorRef. The new Actor
        /// is of type actorType and will be instantiated with the class
        /// arguments found in props.
        /// </summary>
        /// <param name="actorType">the Type of the Actor to create</param>
        /// <param name="props">the Props to pass as class arguments</param>
        /// <param name="name">the string name of the actor</param>
        /// <returns>ActorRef</returns>
        public ActorRef ActorOf(Type actorType, Props props, string name)
        {
            // TODO: Full creations should be asynchronous

            ValidateName(name);

            Actor actor = ActorCreator.CreateWith(actorType, props);

            ActorContext context =
                new ActorContext(
                    System,
                    actorType,
                    actor,
                    name,
                    props,
                    this.Self,
                    this.Path);

            Children.Add(context.Self);

            context.System = System;

            return context.Self;
        }

        /// <summary>
        /// Answers the ActorSelection for a path. The path may be a simple
        /// actor name. If not found, answers the DeadLetters actor.
        /// NOTE: This currently does NOT support wildcards, which implies
        /// that currently only one actor may be returned in the selection.
        /// </summary>
        /// <param name="path">the string path to find</param>
        /// <returns>ActorSelection</returns>
        public ActorSelection ActorSelection(string path)
        {
            if (path.StartsWith("/"))
            {
                return System.ActorSelection(path);
            }

            return ActorSelectionFrom(false, path);
        }

        /// <summary>
        /// Causes the Actor to become a different kind of actor.
        /// This has little if any use with the current design
        /// because messages are delivered to individual OnReceive()
        /// methods.
        /// </summary>
        /// <param name="receive">the Receive to which messages are dispatched</param>
        /// <param name="discardOld">the bool that if set causes an Unbecome() before the Become()</param>
        public void Become(Receive receive, bool discardOld = true)
        {
            if (discardOld)
            {
                Unbecome();
            }

            Receive = receive;

            Receivers.Push(receive);

            Channel.ClearSubscribers();

            Channel.Subscribe(Fiber, GetOnReceiveDelegate());
        }

        /// <summary>
        /// Stops the Actor referenced by actor.
        /// </summary>
        /// <param name="actor">the ActorRef of the Actor to stop</param>
        public void Stop(ActorRef actor)
        {
            if (actor == Self)
            {
                Parent.Context.Stop(actor);
            }
            else
            {
                if (Children.Remove(actor))
                {
                    actor.Context.Stop();
                }
                else
                {
                    Sweep(actor, new List<ActorRef>(Children));
                }
            }
        }

        /// <summary>
        /// Pops the stack of the behaviors that the Actor may have
        /// and sets the OnReceiveDelegate to the previous.
        /// </summary>
        public void Unbecome()
        {
            if (Receivers.Count > 1)
            {
                Receivers.Pop();
            }

            Receive = Receivers.Pop();

            Channel.ClearSubscribers();

            Channel.Subscribe(Fiber, GetOnReceiveDelegate());
        }

        /// <summary>
        /// Constructs a new ActorContext.
        /// </summary>
        /// <param name="system">the ActorSystem within which the actor is created</param>
        /// <param name="actorType">the Type of the actor</param>
        /// <param name="actor">the Actor to create</param>
        /// <param name="name">the string name to give the Actor</param>
        /// <param name="props">the Props to pass individually as class arguments</param>
        /// <param name="parent">the ActorRef of the parent of the Actor being created</param>
        /// <param name="parentPath">the ActorPath of the Actor being created</param>
        /// <param name="suspended">the bool indicating whether the actor is being created as suspended</param>
        /// <param name="channel">the IChannel the actor will use</param>
        /// <param name="fiber">the IFiber the actor will use</param>
        internal ActorContext(
            ActorSystem system,
            Type actorType,
            Actor actor,
            string name,
            Props props,
            ActorRef parent,
            ActorPath parentPath,
            bool suspended,
            IChannel<Delivery> channel,
            IFiber fiber)
        {
            Actor = actor;
            Actor.InternalContext = this;
            Channel = channel;
            Fiber = fiber;
            Children = new List<ActorRef>(0);
            Parent = parent;
            Path = parentPath.WithName(name);
            Props = props;
            Receivers = new Stack<Receive>(1);
            Self = new ActorRef(this);
            Sender = ActorRef.NoSender;
            _Suspended = suspended;
            System = system;
            _Terminated = false;
            Type = actorType;

            Start();
        }

        /// <summary>
        /// Constructs a new ActorContext.
        /// </summary>
        /// <param name="system">the ActorSystem within which the actor is created</param>
        /// <param name="actor">the Actor to create</param>
        /// <param name="name">the string name to give the Actor</param>
        /// <param name="props">the Props to pass individually as class arguments</param>
        /// <param name="parent">the ActorRef of the parent of the Actor being created</param>
        /// <param name="parentPath">the ActorPath of the Actor being created</param>
        internal ActorContext(
            ActorSystem system,
            Type actorType,
            Actor actor,
            string name,
            Props props,
            ActorRef parent,
            ActorPath parentPath)
            : this(system, actorType, actor, name, props, parent,
                   parentPath, false, new Channel<Delivery>(), new PoolFiber())
        {
        }

        /// <summary>
        /// Gets and sets my Actor.
        /// </summary>
        internal Actor Actor { get; set; }

        /// <summary>
        /// Gets and sets (privately) my Channel.
        /// </summary>
        internal IChannel<Delivery> Channel { get; private set; }

        /// <summary>
        /// Gets and sets (privately) my Fiber.
        /// </summary>
        internal IFiber Fiber { get; private set; }

        /// <summary>
        /// Gets and sets my current Receiver.
        /// </summary>
        internal Receive Receive { get; set; }

        /// <summary>
        /// Gets and sets my current Suspended state.
        /// </summary>
        private volatile bool _Suspended;
        internal bool Suspended { get { return _Suspended; } }

        /// <summary>
        /// Gets and sets my current Type.
        /// </summary>
        internal Type Type { get; set; }

        /// <summary>
        /// Enqueues the delivery.
        /// </summary>
        /// <param name="delivery">the Delivery to enqueue</param>
        internal void Enqueue(Delivery delivery)
        {
            Channel.Publish(delivery);
        }

        /// <summary>
        /// Answers whether or not I have child actors.
        /// </summary>
        /// <returns>bool</returns>
        internal bool HasChildren()
        {
            return Children != null;
        }

        /// <summary>
        /// Stops the actor and then calls PostStop.
        /// </summary>
        internal void Stop()
        {
            _Suspended = true;

            Actor.StopChildren();

            _Terminated = true;

            StopConcurrency();

            Actor.PostStop();

            StopContext();
        }

        /// <summary>
        /// Gets and sets the Stack of Receivers.
        /// </summary>
        private Stack<Receive> Receivers { get; set; }

        /// <summary>
        /// My private nameIndex used to name actors that are
        /// not given a name by the requesting creator.
        /// </summary>
        private long nameIndex = 10000;

        /// <summary>
        /// Creates a new Actor and returns its ActorRef. The new Actor
        /// is of type actorType and will be instantiated with the class
        /// arguments found in props.
        /// </summary>
        /// <param name="actorType">the Type of the Actor to create</param>
        /// <param name="props">the Props to pass as class arguments</param>
        /// <param name="name">the string name of the actor</param>
        /// <param name="suspended"></param>
        /// <param name="channel"></param>
        /// <param name="fiber"></param>
        /// <returns>ActorRef</returns>
        private ActorRef ActorOf(
            Type actorType,
            Props props,
            string name,
            bool suspended,
            IChannel<Delivery> channel,
            IFiber fiber)
        {
            // TODO: Full creations should be asynchronous

            ValidateName(name);

            Actor actor = ActorCreator.CreateWith(actorType, props);

            ActorContext context =
                new ActorContext(
                    System,
                    actorType,
                    actor,
                    name,
                    props,
                    this.Self,
                    this.Path,
                    suspended,
                    channel,
                    fiber);

            Children.Add(context.Self);

            context.System = System;

            return context.Self;
        }

        /// <summary>
        /// Answers the ActorSelection for a path. The path may be a simple
        /// actor name. If not found, answers the DeadLetters actor.
        /// NOTE: This currently does NOT support wildcards, which implies
        /// that currently only one actor may be returned in the selection.
        /// </summary>
        /// <param name="root">the bool indicating whether this is a find from root</param>
        /// <param name="path">the string path elements to find</param>
        /// <returns>ActorSelection</returns>
        internal ActorSelection ActorSelectionFrom(bool root, string path)
        {
            return ActorSelectionFrom(
                root,
                path.Split(
                    "/".ToCharArray(),
                    StringSplitOptions.RemoveEmptyEntries),
                0);
        }

        /// <summary>
        /// Answers the ActorSelection for a path. The path may be a simple
        /// actor name. If not found, answers the DeadLetters actor.
        /// NOTE: This currently does NOT support wildcards, which implies
        /// that currently only one actor may be returned in the selection.
        /// </summary>
        /// <param name="root">the bool indicating whether this is a find from root</param>
        /// <param name="path">the string[] path elements to find</param>
        /// <param name="index">the int index of the current path element</param>
        /// <returns>ActorSelection</returns>
        internal ActorSelection ActorSelectionFrom(bool root, string[] path, int index)
        {
            if (root && path[0].Equals("user"))
            {
                ++index;
            }

            var children = new List<ActorRef>(Children);

            foreach (ActorRef child in children)
            {
                if (child.Path.Name.Equals(path[index]))
                {
                    if (index + 1 == path.Length)
                    {
                        return new ActorSelection(child);
                    }
                    else
                    {
                        return child.Context.ActorSelectionFrom(false, path, index + 1);
                    }
                }
            }

            return new ActorSelection(System.DeadLetters);
        }

        /// <summary>
        /// Suspend delivery of message until the
        /// actor is ready or until terminated.
        /// </summary>
        private void CheckSuspended()
        {
            while (_Suspended)
            {
                if (Terminated)
                {
                    break;
                }

                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Answers the generated base-64 string name to
        /// use as the actor's name.
        /// </summary>
        /// <returns></returns>
        private string GenerateActorName()
        {
            long value = Interlocked.Increment(ref nameIndex);

            byte[] bytes = BitConverter.GetBytes(value);

            return "$" + Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Answers the definition of the OnReceive delegate.
        /// </summary>
        /// <returns>Action<Delivery></returns>
        private Action<Delivery> GetOnReceiveDelegate()
        {
            Action<Delivery> onReceiveDelegate = delegate(Delivery delivery)
            {
                CheckSuspended();

                Sender = delivery.Sender;

                try
                {
                    ((dynamic)Receive).OnReceive((dynamic)delivery.Message);
                }
                catch (Exception reason)
                {
                    HandleFailure(reason, delivery.Message);
                }

                Sender = ActorRef.NoSender;
            };

            return onReceiveDelegate;
        }

        /// <summary>
        /// Handles an exceptional situation where this Actor
        /// fails and its parent must decide what to do. Note
        /// that the context of this method is the failed actor.
        /// </summary>
        /// <param name="reason">the Exception reason for the failure</param>
        /// <param name="message">the object message received when failure occurred</param>
        private void HandleFailure(Exception reason, object message)
        {
            Parent.Context.RecoverFromFor(reason, message, Self);
        }

        /// <summary>
        /// Escalate the failure to my parent. Note that the context
        /// of this method is the supervisor parent.
        /// </summary>
        /// <param name="reason">the Exception that caused the failure</param>
        private void RecoverByEscalation(Exception reason)
        {
            Parent.Context.RecoverFromFor(reason, null, Self);
        }

        /// <summary>
        /// Restart my child that failed. Note that the context
        /// of this method is the supervisor parent.
        /// </summary>
        /// <param name="reason">the Exception that caused the failure</param>
        /// <param name="message">the object message that was being processed during failure</param>
        /// <param name="child">the ActorRef of the failed child</param>
        private void RecoverByRestarting(Exception reason, object message, ActorRef child)
        {
            ActorContext childContext = child.Context;

            try
            {
                childContext._Suspended = true;

                childContext.Actor.PreRestart(reason, message);

                Children.Remove(child); // free name

                ActorRef restarted =
                    ActorOf(
                        childContext.Type,
                        childContext.Props,
                        childContext.Path.Name,
                        true,
                        childContext.Channel,
                        childContext.Fiber);

                restarted.Context._Suspended = false;

                childContext.StopContext();

                restarted.Context.Actor.PostRestart(reason);
            }
            finally
            {
                childContext._Suspended = false;
            }
        }

        /// <summary>
        /// Resume my child that failed. Note that the context
        /// of this method is the supervisor parent.
        /// </summary>
        /// <param name="reason">the Exception that caused the failure</param>
        /// <param name="child">the ActorRef of the failed child</param>
        private void RecoverByResuming(Exception reason, ActorRef child)
        {
            // ignore
        }

        /// <summary>
        /// Stop my child that failed. Note that the context
        /// of this method is the supervisor parent.
        /// </summary>
        /// <param name="e">the Exception that occurred in my child</param>
        private void RecoverByStopping(Exception reason, ActorRef child)
        {
            Stop(child);
        }

        /// <summary>
        /// Recover from a exceptional failure (reason) that occurred
        /// during a message by means of a supervisorStrategy. Note
        /// that the context of this method is the supervisor parent.
        /// </summary>
        /// <param name="reason">the Exception that caused the failure</param>
        /// <param name="message">the object message that was being processed during failure</param>
        /// <param name="child">the ActorRef of the failed child</param>
        private void RecoverFromFor(Exception reason, object message, ActorRef child)
        {
            SupervisorStrategy.Directive directive =
                Actor.InternalSupervisorStrategy.Decide(reason);

            switch (directive)
            {
                case SupervisorStrategy.Directive.Escalate:
                    RecoverByEscalation(reason);
                    break;
                case SupervisorStrategy.Directive.Restart:
                    RecoverByRestarting(reason, message, child);
                    break;
                case SupervisorStrategy.Directive.Resume:
                    RecoverByResuming(reason, child);
                    break;
                case SupervisorStrategy.Directive.Stop:
                    RecoverByStopping(reason, child);
                    break;
            }
        }

        /// <summary>
        /// Prepares the actor to be started, calls PreStart(),
        /// and then Starts the actor.
        /// </summary>
        private void Start()
        {
            Become(Actor, false);

            if (!Suspended)
            {
                Actor.PreStart();

                Fiber.Start();
            }
        }

        /// <summary>
        /// Stops my channel and fiber separately.
        /// </summary>
        private void StopConcurrency()
        {
            Channel.ClearSubscribers();
            Channel = null;

            Fiber.Dispose();
            Fiber = null;
        }

        /// <summary>
        /// Stops my channel and fiber separately.
        /// </summary>
        private void StopContext()
        {
            Actor.InternalContext = null;
            Actor = null;
            Children = null;
            Parent = null;
            Path = null;
            Props = null;
            Receivers = null;
            Self = null;
            Sender = ActorRef.NoSender;
            _Suspended = true;
            System = null;
            _Terminated = true;
            Type = null;
        }

        /// <summary>
        /// Stops actor at any level.
        /// </summary>
        /// <param name="actor">the ActorRef</param>
        /// <param name="children">the List<ActorRef> of my children</param>
        private void Sweep(ActorRef actor, List<ActorRef> children)
        {
            foreach (ActorRef child in Children)
            {
                if (child == actor)
                {
                    child.Context.Parent.Context.Stop(child);

                    return;
                }
            }

            foreach (ActorRef child in Children)
            {
                Sweep(actor, child.Context.Children);
            }
        }

        /// <summary>
        /// Validates the proposed actor name.
        /// </summary>
        /// <param name="name">the string name proposed for the actor</param>
        private void ValidateName(string name)
        {
            if (name == null || name.Trim().Length == 0)
            {
                throw new InvalidOperationException("The actor name is required.");
            }

            name = name.Trim();

            if (name.Substring(1).IndexOf("$") >= 0 || name.IndexOf("/") >= 0)
            {
                throw new InvalidOperationException("The actor name has invalid character(s).");
            }

            var children = new List<ActorRef>(Children);

            foreach (ActorRef child in children)
            {
                if (child.Path.Name.Equals(name))
                {
                    throw new InvalidOperationException("The actor name is not unique.");
                }
            }
        }
    }

    /// <summary>
    /// Defines the creator/instantiator of Actor subclasses.
    /// </summary>
    internal class ActorCreator
    {
        /// <summary>
        /// Creates the Actor of type actorType by passing the
        /// individual props as class arguments.
        /// </summary>
        /// <param name="actorType">the Type of the Actor subclass being created</param>
        /// <param name="props">the Props to pass as class arguments</param>
        /// <returns></returns>
        internal static Actor CreateWith(Type actorType, Props props)
        {
            int argumentCount = props.Count;
            int argumentIndex = 0;

            Type[] types = new Type[argumentCount];
            object[] arguments = new object[argumentCount];

            foreach (object argument in props.Values)
            {
                types[argumentIndex] = argument.GetType();

                arguments[argumentIndex] = argument;

                ++argumentIndex;
            }

            Actor actor = (Actor)
                Activator.CreateInstance(
                    actorType,
                    BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.CreateInstance
                    | BindingFlags.Instance,
                    null,
                    arguments,
                    null);

            return actor;
        }
    }

    /// <summary>
    /// Defines the means of delivering messages with a sender.
    /// </summary>
    public class Delivery
    {
        /// <summary>
        /// Constructs a new Delivery with a message and sender.
        /// </summary>
        /// <param name="message">the object message to deliver</param>
        /// <param name="sender">the ActorRef that sent the message</param>
        public Delivery(object message, ActorRef sender)
        {
            Message = message;
            Sender = sender;
        }

        /// <summary>
        /// Gets and sets (privately) my Message.
        /// </summary>
        public object Message { get; private set; }

        /// <summary>
        /// Gets and sets (privately) my Sender.
        /// </summary>
        public ActorRef Sender { get; private set; }
    }

    /// <summary>
    /// Defines the type required to serve as a message receiver.
    /// </summary>
    public interface Receive
    {
    }
}
