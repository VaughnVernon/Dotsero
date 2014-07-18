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
    public class SupervisionRestartTests
    {
        private ActorSystem system;

        [TestMethod]
        public void SupervisionRestart()
        {
            AutoResetEvent[] levelEvents = LevelEvents();
            ActorRef level1 = RestartLevel1Actor(levelEvents);

            level1.Tell("CRASH");

            var set1 = levelEvents[0].WaitOne(1000, false);
            var set2 = levelEvents[1].WaitOne(1000, false);
            var set3 = levelEvents[2].WaitOne(1000, false);

            system.Shutdown();

            Assert.IsTrue(set1);
            Assert.IsTrue(set2);
            Assert.IsTrue(set3);
        }

        private ActorRef RestartLevel1Actor(AutoResetEvent[] levelEvents)
        {
            system = ActorSystem.Create("ActorTests");

            return system.ActorOf(typeof(RestartLevel1), Props.With((object)levelEvents), "restartLevel1");
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

    public class RestartSupervisorStrategy : SupervisorStrategy
    {
        public RestartSupervisorStrategy()
            : base(SupervisorStrategy.StrategyType.OneForOne)
        {
        }

        public override Directive Decide(Exception e)
        {
            return SupervisorStrategy.Directive.Restart;
        }
    }

    public class RestartLevel1 : Actor
    {
        private AutoResetEvent[] levelEvents;
        private ActorRef level2;

        public RestartLevel1(AutoResetEvent[] levelEvents)
        {
            this.levelEvents = levelEvents;
        }

        public override void PreStart()
        {
            base.PreStart();

            level2 = Context.ActorOf(typeof(RestartLevel2), Props.With((object)levelEvents), "restartLevel2");
        }

        public void OnReceive(string message)
        {
            level2.Tell(message);
        }
    }

    public class RestartLevel2 : Actor
    {
        private AutoResetEvent[] levelEvents;
        private ActorRef level3;

        public RestartLevel2(AutoResetEvent[] levelEvents)
        {
            this.levelEvents = levelEvents;
        }

        public override void PreStart()
        {
            base.PreStart();

            level3 = Context.ActorOf(
                typeof(RestartLevel3),
                Props.With((object)levelEvents),
                "restartLevel3");
        }

        public void OnReceive(string message)
        {
            level3.Tell(message);
        }
    }

    public class RestartLevel3 : Actor
    {
        private AutoResetEvent[] levelEvents;
        private bool crashed;
        private bool restarted;

        public RestartLevel3(AutoResetEvent[] levelEvents)
        {
            this.levelEvents = levelEvents;
        }

        public override void PreStart()
        {
            base.PreStart();

            if (restarted) // should be set by PostRestart
            {
                levelEvents[2].Set();
            }
        }

        public override void PreRestart(Exception reason, object message)
        {
            base.PreRestart(reason, message);

            if (crashed) // still set
            {
                levelEvents[0].Set();
            }
        }

        public override void PostRestart(Exception reason)
        {
            if (!crashed) // cleared by new instance restart
            {
                restarted = true;

                levelEvents[1].Set();
            }

            base.PostRestart(reason);
        }

        public void OnReceive(string message)
        {
            if (!crashed)
            {
                crashed = true;

                // the default SupervisorStrategy is
                // used so the Exception type will
                // cause a restart.
                throw new Exception("TEST RESTART");
            }
        }
    }
}
