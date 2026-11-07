using System;
using System.Collections.Generic;
using Contracts.State;
using NUnit.Framework;

namespace Gameplay.Tests
{
    public class VersionedValueTests
    {
        [Test]
        public void DefaultVersion_IsReadOnFirstRefresh()
        {
            var value = default(VersionedValue<int>);
            var source = new object();
            var reader = new VersionReader();

            Assert.AreEqual(0UL, value.Version);
            Assert.IsTrue(reader.NeedsRefresh(source, value.Version));
            reader.Record(source, value.Version);
            Assert.IsFalse(reader.NeedsRefresh(source, value.Version));
        }

        [Test]
        public void Readers_AcknowledgeChangesIndependently()
        {
            var value = new VersionedValue<int>(0);
            var source = new object();
            var first = new VersionReader();
            var second = new VersionReader();
            first.Record(source, value.Version);
            second.Record(source, value.Version);

            value.Set(1);
            first.Record(source, value.Version);

            Assert.IsFalse(first.NeedsRefresh(source, value.Version));
            Assert.IsTrue(second.NeedsRefresh(source, value.Version));
            second.Record(source, value.Version);
            Assert.IsFalse(second.NeedsRefresh(source, value.Version));
        }

        [Test]
        public void ChangeDuringRender_RemainsPendingAfterRecordingCapturedVersion()
        {
            var value = new VersionedValue<int>(0);
            var source = new object();
            var reader = new VersionReader();
            ulong captured = value.Version;

            value.Set(1);
            reader.Record(source, captured);

            Assert.IsTrue(reader.NeedsRefresh(source, value.Version));
        }

        [Test]
        public void ReplacementSource_WithSameVersionRequiresRefresh()
        {
            var reader = new VersionReader();
            reader.Record(new object(), 1);

            Assert.IsTrue(reader.NeedsRefresh(new object(), 1));
        }

        [Test]
        public void Invalidate_OnlyAffectsCurrentReader()
        {
            var source = new object();
            var first = new VersionReader();
            var second = new VersionReader();
            first.Record(source, 1);
            second.Record(source, 1);

            first.Invalidate();

            Assert.IsTrue(first.NeedsRefresh(source, 1));
            Assert.IsFalse(second.NeedsRefresh(source, 1));
        }

        [Test]
        public void Touch_PublishesInPlaceCollectionChangesWithoutCopying()
        {
            var list = new List<int> { 1 };
            var value = new VersionedValue<List<int>>(list);
            ulong previous = value.Version;

            list[0] = 2;
            value.Touch();

            Assert.AreSame(list, value.Value);
            Assert.AreEqual(2, value.Value[0]);
            Assert.AreNotEqual(previous, value.Version);
            previous = value.Version;
            value.Set(list);
            Assert.AreNotEqual(previous, value.Version, "相同集合引用也能重新发布内容变化。");
        }

        [Test]
        public void MultipleWrites_ExposeFinalValueToNextRead()
        {
            var source = new object();
            var reader = new VersionReader();
            var value = new VersionedValue<int>(0);
            reader.Record(source, value.Version);

            value.Value = 1;
            value.Set(2);
            value.Set(3);

            Assert.IsTrue(reader.NeedsRefresh(source, value.Version));
            Assert.AreEqual(3, value.Value);
            reader.Record(source, value.Version);
            Assert.IsFalse(reader.NeedsRefresh(source, value.Version));
        }

        [Test]
        public void VersionPollingAndAcknowledgment_AllocateNoManagedMemory()
        {
            var source = new object();
            var reader = new VersionReader();
            var value = new VersionedValue<int>(0);
            for (int i = 0; i < 100; i++)
            {
                value.Set(i);
                if (reader.NeedsRefresh(source, value.Version)) reader.Record(source, value.Version);
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++)
            {
                value.Set(i);
                if (reader.NeedsRefresh(source, value.Version)) reader.Record(source, value.Version);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.AreEqual(0L, allocated);
        }
    }
}
