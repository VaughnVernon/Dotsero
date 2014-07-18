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
            ActorRef newActor = Context.ActorOf(actorType, props, name);

            newActor.Context.System = this;

            return newActor;
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

            StartTime = DateTime.Now;
        }
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
