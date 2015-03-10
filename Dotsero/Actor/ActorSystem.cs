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
    using System;
    using System.Threading;

    /// <summary>
    /// Defines the ActorSystem.
    /// </summary>
    public class ActorSystem
    {
        /// <summary>
        /// Creates a new ActorSystem with a name.
        /// </summary>
        /// <param name="name">the string name of the system</param>
        /// <returns>ActorSystem</returns>
        public static ActorSystem Create(string name)
        {
            return new ActorSystem(name);
        }

        /// <summary>
        /// Gets and sets (privately) my Context.
        /// </summary>
        public ActorContext Context { get; private set; }

        /// <summary>
        /// Gets and sets (privately) my DeadLetters.
        /// </summary>
        public ActorRef DeadLetters { get; private set; }

        /// <summary>
        /// Gets and sets (privately) my Name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets and sets (privately) my Scheduler.
        /// </summary>
        public Scheduler Scheduler { get; private set; }

        /// <summary>
        /// Gets and set (privately) my StartTime.
        /// </summary>
        public DateTime StartTime { get; private set; }

        /// <summary>
        /// Gets and sets (privately) my SystemContext.
        /// </summary>
        public ActorContext SystemContext { get; private set; }

        /// <summary>
        /// Gets my UpTime as a TimeSpan.
        /// </summary>
        public TimeSpan UpTime { get { return DateTime.Now.Subtract(StartTime);  } }

        /// <summary>
        /// Creates a new Actor of type actorType and with class
        /// arguments found in props.
        /// </summary>
        /// <param name="actorType">the Type of the Actor to create</param>
        /// <param name="props">the Props to pass as individual class arguments</param>
        /// <returns>ActorRef</returns>
        public ActorRef ActorOf(Type actorType, Props props)
        {
            return Context.ActorOf(actorType, props);
        }

        /// <summary>
        /// Creates a new Actor of type actorType, with class
        /// arguments found in props, and named by the name.
        /// </summary>
        /// <param name="actorType">the Type of the Actor to create</param>
        /// <param name="props">the Props to pass as individual class arguments</param>
        /// <returns>ActorRef</returns>
        /// <param name="name">the string name of the actor to create</param>
        /// <returns>ActorRef</returns>
        public ActorRef ActorOf(Type actorType, Props props, string name)
        {
            return Context.ActorOf(actorType, props, name);
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
            return Context.ActorSelectionFrom(true, path);
        }

        /// <summary>
        /// Shut down the system.
        /// </summary>
        public void Shutdown()
        {
            Context.Actor.StopChildren();

            Context.Stop();

            SystemContext.Actor.StopChildren();

            SystemContext.Stop();
        }

        /// <summary>
        /// Stops the Actor referenced by actor.
        /// </summary>
        /// <param name="actor">the ActorRef ofthe Actor to stop</param>
        public void Stop(ActorRef actor)
        {
            Context.Stop(actor);
        }

        /// <summary>
        /// Constructs a new ActorSystem with a name.
        /// </summary>
        /// <param name="name">the string name of the system</param>
        private ActorSystem(string name)
        {
            // top level
            Actor system = new _System();
            var systemContext =
                new ActorContext(
                    this,
                    typeof(_System),
                    system,
                    ActorPath.RootName,
                    new Props(),
                    ActorRef.None,
                    new ActorPath(ActorPath.SystemName));

            // user guardian
            Actor userGuardian = new _UserGuardian();
            var userGuardianContext =
                new ActorContext(
                    this,
                    typeof(_UserGuardian),
                    userGuardian,
                    "user",
                    new Props(),
                    systemContext.Self,
                    systemContext.Path);

            // system guardian
            Actor systemGuardian = new _System();
            var systemGuardianContext =
                new ActorContext(
                    this,
                    typeof(_System),
                    systemGuardian,
                    "sys",
                    new Props(),
                    systemContext.Self,
                    systemContext.Path);

            Context = userGuardianContext;

            SystemContext = systemGuardianContext;

            DeadLetters =
                SystemContext.ActorOf(
                    typeof(DeadLetters),
                    Props.None,
                    "deadLetters");

            Name = name;

            Scheduler = new Scheduler();

            StartTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Defines the system scheduler.
    /// </summary>
    public class Scheduler
    {
        /// <summary>
        /// Schedules a recurring timer event.
        /// </summary>
        /// <param name="delay">the long number of milliseconds before the event is rasied</param>
        /// <param name="receiver">the ActorRef of the actor to receive notification</param>
        /// <param name="message">the object message to send to the receiver when the event occurs</param>
        /// <returns>Cancellable</returns>
        public Cancellable Schedule(
            long initialDelay,
            long frequency,
            ActorRef receiver,
            object message)
        {
            if (initialDelay < 0)
            {
                throw new NotSupportedException("The initial delay must be zero or greater.");
            }

            if (frequency < 1)
            {
                throw new NotSupportedException("The frequency must be greater than zero.");
            }

            if (receiver == ActorRef.None || message == null)
            {
                throw new NotSupportedException("Must provide a receiver and message.");
            }

            return new ScheduledTimerEvent(initialDelay, frequency, receiver, message);
        }

        /// <summary>
        /// Schedules a single timer event.
        /// </summary>
        /// <param name="delay">the long number of milliseconds before the event is rasied</param>
        /// <param name="receiver">the ActorRef of the actor to receive notification</param>
        /// <param name="message">the object message to send to the receiver when the event occurs</param>
        /// <returns>Cancellable</returns>
        public Cancellable ScheduleOnce(
            long delay,
            ActorRef receiver,
            object message)
        {
            if (delay < 1)
            {
                throw new NotSupportedException("Delay must be greater than zero.");
            }

            if (receiver == ActorRef.None || message == null)
            {
                throw new NotSupportedException("Must provide a receiver and message.");
            }

            return new ScheduledTimerEvent(delay, 0, receiver, message);
        }

        /// <summary>
        /// Constructs the system Scheduler.
        /// </summary>
        internal Scheduler() { }
    }

    /// <summary>
    /// Defines a ScheduledTimerEvent used by the system Scheduler.
    /// </summary>
    internal class ScheduledTimerEvent : Cancellable
    {
        private long frequency;
        private object message;
        private ActorRef receiver;
        private Timer timer;

        public ScheduledTimerEvent(
            long delay,
            long frequency,
            ActorRef receiver,
            object message)
        {
            this.frequency = frequency;
            this.receiver = receiver;
            this.message = message;

            timer = new Timer(
                (TimerCallback)this.OnTimedEvent,
                null,
                delay,
                frequency == 0 ? -1 : frequency);
        }

        /// <summary>
        /// Implemented Cancellable Cancel.
        /// </summary>
        public void Cancel()
        {
            timer.Change(Timeout.Infinite, 0);

            timer.Dispose();

            timer = null;
        }

        /// <summary>
        /// Implemented Cancellable Cancelled.
        /// </summary>
        public bool Cancelled
        {
            get
            {
                return timer == null;
            }
        }

        /// <summary>
        /// Called when the timer elapses.
        /// </summary>
        /// <param name="stateInfo">the event infor</param>
        public void OnTimedEvent(object stateInfo)
        {
            if (frequency == 0)
            {
                Cancel();
            }
            
            receiver.Tell(message);
        }
    }

    /// <summary>
    /// Defines the interface used to cancel some event.
    /// </summary>
    public interface Cancellable
    {
        /// <summary>
        /// Cancels the event.
        /// </summary>
        void Cancel();

        /// <summary>
        /// Indicates whether the event has been canceled.
        /// </summary>
        bool Cancelled { get; }
    }

    /// <summary>
    /// Defines the dead letters actor.
    /// There should be only one per ActorSystem.
    /// </summary>
    internal class DeadLetters : Actor
    {
        /// <summary>
        /// Constructs a new dead letters actor.
        /// </summary>
        internal DeadLetters()
        {
        }

        /// <summary>
        /// Receives a message and logs it.
        /// </summary>
        /// <param name="message"></param>
        public void OnReceive(object message)
        {
            Console.Out.WriteLine("Dead Letter: INFO: " + message);
        }
    }

    /// <summary>
    /// Defines the user guardian pseudo actor.
    /// There should be only one per ActorSystem.
    /// </summary>
    internal class _UserGuardian : Actor
    {
        /// <summary>
        /// Constructs a new guardian pseudo actor.
        /// </summary>
        internal _UserGuardian()
        {
        }

        public void OnReceive(object message)
        {
            Console.Out.WriteLine("User Guardian: LOGGING: " + message);
        }
    }

    /// <summary>
    /// Defines the system root pseudo actor.
    /// There should be only one per ActorSystem.
    /// </summary>
    internal class _System : Actor
    {
        /// <summary>
        /// Constructs a new guardian pseudo actor.
        /// </summary>
        internal _System()
        {
        }

        public void OnReceive(object message)
        {
            Console.Out.WriteLine("System Guardian: LOGGING: " + message);
        }
    }
}
