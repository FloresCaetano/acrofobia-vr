using System;
using Acrofobia.Stimuli;

namespace Acrofobia.Suds
{
    public enum SudsTrigger
    {
        Auto,
        Console,
        Local,
        /// <summary>Tutorial rehearsal; never reported.</summary>
        Practice,
    }

    /// <summary>One SUDS answer with the context it was given in. Serialized as the suds.response payload.</summary>
    [Serializable]
    public sealed class SudsRecord
    {
        public string id;
        public int value;
        public int level;
        public string scene;
        public string trigger;
        public long answeredAt;
        public StimulusValues stimuli;

        public static SudsRecord Create(int value, int level, string scene, SudsTrigger trigger, StimulusValues stimuli, DateTimeOffset now)
        {
            return new SudsRecord
            {
                id = Guid.NewGuid().ToString("N"),
                value = value,
                level = level,
                scene = scene,
                trigger = trigger.ToString().ToLowerInvariant(),
                answeredAt = now.ToUnixTimeMilliseconds(),
                stimuli = stimuli,
            };
        }
    }
}
