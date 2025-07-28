using System;
using System.ComponentModel;
using System.Threading;
using Grumpy.Common.BaseObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BufferWorkerTests
{
    [TestClass]
    public class BackgroundWorkerTests
    {
        [TestMethod]
        public void Worker_ExecutesAction_OnResume() {
            int counter = 0;
            using var worker = new Grumpy.Common.BaseObjects.DormantWorker(() => Interlocked.Increment(ref counter));

            // Should not run until Resume is called
            Thread.Sleep(100);
            Assert.AreEqual(0, counter);

            worker.Resume();
            Thread.Sleep(100);
            Assert.AreEqual(1, counter);

            worker.Resume();
            Thread.Sleep(100);
            Assert.AreEqual(2, counter);
        }

        [TestMethod]
        public void Worker_Pauses_AfterEachAction() {
            int counter = 0;
            using var worker = new Grumpy.Common.BaseObjects.DormantWorker(() => Interlocked.Increment(ref counter));

            worker.Resume();
            Thread.Sleep(100);
            Assert.AreEqual(1, counter);

            // Wait to ensure it doesn't run again without Resume
            Thread.Sleep(200);
            Assert.AreEqual(1, counter);

            worker.Resume();
            Thread.Sleep(100);
            Assert.AreEqual(2, counter);
        }

        [TestMethod]
        public void Worker_Dispose_StopsThread() {
            int counter = 0;
            var worker = new Grumpy.Common.BaseObjects.DormantWorker(() => Interlocked.Increment(ref counter));
            worker.Resume();
            Thread.Sleep(100);
            worker.Dispose();

            int countAfterDispose = counter;
            Thread.Sleep(200);
            Assert.AreEqual(countAfterDispose, counter, "Worker should not run after Dispose.");
        }

        [TestMethod]
        public void Worker_MultipleResume_Calls() {
            int counter = 0;
            using var worker = new Grumpy.Common.BaseObjects.DormantWorker(() => Interlocked.Increment(ref counter));

            for (int i = 0; i < 5; i++) {
                worker.Resume();
                Thread.Sleep(50);
            }
            Thread.Sleep(100);
            Assert.AreEqual(5, counter);
        }
    }
}