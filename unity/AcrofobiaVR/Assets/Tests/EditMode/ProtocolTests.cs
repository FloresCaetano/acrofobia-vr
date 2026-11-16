using Acrofobia.Network;
using Acrofobia.Stimuli;
using NUnit.Framework;

namespace Acrofobia.Tests
{
    public sealed class ProtocolTests
    {
        [Test]
        public void Messages_HaveTypePayloadAndTimestamp()
        {
            string json = Protocol.Serialize(Protocol.LevelSet, new LevelSetPayload { level = 3 });

            StringAssert.Contains("\"type\":\"level.set\"", json);
            StringAssert.Contains("\"payload\":{\"level\":3}", json);
            StringAssert.Contains("\"ts\":", json);
        }

        [Test]
        public void ServerMessages_AreReadByTypeThenPayload()
        {
            const string json = "{\"type\":\"stimulus.set\",\"payload\":{\"stimulus\":\"wind\",\"value\":72},\"ts\":1}";

            Assert.That(Protocol.ReadType(json), Is.EqualTo(Protocol.StimulusSet));
            var payload = Protocol.ReadPayload<StimulusSetPayload>(json);
            Assert.That(payload.stimulus, Is.EqualTo("wind"));
            Assert.That(payload.value, Is.EqualTo(72f));
        }

        [Test]
        public void MalformedJson_HasNoType()
        {
            Assert.That(Protocol.ReadType("not json"), Is.Null);
        }

        [Test]
        public void StimuliSerialize_WithProtocolKeys()
        {
            string json = Protocol.Serialize(Protocol.StateUpdate, new StatePayload { stimuli = new StimulusValues(1, 2, 3, 4, 5) });
            StringAssert.Contains("\"stimuli\":{\"height\":1.0,\"wind\":2.0,\"sway\":3.0,\"fog\":4.0,\"sound\":5.0}", json);
        }

        [TestCase(new[] { "app.exe", "-server", "ws://10.0.0.5:9000" }, "ws://10.0.0.5:9000/")]
        [TestCase(new[] { "app.exe" }, "ws://localhost:8080")]
        [TestCase(new[] { "app.exe", "-server", "http://wrong" }, "ws://localhost:8080")]
        [TestCase(new[] { "app.exe", "-server" }, "ws://localhost:8080")]
        public void ServerUrl_CanBeOverriddenFromTheCommandLine(string[] args, string expected)
        {
            Assert.That(NetworkSettings.ResolveUrl("ws://localhost:8080", args), Is.EqualTo(expected));
        }
    }
}
