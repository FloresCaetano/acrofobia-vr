using System;
using System.IO;
using Acrofobia.Stimuli;
using Acrofobia.Suds;
using NUnit.Framework;

namespace Acrofobia.Tests
{
    public sealed class SudsOutboxTests
    {
        private string file;

        [SetUp]
        public void CreateTempPath() => file = Path.Combine(Path.GetTempPath(), $"outbox_{Guid.NewGuid():N}.json");

        [TearDown]
        public void DeleteTempFile()
        {
            if (File.Exists(file)) File.Delete(file);
        }

        private static SudsRecord Record(int value) =>
            SudsRecord.Create(value, 2, "Acrophobia", SudsTrigger.Auto, StimulusValues.Minimum, DateTimeOffset.UtcNow);

        [Test]
        public void PendingAnswers_SurviveARestart()
        {
            var outbox = new SudsOutbox(file);
            outbox.Add(Record(40), sentOffline: true);
            outbox.Add(Record(35), sentOffline: false);

            var reloaded = new SudsOutbox(file);

            Assert.That(reloaded.Count, Is.EqualTo(2));
            Assert.That(reloaded.Pending[0].record.value, Is.EqualTo(40));
            Assert.That(reloaded.Pending[0].queued, Is.True);
        }

        [Test]
        public void Acknowledge_RemovesOnlyThatAnswer()
        {
            var outbox = new SudsOutbox(file);
            var first = Record(10);
            outbox.Add(first, false);
            outbox.Add(Record(20), false);

            Assert.That(outbox.Acknowledge(first.id), Is.True);
            Assert.That(outbox.Acknowledge("unknown"), Is.False);
            Assert.That(new SudsOutbox(file).Count, Is.EqualTo(1));
        }

        [Test]
        public void Disconnection_MarksPendingAnswersAsLate()
        {
            var outbox = new SudsOutbox(file);
            outbox.Add(Record(50), false);
            outbox.MarkAllQueued();
            Assert.That(outbox.Pending[0].queued, Is.True);
        }

        [Test]
        public void CorruptFile_StartsEmpty()
        {
            File.WriteAllText(file, "{ not json");
            Assert.That(new SudsOutbox(file).Count, Is.EqualTo(0));
        }
    }
}
