using Acrofobia.Stimuli;
using NUnit.Framework;

namespace Acrofobia.Tests
{
    public sealed class StimulusBlenderTests
    {
        [Test]
        public void Target_IsReachedAfterTheTransitionDuration()
        {
            var blender = new StimulusBlender(2f, StimulusValues.Minimum);

            blender.SetTarget(StimulusId.Height, 80f, now: 10f);
            blender.Update(11f);
            float halfway = blender.Current.height;
            blender.Update(12f);

            Assert.That(halfway, Is.EqualTo(40f).Within(0.01f), "smoothstep is symmetric, so t = 0.5 gives half the change");
            Assert.That(blender.Current.height, Is.EqualTo(80f));
        }

        [Test]
        public void NewTarget_MidTransition_StartsFromTheCurrentValue()
        {
            var blender = new StimulusBlender(2f, StimulusValues.Minimum);
            blender.SetTarget(StimulusId.Wind, 100f, now: 0f);
            blender.Update(1f);
            float before = blender.Current.wind;

            blender.SetTarget(StimulusId.Wind, 0f, now: 1f);
            blender.Update(1f);

            Assert.That(blender.Current.wind, Is.EqualTo(before).Within(0.001f), "no jump when the target changes");
        }

        [Test]
        public void Values_AreClampedToTheScale()
        {
            var blender = new StimulusBlender(0f, StimulusValues.Minimum);
            blender.SetTarget(StimulusId.Fog, 140f, now: 0f);
            blender.Update(0f);

            Assert.That(blender.Target.fog, Is.EqualTo(100f));
            Assert.That(blender.Current.fog, Is.EqualTo(100f));
        }

        [Test]
        public void SettingTheSameTarget_ReportsNoChange()
        {
            var blender = new StimulusBlender(2f, new StimulusValues(10f, 10f, 10f, 10f, 10f));
            Assert.That(blender.SetTarget(StimulusId.Sway, 10f, now: 0f), Is.False);
        }

        [Test]
        public void Reset_JumpsWithoutInterpolation()
        {
            var blender = new StimulusBlender(2f, StimulusValues.Minimum);
            var values = new StimulusValues(1f, 2f, 3f, 4f, 5f);
            blender.Reset(values);
            blender.Update(0.1f);
            Assert.That(blender.Current, Is.EqualTo(values));
        }
    }
}
