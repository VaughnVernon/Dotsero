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

    [TestClass]
    public class ActorPathTests
    {
        [TestMethod]
        public void UserContextPath()
        {
            ActorSystem system = ActorSystem.Create("ActorPathTests");

            Assert.AreEqual("/user", system.Context.Path.Value);

            Assert.IsFalse(system.Context.Path.IsRoot());

            system.Shutdown();
        }

        [TestMethod]
        public void SystemContextPath()
        {
            ActorSystem system = ActorSystem.Create("ActorPathTests");

            Assert.AreEqual("/sys", system.SystemContext.Path.Value);

            Assert.IsFalse(system.Context.Path.IsRoot());

            system.Shutdown();
        }

        [TestMethod]
        public void SystemParentPath()
        {
            ActorSystem system = ActorSystem.Create("ActorPathTests");

            Assert.AreEqual(ActorPath.RootName, system.Context.Parent.Path.Value);

            Assert.IsTrue(system.Context.Parent.Path.IsRoot());

            system.Shutdown();
        }

        [TestMethod]
        public void ActorName()
        {
            ActorSystem system = ActorSystem.Create("ActorPathTests");

            Assert.AreEqual("deadLetters", system.DeadLetters.Path.Name);

            system.Shutdown();
        }
    }
}
