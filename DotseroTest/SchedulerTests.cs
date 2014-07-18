namespace DotseroTest
{
    using Dotsero.Actor;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Threading;

    [TestClass]
    public class SchedulerTests
    {
        [TestMethod]
        public void ScheduleOnce()
        {
            var system = ActorSystem.Create("SchedulerTests");

            AutoResetEvent firedEvent = new AutoResetEvent(false);

            var actor = system.ActorOf(typeof(SchedulerTestOnce), Props.With(firedEvent));

            system.Scheduler.ScheduleOnce(100, actor, "FIRED");

            var fired = firedEvent.WaitOne(1000, false);

            system.Stop(actor);

            system.Shutdown();

            Assert.IsTrue(fired);
        }

        [TestMethod]
        public void ScheduleRepeating()
        {
            var system = ActorSystem.Create("SchedulerTests");

            AutoResetEvent[] firedEvents = new AutoResetEvent[3];
            firedEvents[0] = new AutoResetEvent(false);
            firedEvents[1] = new AutoResetEvent(false);
            firedEvents[2] = new AutoResetEvent(false);

            var actor = system.ActorOf(typeof(SchedulerTestRepeating), Props.With((object)firedEvents));

            Cancellable schedule = system.Scheduler.Schedule(100, 100, actor, "FIRED");

            var fired1 = firedEvents[0].WaitOne(1000, false);
            var fired2 = firedEvents[1].WaitOne(1000, false);
            var fired3 = firedEvents[2].WaitOne(1000, false);

            schedule.Cancel();

            system.Stop(actor);

            system.Shutdown();

            Assert.IsTrue(fired1);
            Assert.IsTrue(fired2);
            Assert.IsTrue(fired3);
        }

        [TestMethod]
        public void ScheduleCancel()
        {
            var system = ActorSystem.Create("SchedulerTests");

            AutoResetEvent[] firedEvents = new AutoResetEvent[3];
            firedEvents[0] = new AutoResetEvent(false);
            firedEvents[1] = new AutoResetEvent(false);
            firedEvents[2] = new AutoResetEvent(false);

            var actor = system.ActorOf(typeof(SchedulerTestRepeating), Props.With((object)firedEvents));

            Cancellable schedule = system.Scheduler.Schedule(100, 100, actor, "FIRED");

            var fired1 = firedEvents[0].WaitOne(1000, false);
            var fired2 = firedEvents[1].WaitOne(1000, false);

            schedule.Cancel();

            var fired3 = firedEvents[2].WaitOne(1000, false);

            system.Stop(actor);

            system.Shutdown();

            Assert.IsTrue(fired1);
            Assert.IsTrue(fired2);
            Assert.IsFalse(fired3);
            Assert.IsTrue(schedule.Cancelled);
        }
    }

    public class SchedulerTestOnce : Actor
    {
        private AutoResetEvent firedEvent;

        public SchedulerTestOnce(AutoResetEvent firedEvent)
        {
            this.firedEvent = firedEvent;
        }

        public void OnReceive(string message)
        {
            if (message.Equals("FIRED"))
            {
                firedEvent.Set();
            }
        }
    }

    public class SchedulerTestRepeating : Actor
    {
        private AutoResetEvent[] firedEvents;
        private int receivedCount;

        public SchedulerTestRepeating(AutoResetEvent[] firedEvents)
        {
            this.firedEvents = firedEvents;
        }

        public void OnReceive(string message)
        {
            if (receivedCount < 3)
            {
                if (message.Equals("FIRED"))
                {
                    firedEvents[receivedCount++].Set();
                }
            }
            else
            {
                Context.Stop(Self);
            }
        }
    }
}
