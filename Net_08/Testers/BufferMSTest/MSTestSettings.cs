
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grumpy.Common.BaseObjects.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BufferMSTest
{
    [TestClass]
    public class BufferBaseTests
    {
        [TestMethod]
        public void AddAndRemove_SingleThreaded_WorksCorrectly() {
            var buffer = new BufferBase<int>(5);

            Assert.IsTrue(buffer.TryAdd(1, out var error1), error1);
            Assert.IsTrue(buffer.TryAdd(2, out var error2), error2);
            Assert.AreEqual(2, buffer.Count);

            Assert.IsTrue(buffer.TryPopAt(0, out var val1, out var error3), error3);
            Assert.AreEqual(1, val1);

            Assert.IsTrue(buffer.TryPeekAt(0, out var val2, out var error4), error4);
            Assert.AreEqual(2, val2);

            Assert.IsTrue(buffer.TryPopLast(out var val3, out var error5), error5);
            Assert.AreEqual(2, val3);

            Assert.IsTrue(buffer.IsEmpty);
        }

        [TestMethod]
        public void Events_AreRaisedCorrectly() {
            var buffer = new BufferBase<int>(3);
            var events = new List<string>();

            buffer.HasBecomeEmpty += (s, e) => { lock (events) { events.Add("Empty"); } };
            buffer.HasReachedCapacity += (s, e) => { lock (events) { events.Add("Capacity"); } };
            buffer.AtUpperThreshold += (s, e) => { lock (events) { events.Add("Upper"); } };
            buffer.AtLowerThreshold += (s, e) => { lock (events) { events.Add("Lower"); } };
            buffer.ItemAdded += (s, count) => { lock (events) { events.Add($"Added:{count}"); } };
            buffer.ItemsDiscarded += (s, count) => { lock (events) { events.Add($"Discarded:{count}"); } };

            buffer.UpperThreshold = 2;
            buffer.LowerThreshold = 1;

            buffer.TryAdd(1, out _); // Should trigger ItemAdded
            buffer.TryAdd(2, out _); // Should trigger ItemAdded, Upper
            buffer.TryAdd(3, out _); // Should trigger ItemAdded, Capacity

            buffer.TryPopLast(out _, out _); // Should trigger nothing
            buffer.TryPopLast(out _, out _); // Should trigger Lower
            buffer.TryPopLast(out _, out _); // Should trigger Empty

            // Wait for async events
            Thread.Sleep(300);

            lock (events) {
                Assert.IsTrue(events.Any(e => e.StartsWith("Added")));
                Assert.IsTrue(events.Contains("Upper"));
                Assert.IsTrue(events.Contains("Capacity"));
                Assert.IsTrue(events.Contains("Lower"));
                Assert.IsTrue(events.Contains("Empty"));
            }
        }

        [TestMethod]
        public void TryAddItems_RespectsCapacity() {
            var buffer = new BufferBase<int>(4);
            Assert.IsTrue(buffer.TryAddItems(new[] { 1, 2 }, out var error1), error1);
            Assert.IsTrue(buffer.TryAddItems(new[] { 3 }, out var error2), error2);
            Assert.IsFalse(buffer.TryAddItems(new[] { 4, 5 }, out var error3));
            Assert.IsTrue(error3.Contains("Not enough room"));
        }

        [TestMethod]
        public void TryMakeRoom_RemovesItemsCorrectly() {
            var buffer = new BufferBase<int>(4);
            buffer.TryAddItems(new[] { 1, 2, 3, 4 }, out _);

            Assert.IsTrue(buffer.TryMakeRoom(2, out var removed, out var error), error);
            Assert.AreEqual(2, removed);
            Assert.AreEqual(2, buffer.Count);
        }

        [TestMethod]
        public void Multithreaded_AddAndRemove_IsThreadSafe() {
            var buffer = new BufferBase<int>(100);
            int addSuccess = 0, popSuccess = 0;
            int threadCount = 8;
            int itemsPerThread = 1000;

            var addTasks = Enumerable.Range(0, threadCount).Select(_ => Task.Run(() => {
                for (int i = 0; i < itemsPerThread; i++) {
                    if (buffer.TryAdd(i, out string _))
                        Interlocked.Increment(ref addSuccess);
                }
            })).ToArray();

            var popTasks = Enumerable.Range(0, threadCount).Select(_ => Task.Run(() => {
                for (int i = 0; i < itemsPerThread; i++) {
                    if (buffer.TryPopLast(out _, out string _))
                        Interlocked.Increment(ref popSuccess);
                }
            })).ToArray();

            Task.WaitAll(addTasks.Concat(popTasks).ToArray());

            Assert.IsTrue(buffer.Count >= 0 && buffer.Count <= buffer.MaxCapacity);
            Assert.IsTrue(addSuccess > 0);
            Assert.IsTrue(popSuccess >= 0);
        }

        [TestMethod]
        public void PeekAllAsArrayAndList_ReturnsCorrectOrder() {
            var buffer = new BufferBase<int>(10);
            buffer.TryAddItems(new[] { 1, 2, 3 }, out _);

            var arr = buffer.PeekAllAsArray(out var error1);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, arr);

            var arrRev = buffer.PeekAllAsArray(out var error2, recentFirst: true);
            CollectionAssert.AreEqual(new[] { 3, 2, 1 }, arrRev);

            var list = buffer.PeekAllAsList(out var error3);
            CollectionAssert.AreEqual(new List<int> { 1, 2, 3 }, list);

            var listRev = buffer.PeekAllAsList(out var error4, recentFirst: true);
            CollectionAssert.AreEqual(new List<int> { 3, 2, 1 }, listRev);
        }
    }
}
