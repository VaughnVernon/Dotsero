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
    public class ActorManagerWorkerTests
    {
        [TestMethod]
        public void ManagerUsesWorker()
        {
            ActorSystem system = ActorSystem.Create("ActorSystemTests");

            ActorRef worker =
                system.ActorOf(
                    typeof(Worker),
                    Props.None,
                    "worker");

            AutoResetEvent helloBackEvent = new AutoResetEvent(false);

            AutoResetEvent specificEvent = new AutoResetEvent(false);

            ActorRef manager =
                system.ActorOf(
                    typeof(Manager),
                    Props.With(worker, helloBackEvent, specificEvent),
                    "manager");

            manager.Tell("start");

            var helloBackDone = helloBackEvent.WaitOne(1000, false);

            var specificDone = specificEvent.WaitOne(500, false);

            system.Stop(worker);

            system.Stop(manager);

            system.Shutdown();

            Assert.IsTrue(helloBackDone);

            Assert.IsTrue(specificDone);
        }

        public class Manager : Actor
        {
            private EventWaitHandle helloBackEvent;
            private EventWaitHandle specificDone;
            private ActorRef worker;

            public Manager(
                ActorRef worker,
                EventWaitHandle helloBackEvent,
                EventWaitHandle specificDone)
                : base()
            {
                this.worker = worker;
                this.helloBackEvent = helloBackEvent;
                this.specificDone = specificDone;
            }

            public void OnReceive(string message)
            {
                switch (message)
                {
                    case "start":
                        worker.Tell("hello", Self);
                        break;

                    case "hello, back":
                        helloBackEvent.Set();
                        break;

                    default:
                        Assert.Fail("Invalid message type.");
                        break;
                }
            }

            public void OnReceive(SpecificMessage specificMessage)
            {
                specificDone.Set();
            }
        }

        public class SpecificMessage
        {
            public static SpecificMessage Create(string message)
            {
                return new SpecificMessage(message);
            }

            public SpecificMessage(string payload)
            {
                this.Payload = payload;
            }

            public string Payload { get; set; }

            public override string ToString()
            {
                return "SpecificMessage: " + Payload;
            }
        }

        public class Worker : Actor
        {
            public Worker()
            {
            }

            public void OnReceive(string message)
            {
                Assert.AreEqual("hello", message);

                if (this.CanReply)
                {
                    Sender.Tell("hello, back", Self);
                    Sender.Tell(SpecificMessage.Create("Very Specific"), Self);
                }
                else
                {
                    Assert.Fail("Worker: no reply-to available...");
                }
            }
        }
    }
}
