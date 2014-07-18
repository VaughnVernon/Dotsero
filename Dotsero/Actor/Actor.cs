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
    using System.Collections.Generic;

    /// <summary>
    /// Defines an actor base class.
    /// </summary>
    public abstract class Actor : Receive
    {
        /// <summary>
        /// Called before the actor is started. (Life cycle.)
        /// </summary>
        public virtual void PreStart()
        {
        }

        /// <summary>
        /// Called after the actor is stopped. (Life cycle.)
        /// </summary>
        public virtual void PostStop()
        {
        }

        /// <summary>
        /// Called before the actor is restarted.
        /// </summary>
        /// <param name="reason">the Exception that caused the restart</param>
        /// <param name="message">the object message or null</param>
        public virtual void PreRestart(Exception reason, object message)
        {
            StopChildren();

            PostStop();
        }

        /// <summary>
        /// Called after the actor has been restarted, but
        /// before it is started.
        /// </summary>
        /// <param name="reason">the Exception that caused the restart</param>
        public virtual void PostRestart(Exception reason)
        {
            PreStart();
        }

        /// <summary>
        /// Gets and sets my internal Context.
        /// </summary>
        internal ActorContext InternalContext
        {
            get { return Context; }
            set { Context = value; }
        }

        /// <summary>
        /// Gets and sets my InternalSupervisorStrategy.
        /// </summary>
        internal SupervisorStrategy InternalSupervisorStrategy { get { return SupervisorStrategy; } }

        /// <summary>
        /// Stop all my child Actors.
        /// </summary>
        internal void StopChildren()
        {
            // prevents concurrent modification exception
            List<ActorRef> childrenToStop =
                new List<ActorRef>(Context.Children);

            foreach (ActorRef child in childrenToStop)
            {
                Context.Stop(child);
            }
        }

        /// <summary>
        /// Constructs a new Actor.
        /// </summary>
        protected Actor()
            : base()
        {
            SupervisorStrategy = SupervisorStrategy.Default;
        }

        /// <summary>
        /// Gets whether or not this Actor can reply to the current message.
        /// The Actor can only reply if the sender of the current message
        /// was provided as a Tell() argument.
        /// </summary>
        protected bool CanReply { get { return Sender != null; } }

        /// <summary>
        /// Gets and sets my Context.
        /// </summary>
        protected ActorContext Context { get; set; }

        /// <summary>
        /// Gets my Self as an ActorRef.
        /// </summary>
        protected ActorRef Self { get { return Context.Self; } }

        /// <summary>
        /// Gets the sender of my current message.
        /// </summary>
        protected ActorRef Sender { get { return Context.Sender; } set { Context.Sender = value; } }

        /// <summary>
        /// Gets and sets my SupervisorStrategy.
        /// </summary>
        protected SupervisorStrategy SupervisorStrategy { get; set; }
    }

    /// <summary>
    /// Defines the supervisor strategy.
    /// </summary>
    public class SupervisorStrategy
    {
        /// <summary>
        /// Defines how failures can be handled.
        /// </summary>
        public enum Directive
        {
            Escalate, Restart, Resume, Stop
        }

        /// <summary>
        /// Defines the two types of strategies.
        /// </summary>
        protected enum StrategyType
        {
            AllForOne, OneForOne
        }

        /// <summary>
        /// The system default SupervisorStrategy.
        /// </summary>
        public static SupervisorStrategy Default = new OneForOneStrategy();

        /// <summary>
        /// Answers the Directive after deciding how the exception should be handled.
        /// Can be overridden by concreate Actor subclass to change strategy.
        /// </summary>
        /// <param name="e">the Exception that occurred</param>
        /// <returns>SupervisorStrategy.Directive</returns>
        public virtual SupervisorStrategy.Directive Decide(Exception e)
        {
            Type eType = e.GetType();

            if (eType == typeof(ActorInitializationException))
            {
                return SupervisorStrategy.Directive.Stop;
            }
            else if (eType == typeof(ActorKilledException))
            {
                return SupervisorStrategy.Directive.Stop;
            }
            else if (eType == typeof(InvalidOperationException))
            {
                return SupervisorStrategy.Directive.Restart;
            }
            else if (eType == typeof(NotSupportedException))
            {
                return SupervisorStrategy.Directive.Restart;
            }
            else if (eType == typeof(Exception))
            {
                return SupervisorStrategy.Directive.Restart;
            }
            else
            {
                return SupervisorStrategy.Directive.Escalate;
            }
        }

        /// <summary>
        /// Constructs a new type of strategy.
        /// </summary>
        /// <param name="type"></param>
        protected SupervisorStrategy(StrategyType type)
        {
            Type = type;
        }

        /// <summary>
        /// Gets and sets my strategy type.
        /// </summary>
        private StrategyType Type { get; set; }
    }

    /// <summary>
    /// Defines the all-for-one supervisor strategy, which
    /// means that if a child fails, the strategy will apply
    /// to all siblings, and below.
    /// </summary>
    public sealed class AllForOneStrategy : SupervisorStrategy
    {
        public AllForOneStrategy()
            : base(StrategyType.AllForOne)
        {
        }
    }

    /// <summary>
    /// Defines the one-for-one supervisor strategy, which
    /// means that the strategy will be applied only to
    /// the one failed child.
    /// </summary>
    public sealed class OneForOneStrategy : SupervisorStrategy
    {
        public OneForOneStrategy()
            : base(StrategyType.OneForOne)
        {
        }
    }

    /// <summary>
    /// Defines the ActorInitializationException.
    /// </summary>
    public class ActorInitializationException : Exception
    {
        public ActorInitializationException() :base() { }
        public ActorInitializationException(string message) :base(message) { }
        public ActorInitializationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Defines the ActorInitializationException.
    /// </summary>
    public class ActorKilledException : Exception
    {
        public ActorKilledException() :base() { }
        public ActorKilledException(string message) :base(message) { }
        public ActorKilledException(string message, Exception innerException) : base(message, innerException) { }
    }
}
