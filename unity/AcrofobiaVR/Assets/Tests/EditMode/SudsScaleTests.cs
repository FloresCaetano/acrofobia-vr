using Acrofobia.Suds;
using NUnit.Framework;

namespace Acrofobia.Tests
{
    public sealed class SudsScaleTests
    {
        [TestCase(0, "Totalmente tranquilo")]
        [TestCase(30, "Algo inquieto")]
        [TestCase(50, "Ansioso")]
        [TestCase(70, "Muy ansioso")]
        [TestCase(85, "Con mucho miedo")]
        [TestCase(100, "El peor miedo")]
        public void Label_DescribesTheValue(int value, string expected)
        {
            Assert.That(SudsScale.Label(value), Is.EqualTo(expected));
        }

        [Test]
        public void Smile_GoesFromSmileToFrown()
        {
            Assert.That(SudsScale.Smile(0), Is.EqualTo(1f));
            Assert.That(SudsScale.Smile(100), Is.EqualTo(-1f));
        }

        [Test]
        public void OutOfRangeValues_AreClamped()
        {
            Assert.That(SudsScale.Label(-20), Is.EqualTo(SudsScale.Label(0)));
            Assert.That(SudsScale.Label(250), Is.EqualTo(SudsScale.Label(100)));
        }
    }
}
