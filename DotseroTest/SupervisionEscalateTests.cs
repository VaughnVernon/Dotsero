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
    public class SupervisionEscalateTests
    {
        private ActorSystem system;

        [TestMethod]
        public void SupervisionEscalate()
        {
            AutoResetEvent[] levelEvents = LevelEvents();
            ActorRef level1 = EscalateLevel1(levelEvents);

            level1.Tell("CRASH");

            var set1 = levelEvents[0].WaitOne(1000000, false);
            var set2 = levelEvents[1].WaitOne(1000000, false);
            var set3 = levelEvents[2].WaitOne(1000000, false);

            system.Shutdown();

            Assert.IsTrue(set1);
            Assert.IsTrue(set2);
            Assert.IsTrue(set3);
        }

        private ActorRef EscalateLevel1(AutoResetEvent[] levelEvents)
        {
            system = ActorSystem.Create("ActorTests");

            return system.ActorOf(typeof(EscalateLevel1), Props.With((object)levelEvents), "escalateLevel1");
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

    public class EscalateSupervisorStrategy : SupervisorStrategy
    {
        public EscalateSupervisorStrategy()
            : base(SupervisorStrategy.StrategyType.OneForOne)
        {
        }

        public override Directive Decide(Exception e)
        {
            return SupervisorStrategy.Directive.Escalate;
        }
    }

    public class EscalateLevel1 : Actor
    {
        private AutoResetEvent[] levelEvents;
        private ActorRef level2;

        public EscalateLevel1(AutoResetEvent[] levelEvents)
        {
            this.levelEvents = levelEvents;
        }

        public override void PreStart()
        {
            base.PreStart();

            level2 = Context.ActorOf(
                typeof(EscalateLevel2),
                Props.With((object)levelEvents),
                "escalateLevel2");
        }

        public void OnReceive(string message)
        {
            level2.Tell(message);
        }
    }

    public class EscalateLevel2 : Actor
    {
        private AutoResetEvent[] levelEvents;
        private ActorRef level3;
        private bool restarted;

        public EscalateLevel2(AutoResetEvent[] levelEvents)
        {
            this.levelEvents = levelEvents;

            SupervisorStrategy = new EscalateSupervisorStrategy();
        }

        public override void PreStart()
        {
            base.PreStart();

            if (restarted)
            {
                levelEvents[2].Set();
            }

            level3 = Context.ActorOf(
                typeof(EscalateLevel3),
                Props.With((object)levelEvents),
                "escalateLevel3");
        }

        public override void PreRestart(Exception reason, object message)
        {
            base.PreRestart(reason, message);

            levelEvents[0].Set();
        }

        public override void PostRestart(Exception reason)
        {
            levelEvents[1].Set();

            restarted = true;

            base.PostRestart(reason);
        }

        public void OnReceive(string message)
        {
            level3.Tell(message);
        }
    }

    public class EscalateLevel3 : Actor
    {
        private AutoResetEvent[] levelEvents;

        public EscalateLevel3(AutoResetEvent[] levelEvents)
        {
            this.levelEvents = levelEvents;
        }

        public void OnReceive(string message)
        {
            Console.Out.WriteLine("Level3C: throwing");

            // the EscalateSupervisorStrategy is
            // used by my parent, so the exception
            // type will cause a escalation. Level1
            // will receive the escalation and its
            // supervisor strategy will tell Leve2
            // to restart.
            throw new InvalidOperationException("TEST ESCALATE");
        }
    }
}
