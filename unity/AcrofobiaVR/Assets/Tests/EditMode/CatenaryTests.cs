using Acrofobia.Environment.Bridge;
using NUnit.Framework;

namespace Acrofobia.Tests
{
    public sealed class CatenaryTests
    {
        [Test]
        public void Drop_IsZeroAtAnchors_AndEqualsSagAtMidSpan()
        {
            var curve = Catenary.FromSag(90f, 2.6f);

            Assert.That(curve.Drop(0f), Is.EqualTo(0f).Within(1e-3f));
            Assert.That(curve.Drop(1f), Is.EqualTo(0f).Within(1e-3f));
            Assert.That(curve.Drop(0.5f), Is.EqualTo(2.6f).Within(1e-3f));
        }

        [Test]
        public void Drop_IsSymmetric()
        {
            var curve = Catenary.FromSag(60f, 4f);
            Assert.That(curve.Drop(0.2f), Is.EqualTo(curve.Drop(0.8f)).Within(1e-4f));
        }

        [Test]
        public void ZeroSag_IsAStraightLine()
        {
            var curve = Catenary.FromSag(50f, 0f);
            Assert.That(curve.Drop(0.5f), Is.EqualTo(0f));
        }
    }
}
