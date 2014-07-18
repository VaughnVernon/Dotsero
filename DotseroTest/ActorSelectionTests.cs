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
    using System.Threading;

    [TestClass]
    public class ActorSelectionTests
    {
        private ActorSystem system;

        [TestMethod]
        public void ActorSelectionFromRoot()
        {
            AutoResetEvent[] selEvents = SelEvents();

            Sel1Actor(selEvents);

            system.ActorSelection("/user/sel1").Tell("TEST");
            system.ActorSelection("/user/sel1/sel2").Tell("TEST");
            system.ActorSelection("/user/sel1/sel2/sel3").Tell("TEST");
            system.ActorSelection("/user/sel1/sel2/sel3/sel4").Tell("TEST");

            var set1 = selEvents[0].WaitOne(1000, false);
            var set2 = selEvents[1].WaitOne(1000, false);
            var set3 = selEvents[2].WaitOne(1000, false);
            var set4 = selEvents[3].WaitOne(1000, false);

            system.Shutdown();

            Assert.IsTrue(set1);
            Assert.IsTrue(set2);
            Assert.IsTrue(set3);
            Assert.IsTrue(set4);
        }

        [TestMethod]
        public void ActorSelectionFromContext()
        {
            AutoResetEvent[] selEvents = SelEvents();

            Sel1Actor(selEvents);

            system.ActorSelection("/user/sel1").Tell("SEL2");

            var set1 = selEvents[0].WaitOne(1000, false);
            var set2 = selEvents[1].WaitOne(1000, false);
            var set3 = selEvents[2].WaitOne(1000, false);
            var set4 = selEvents[3].WaitOne(1000, false);

            system.Shutdown();

            Assert.IsTrue(set1);
            Assert.IsTrue(set2);
            Assert.IsTrue(set3);
            Assert.IsTrue(set4);
        }

        private void Sel1Actor(AutoResetEvent[] selEvents)
        {
            system = ActorSystem.Create("ActorTests");

            system.ActorOf(
                typeof(TestSelection1),
                Props.With((object)selEvents),
                "sel1");
        }

        private AutoResetEvent[] SelEvents()
        {
            AutoResetEvent[] selEvents = new AutoResetEvent[4];

            selEvents[0] = new AutoResetEvent(false);
            selEvents[1] = new AutoResetEvent(false);
            selEvents[2] = new AutoResetEvent(false);
            selEvents[3] = new AutoResetEvent(false);

            return selEvents;
        }
    }

    public class TestSelection1 : Actor
    {
        private AutoResetEvent[] selEvents;

        public TestSelection1(AutoResetEvent[] selEvents)
        {
            this.selEvents = selEvents;
        }

        public override void PreStart()
        {
            base.PreStart();

            Context.ActorOf(
                typeof(TestSelection2),
                Props.With((object)selEvents),
                "sel2");
        }

        public void OnReceive(string message)
        {
            selEvents[0].Set();

            if (message.Equals("SEL2"))
            {
                Context.ActorSelection("sel2").Tell("SEL3-4");
            }
        }
    }

    public class TestSelection2 : Actor
    {
        private AutoResetEvent[] selEvents;

        public TestSelection2(AutoResetEvent[] selEvents)
        {
            this.selEvents = selEvents;
        }

        public override void PreStart()
        {
            base.PreStart();

            Context.ActorOf(
                typeof(TestSelection3),
                Props.With((object)selEvents),
                "sel3");
        }

        public void OnReceive(string message)
        {
            selEvents[1].Set();

            if (message.Equals("SEL3-4"))
            {
                Context.ActorSelection("sel3").Tell("TEST");
                Context.ActorSelection("sel3/sel4").Tell("TEST");
            }
        }
    }

    public class TestSelection3 : Actor
    {
        private AutoResetEvent[] selEvents;

        public TestSelection3(AutoResetEvent[] selEvents)
        {
            this.selEvents = selEvents;
        }

        public override void PreStart()
        {
            base.PreStart();

            Context.ActorOf(
                typeof(TestSelection4),
                Props.With((object)selEvents),
                "sel4");
        }

        public void OnReceive(string message)
        {
            selEvents[2].Set();
        }
    }

    public class TestSelection4 : Actor
    {
        private AutoResetEvent[] selEvents;

        public TestSelection4(AutoResetEvent[] selEvents)
        {
            this.selEvents = selEvents;
        }

        public void OnReceive(string message)
        {
            selEvents[3].Set();
        }
    }
}
