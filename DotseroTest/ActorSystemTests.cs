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
    public class ActorSystemTests
    {
        [TestMethod]
        public void SystemStart()
        {
            ActorSystem system = ActorSystem.Create("ActorSystemTests");

            Assert.AreEqual("ActorSystemTests", system.Name);

            system.Shutdown();
        }

        [TestMethod]
        public void SystemStartWithUpTime()
        {
            DateTime preSystem = DateTime.Now;

            Thread.Sleep(10);

            ActorSystem system = ActorSystem.Create("ActorSystemTests");

            Thread.Sleep(150);

            Assert.IsTrue(preSystem.CompareTo(system.StartTime) <= 0);

            Assert.IsTrue(system.UpTime.Ticks > 100);

            system.Shutdown();
        }

        [TestMethod]
        public void DeadLetters()
        {
            ActorSystem system = ActorSystem.Create("ActorSystemTests");

            system.DeadLetters.Tell("TESTING DEAD LETTERS");

            Thread.Sleep(1000);

            system.Shutdown();
        }

        [TestMethod]
        public void DeadActorSelection()
        {
            ActorSystem system = ActorSystem.Create("ActorSystemTests");

            ActorSelection selection = system.ActorSelection("/user/NonExistingActor42");

            selection.Tell("TESTING DEAD LETTERS");

            Thread.Sleep(1000);

            system.Shutdown();
        }
    }
}
