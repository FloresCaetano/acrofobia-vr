using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Acrofobia.Suds
{
    /// <summary>
    /// SUDS answers waiting for the server's acknowledgement. Saved to disk on every change, so answers
    /// given while offline (or right before a crash) are delivered on the next connection. The server
    /// ignores duplicate ids, so resending is always safe.
    /// </summary>
    public sealed class SudsOutbox
    {
        private readonly string filePath;
        private readonly List<Entry> entries = new();

        public SudsOutbox(string filePath)
        {
            this.filePath = filePath;
            Load();
        }

        public int Count => entries.Count;
        public IReadOnlyList<Entry> Pending => entries;

        /// <param name="sentOffline">True when the answer could not be sent right away; the console then labels it as late.</param>
        public void Add(SudsRecord record, bool sentOffline)
        {
            entries.Add(new Entry { record = record, queued = sentOffline });
            Save();
        }

        /// <summary>Marks every pending answer as delivered late (called when the connection drops).</summary>
        public void MarkAllQueued()
        {
            bool changed = false;
            foreach (var entry in entries)
            {
                changed |= !entry.queued;
                entry.queued = true;
            }
            if (changed) Save();
        }

        public bool Acknowledge(string id)
        {
            int removed = entries.RemoveAll(entry => entry.record.id == id);
            if (removed > 0) Save();
            return removed > 0;
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(filePath)) return;
                var stored = JsonUtility.FromJson<Stored>(File.ReadAllText(filePath));
                if (stored?.entries != null) entries.AddRange(stored.entries);
            }
            catch (Exception exception) when (exception is IOException or ArgumentException)
            {
                Debug.LogWarning($"[SudsOutbox] Could not read {filePath}: {exception.Message}");
            }
        }

        private void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                string temporary = filePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(new Stored { entries = entries.ToArray() }));
                if (File.Exists(filePath)) File.Delete(filePath);
                File.Move(temporary, filePath);
            }
            catch (IOException exception)
            {
                Debug.LogWarning($"[SudsOutbox] Could not save {filePath}: {exception.Message}");
            }
        }

        [Serializable]
        public sealed class Entry
        {
            public SudsRecord record;
            public bool queued;
        }

        [Serializable]
        private sealed class Stored
        {
            public Entry[] entries;
        }
    }
}
