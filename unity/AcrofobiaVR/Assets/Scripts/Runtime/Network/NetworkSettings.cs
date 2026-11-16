using System;
using UnityEngine;

namespace Acrofobia.Network
{
    /// <summary>Where the therapist server lives. Override at launch with <c>-server ws://host:port</c>.</summary>
    [CreateAssetMenu(menuName = "Acrofobia/Network Settings", fileName = "NetworkSettings")]
    public sealed class NetworkSettings : ScriptableObject
    {
        public const string CommandLineFlag = "-server";

        [SerializeField] private string serverUrl = "ws://localhost:8080";
        [Tooltip("Seconds between periodic state reports.")]
        [Min(0.5f)] public float stateIntervalSeconds = 3f;
        [Tooltip("Upper bound for the reconnection back-off, in seconds.")]
        [Min(1f)] public float maxReconnectDelaySeconds = 5f;

        public Uri ServerUri => new(ResolveUrl(serverUrl, System.Environment.GetCommandLineArgs()));

        /// <summary>Returns the value after <see cref="CommandLineFlag"/> if present and valid, else the fallback.</summary>
        public static string ResolveUrl(string fallback, string[] args)
        {
            int index = Array.IndexOf(args, CommandLineFlag);
            if (index >= 0 && index + 1 < args.Length && Uri.TryCreate(args[index + 1], UriKind.Absolute, out var uri)
                && (uri.Scheme == "ws" || uri.Scheme == "wss"))
            {
                return uri.ToString();
            }
            return fallback;
        }
    }
}
