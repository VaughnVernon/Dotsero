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
    public class SupervisionStopTests
    {
        private ActorSystem system;

        [TestMethod]
        public void SupervisionStop()
        {
            AutoResetEvent[] levelEvents = LevelEvents();
            ActorRef level1 = StopLevel1Actor(levelEvents);

            level1.Tell("CRASH");
            level1.Tell("TEST NO LEVEL2");

            var set1 = levelEvents[0].WaitOne(1000, false);
            var set2 = levelEvents[1].WaitOne(1000, false);
            var set3 = levelEvents[2].WaitOne(1000, false);

            system.Shutdown();

            Assert.IsTrue(set1);
            Assert.IsFalse(set2);
            Assert.IsFalse(set3);
        }

        private ActorRef StopLevel1Actor(AutoResetEvent[] levelEvents)
        {
            system = ActorSystem.Create("ActorTests");

            return system.ActorOf(typeof(StopLevel1), Props.With((object)levelEvents), "stopLevel1");
        }

        private AutoResetEvent[] LevelEvents()
        {
            AutoResetEvent[] levelEvents = new AutoResetEvent[3];

            levelEvents[0] = new AutoResetEvent(false);
            levelEvents[1] = new AutoResetEvent(false);
            levelEvents[2] = new AutoResetEvent(false);

            return levelEvents;
        }
    }

    public class StopLevel1 : Actor
    {
        private AutoResetEvent[] levelEvents;
        private ActorRef level2;

        public StopLevel1(AutoResetEvent[] levelEvents)
        {
            this.levelEvents = levelEvents;
        }

        public override void PreStart()
        {
            base.PreStart();

            level2 = Context.ActorOf(typeof(StopLevel2), Props.With((object)levelEvents), "stopLevel2");
        }

        public void OnReceive(string message)
        {
            level2.Tell(message);
        }
    }

    public class StopLevel2 : Actor
    {
        private AutoResetEvent[] levelEvents;

        public StopLevel2(AutoResetEvent[] levelEvents)
        {
            this.levelEvents = levelEvents;
        }

        public override void PostStop()
        {
            base.PostStop();

            levelEvents[0].Set();
        }

        public void OnReceive(string message)
        {
            // the EscalateSupervisorStrategy is
            // used by my parent, so the exception
            // type will cause a escalation. Level1
            // will receive the escalation and its
            // supervisor strategy will tell Leve2
            // to restart.
            throw new InvalidOperationException("TEST STOP");
        }
    }
}
