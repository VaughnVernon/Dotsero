using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dotsero.Actor
{
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
