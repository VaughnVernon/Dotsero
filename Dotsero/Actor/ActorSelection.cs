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
    using System.Collections.Generic;

    public class ActorSelection
    {
        /// <summary>
        /// Tells the backing Actor(s) the message.
        /// </summary>
        /// <param name="message">the object message</param>
        public void Tell(object message)
        {
            Tell(message, ActorRef.NoSender);
        }

        /// <summary>
        /// Tells the backing Actor(s) the message.
        /// </summary>
        /// <param name="message">the object message</param>
        /// <param name="sender">the ActorRef sender</param>
        public void Tell(object message, ActorRef sender)
        {
            foreach (ActorRef actor in Selections)
            {
                actor.Tell(message, ActorRef.NoSender);
            }
        }

        internal ActorSelection(ActorRef actor)
        {
            Selections = new List<ActorRef>();

            Selections.Add(actor);
        }

        internal ActorSelection(List<ActorRef> actors)
        {
            Selections = new List<ActorRef>(actors);
        }

        private List<ActorRef> Selections { get; set; }
    }
}
