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
    /// Defines and Actor reference.
    /// </summary>
    public class ActorRef
    {
        /// <summary>
        /// Gets the ActorRef for no actor.
        /// </summary>
        public static ActorRef None { get { return null; } }

        /// <summary>
        /// Gets the ActorRef for no sender actor.
        /// </summary>
        public static ActorRef NoSender { get { return null; } }

        /// <summary>
        /// Gets the ActorPath of this actor.
        /// </summary>
        public ActorPath Path { get { return this.Context.Path; } }

        /// <summary>
        /// Gets whether or not the referenced actor has been stopped.
        /// </summary>
        public bool Terminated { get { return Context.Terminated; } }

        /// <summary>
        /// Forwards the message and references the ActorContext's
        /// Sender as the sender.
        /// </summary>
        /// <param name="message">the object message</param>
        /// <param name="context">the ActorContext holding the Sender</param>
        public void Forward(object message, ActorContext context)
        {
            this.Tell(message, context.Sender);
        }

        /// <summary>
        /// Tells the backing Actor the message.
        /// </summary>
        /// <param name="message">the object message</param>
        public void Tell(object message)
        {
            this.Tell(message, ActorRef.NoSender);
        }

        /// <summary>
        /// Tells the backing Actor the message that is being
        /// sent by sender.
        /// </summary>
        /// <param name="message">the object message</param>
        /// <param name="sender">the ActorRef of the sender, which may be ActorRef.NoSender</param>
        public void Tell(object message, ActorRef sender)
        {
            if (!Context.Terminated)
            {
                Context.Enqueue(new Delivery(message, sender));
            }
            else
            {
                Context.System.DeadLetters.Tell(message, sender);
            }
        }

        /// <summary>
        /// Gets and sets my Context.
        /// </summary>
        internal ActorContext Context { get; set; }

        /// <summary>
        /// Constructs my default state with a context.
        /// </summary>
        /// <param name="context">the ActorContext</param>
        internal ActorRef(ActorContext context)
        {
            Context = context;
        }
    }
}
