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
    public class SupervisionResumeTests
    {
        private ActorSystem system;

        [TestMethod]
        public void SupervisionResume()
        {
            AutoResetEvent[] levelEvents = LevelEvents();
            ActorRef level1 = ResumeLevel1Actor(levelEvents);

            level1.Tell("CRASH");
            level1.Tell("TEST NO CRASH");

            var set1 = levelEvents[0].WaitOne(1000, false);
            var set2 = levelEvents[1].WaitOne(1000, false);
            var set3 = levelEvents[2].WaitOne(1000, false);

            system.Shutdown();

            Assert.IsTrue(set1);
            Assert.IsTrue(set2);
            Assert.IsFalse(set3);
        }

        private ActorRef ResumeLevel1Actor(AutoResetEvent[] levelEvents)
        {
            system = ActorSystem.Create("ActorTests");

            return system.ActorOf(typeof(ResumeLevel1), Props.With((object)levelEvents), "resumeLevel1");
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

    public class ResumeSupervisorStrategy : SupervisorStrategy
    {
        public ResumeSupervisorStrategy()
            : base(SupervisorStrategy.StrategyType.OneForOne)
        {
        }

        public override Directive Decide(Exception e)
        {
            return SupervisorStrategy.Directive.Resume;
        }
    }

    public class ResumeLevel1 : Actor
    {
        private AutoResetEvent[] levelEvents;
        private ActorRef level2;

        public ResumeLevel1(AutoResetEvent[] levelEvents)
        {
            this.levelEvents = levelEvents;
        }

        public override void PreStart()
        {
            base.PreStart();

            level2 = Context.ActorOf(typeof(ResumeLevel2), Props.With((object)levelEvents), "resumeLevel2");
        }

        public void OnReceive(string message)
        {
            level2.Tell(message);
        }
    }

    public class ResumeLevel2 : Actor
    {
        private AutoResetEvent[] levelEvents;
        private ActorRef level3;

        public ResumeLevel2(AutoResetEvent[] levelEvents)
        {
            this.levelEvents = levelEvents;

            SupervisorStrategy = new ResumeSupervisorStrategy();
        }

        public override void PreStart()
        {
            base.PreStart();

            level3 = Context.ActorOf(
                typeof(ResumeLevel3),
                Props.With((object)levelEvents),
                "resumeLevel3");
        }

        public void OnReceive(string message)
        {
            level3.Tell(message);
        }
    }

    public class ResumeLevel3 : Actor
    {
        private bool shouldResume;

        private AutoResetEvent[] levelEvents;

        public ResumeLevel3(AutoResetEvent[] levelEvents)
        {
            this.levelEvents = levelEvents;
        }

        public void OnReceive(string message)
        {
            if (shouldResume)
            {
                levelEvents[1].Set();

                Console.Out.WriteLine("3A OnReceive(): resumed");
            }
            else
            {
                shouldResume = true;

                levelEvents[0].Set(); 
                
                Console.Out.WriteLine("3A OnReceive(): throwing");

                // the ResumeSupervisorStrategy is
                // used so the Exception type will
                // cause a resume.
                throw new Exception("TEST RESUME");
            }
        }
    }
}
