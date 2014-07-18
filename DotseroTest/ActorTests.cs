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
    public class ActorTests
    {
        [TestMethod]
        public void ActorGeneratedName()
        {
            var system = ActorSystem.Create("ActorTests");

            var actor = system.ActorOf(typeof(TestActor), Props.None);

            var prefix = "/user/$";

            var actorPath = actor.Path.Value;

            system.Shutdown();

            Assert.IsTrue(actorPath.StartsWith(prefix));

            Assert.IsTrue(actorPath.Length == (prefix.Length + 12));
        }

        [TestMethod]
        public void ActorGivenName()
        {
            var system = ActorSystem.Create("ActorTests");

            var name = Guid.NewGuid().ToString();

            var actor = system.ActorOf(typeof(TestActor), Props.None, name);

            var expectedPath = "/user/" + name;

            var actorPath = actor.Path.Value;

            system.Shutdown();

            Assert.AreEqual(expectedPath, actorPath);
        }

        [TestMethod]
        public void ForwardOriginal()
        {
            var system = ActorSystem.Create("ActorTests");

            AutoResetEvent endEvent = new AutoResetEvent(false);

            var originalName = "Original";
            var forwardTo = system.ActorOf(typeof(TestForwardToActor), Props.With(originalName, endEvent), "ForwardTo");
            var forwardFrom = system.ActorOf(typeof(TestForwardFromActor), Props.With(forwardTo), "ForwardFrom");
            var original = system.ActorOf(typeof(TestForwardOriginalActor), Props.With(forwardFrom), originalName);

            original.Tell("TESTING...");

            var ended = endEvent.WaitOne(1000, false);

            system.Shutdown();

            Assert.IsTrue(ended);
        }

        [TestMethod]
        public void NumberOfAdditionsAndMultiplications()
        {
            var system = ActorSystem.Create("BecomingTests");

            AutoResetEvent endEvent = new AutoResetEvent(false);

            var addition =
                system.ActorOf(
                    typeof(Addition),
                    Props.With(endEvent));

            var multiplication =
                system.ActorOf(
                    typeof(Multiplication),
                    Props.With(endEvent));

            addition.Tell(new Operation(0, 2000000000, 0, multiplication));

            var ended = endEvent.WaitOne(2000, false);

            system.Stop(addition);

            system.Stop(multiplication);

            system.Shutdown();

            Assert.IsTrue(ended);
        }

        [TestMethod]
        public void NumberOfAdditionsAndAdditions()
        {
            var system = ActorSystem.Create("BecomingTests");

            AutoResetEvent endEvent = new AutoResetEvent(false);

            var addition1 =
                system.ActorOf(
                    typeof(Addition),
                    Props.With(endEvent));

            var addition2 =
                system.ActorOf(
                    typeof(Addition),
                    Props.With(endEvent));

            addition1.Tell(new Operation(0, 1000000, 0, addition2));

            var ended = endEvent.WaitOne(4000, false);

            system.Stop(addition1);

            system.Stop(addition2);

            system.Shutdown();

            Assert.IsTrue(ended);
        }
    }

    public class TestActor : Actor
    {
    }

    public class TestForwardOriginalActor : Actor
    {
        private ActorRef to;

        public TestForwardOriginalActor(ActorRef to)
        {
            this.to = to;
        }

        public void OnReceive(string message)
        {
            Console.Out.WriteLine("ORIGINAL: " + message);
            to.Tell(Self.Path.Name, Self);
        }
    }

    public class TestForwardFromActor : Actor
    {
        private ActorRef to;

        public TestForwardFromActor(ActorRef to)
        {
            this.to = to;
        }

        public void OnReceive(string message)
        {
            Console.Out.WriteLine("FROM: " + message);
            to.Forward(message, Context);
        }
    }

    public class TestForwardToActor : Actor
    {
        private AutoResetEvent endEvent;
        private string expectedPath;

        public TestForwardToActor(string expectedPath, AutoResetEvent endEvent)
        {
            this.expectedPath = expectedPath;
            this.endEvent = endEvent;
        }

        public void OnReceive(string message)
        {
            Console.Out.WriteLine("TO: " + message);

            Assert.AreEqual(expectedPath, message);

            endEvent.Set();
        }
    }

    public class Operation
    {
        public Operation(int total, int goal, int operations, ActorRef nextOperation)
        {
            Total = total;
            Goal = goal;
            Operations = operations;
            NextOperation = nextOperation;
        }

        public int Goal { get; private set; }
        public ActorRef NextOperation { get; private set; }
        public int Operations { get; private set; }
        public int Total { get; private set; }
    }

    public class Addition : Actor
    {
        private AutoResetEvent endEvent;

        public Addition(AutoResetEvent endEvent)
        {
            this.endEvent = endEvent;
        }

        public void OnReceive(Operation message)
        {
            int total = message.Total + 1;

            if (total >= message.Goal)
            {
                endEvent.Set();
            }
            else
            {
                message
                    .NextOperation
                    .Tell(new Operation(
                        total,
                        message.Goal,
                        message.Operations + 1,
                        Self));
            }
        }
    }

    public class Multiplication : Actor
    {
        private AutoResetEvent endEvent;

        public Multiplication(AutoResetEvent endEvent)
        {
            this.endEvent = endEvent;
        }

        public void OnReceive(Operation message)
        {
            int total = message.Total * 2;

            if (total >= message.Goal)
            {
                endEvent.Set();
            }
            else
            {
                message
                    .NextOperation
                    .Tell(new Operation(
                        total,
                        message.Goal,
                        message.Operations + 1,
                        Self));
            }
        }
    }
}
