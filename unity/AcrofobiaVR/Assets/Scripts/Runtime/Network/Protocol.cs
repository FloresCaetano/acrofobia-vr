using System;
using Acrofobia.Stimuli;
using UnityEngine;

namespace Acrofobia.Network
{
    /// <summary>
    /// Messages of the form <c>{ "type", "payload", "ts" }</c> (see docs/PROTOCOL.md), serialized with
    /// JsonUtility: the header is read first, then the payload with the type the header names.
    /// </summary>
    public static class Protocol
    {
        public const string Hello = "hello";
        public const string Error = "error";
        public const string SessionStart = "session.start";
        public const string SessionEnd = "session.end";
        public const string LevelSet = "level.set";
        public const string StimulusSet = "stimulus.set";
        public const string CalmTrigger = "calm.trigger";
        public const string SudsRequest = "suds.request";
        public const string SudsConfig = "suds.config";
        public const string SudsResponse = "suds.response";
        public const string SudsAck = "suds.ack";
        public const string StateUpdate = "state.update";

        public static string Serialize<T>(string type, T payload) =>
            JsonUtility.ToJson(new Envelope<T> { type = type, payload = payload, ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });

        /// <summary>Reads only the message type. Returns null for malformed JSON.</summary>
        public static string ReadType(string json)
        {
            try
            {
                return JsonUtility.FromJson<Header>(json)?.type;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public static T ReadPayload<T>(string json) => JsonUtility.FromJson<Envelope<T>>(json).payload;

        [Serializable]
        private sealed class Header
        {
            public string type;
        }

        [Serializable]
        private sealed class Envelope<T>
        {
            public string type;
            public T payload;
            public long ts;
        }
    }

    [Serializable]
    public sealed class LevelInfo
    {
        public int number;
        public string name;
        public string description;
    }

    [Serializable]
    public sealed class HelloPayload
    {
        public string role = "vr";
        public string client;
        public string version;
        public LevelInfo[] levels;
    }

    [Serializable]
    public sealed class LevelSetPayload
    {
        public int level;
    }

    [Serializable]
    public sealed class StimulusSetPayload
    {
        public string stimulus;
        public float value;
    }

    [Serializable]
    public sealed class SudsConfigPayload
    {
        public float intervalSeconds;
    }

    [Serializable]
    public sealed class IdPayload
    {
        public string id;
    }

    [Serializable]
    public sealed class SourcePayload
    {
        public string source;
    }

    [Serializable]
    public sealed class SessionPayload
    {
        public string id;
        public string patientAlias;
    }

    [Serializable]
    public sealed class ErrorPayload
    {
        public string code;
        public string message;
    }

    [Serializable]
    public sealed class StatePayload
    {
        public string scene;
        public int level;
        public string levelName;
        public StimulusValues stimuli;
        public StimulusValues targets;
        public float sudsIntervalSeconds;
        public bool transitioning;
        public bool sudsOpen;
    }

    [Serializable]
    public sealed class SudsResponsePayload
    {
        public string id;
        public int value;
        public int level;
        public string scene;
        public string trigger;
        public long answeredAt;
        public bool queued;
        public StimulusValues stimuli;
    }
}
