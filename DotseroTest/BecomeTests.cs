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

namespace DotseroTest
{
    using Dotsero.Actor;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Threading;

    [TestClass]
    public class BecomeTests
    {
        [TestMethod]
        public void BecomeTwice()
        {
            var system = ActorSystem.Create("BecomingTests");

            AutoResetEvent endEvent = new AutoResetEvent(false);

            var end =
                system.ActorOf(
                    typeof(EndAddAddAddMultMult),
                    Props.With(endEvent));

            var counting =
                system.ActorOf(
                    typeof(Counting),
                    Props.With(end));

            counting.Tell("add");
            counting.Tell("add");
            counting.Tell("add");
            counting.Tell("multiply");
            counting.Tell("multiply");
            counting.Tell("end");

            var ended = endEvent.WaitOne(1000, false);

            system.Stop(counting);

            system.Stop(end);

            system.Shutdown();

            Assert.IsTrue(ended);
        }

        [TestMethod]
        public void BecomeTwiceUnbecomeOnce()
        {
            var system = ActorSystem.Create("BecomeTests");

            AutoResetEvent endEvent = new AutoResetEvent(false);

            var end = 
                system.ActorOf(
                    typeof(EndAddAddMult),
                    Props.With(endEvent));

            var counting =
                system.ActorOf(
                    typeof(Counting),
                    Props.With(end));

            counting.Tell("add");
            counting.Tell("add");
            counting.Tell("add");
            counting.Tell("multiply");
            counting.Tell("back");
            counting.Tell("end");

            var ended = endEvent.WaitOne(1000, false);

            system.Stop(counting);

            system.Stop(end);

            system.Shutdown();

            Assert.IsTrue(ended);
        }
    }

    /// <summary>
    /// Counting actor.
    /// </summary>
    public class Counting : Actor
    {
        public Counting(ActorRef end) : base()
        {
            this.end = end;

            AdditionBehavior = new Addition(this);

            MultiplicationBehavior = new Multiplication(this);
        }

        public void OnReceive(string message)
        {
            Console.Out.WriteLine("Counting: MESSAGE = " + message);
        }

        public override void PreStart()
        {
            base.PreStart();

            Context.Become(AdditionBehavior);
        }

        private Receive AdditionBehavior { get; set; }
        private Receive MultiplicationBehavior { get; set; }
        private int Total { get; set; }

        private ActorRef end;

        /// <summary>
        /// Addition behavior.
        /// </summary>
        public class Addition : Receive
        {
            private Counting me;

            public Addition(Counting actor)
            {
                this.me = actor;
            }

            public void OnReceive(string message)
            {
                switch (message)
                {
                    case "add":
                        me.Total = me.Total + 1;

                        if (me.Total == 3)
                        {
                            me.Context.Become(me.MultiplicationBehavior);
                        }
                        break;

                    case "end":
                        me.end.Tell(me.Total);
                        break;

                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Multiplication behavior.
        /// </summary>
        public class Multiplication : Receive
        {
            private Counting me;

            public Multiplication(Counting actor)
            {
                me = actor;
            }

            public void OnReceive(string message)
            {
                switch (message)
                {
                    case "multiply":
                        me.Total = me.Total * 2;
                        break;

                    case "back":
                        me.Context.Unbecome();
                        break;

                    case "end":
                        me.end.Tell(me.Total);
                        break;

                    default:
                        break;
                }
            }
        }
    }

    public class EndAddAddMult : Actor
    {
        private AutoResetEvent endEvent;

        public EndAddAddMult(AutoResetEvent endEvent)
        {
            this.endEvent = endEvent;
        }

        public void OnReceive(int TotalCount)
        {
            Assert.AreEqual(6, TotalCount);
            endEvent.Set();
        }
    }

    public class EndAddAddAddMultMult : Actor
    {
        private AutoResetEvent endEvent;

        public EndAddAddAddMultMult(AutoResetEvent endEvent)
        {
            this.endEvent = endEvent;
        }

        public void OnReceive(int TotalCount)
        {
            Assert.AreEqual(12, TotalCount);
            endEvent.Set();
        }
    }
}
