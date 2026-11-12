// Picks a random line for a topic from the speech library — never the same line twice in a row for that topic.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class SpeechLinePicker
    {
        private readonly Dictionary<SpeechTopic, int> _last = new();

        // False if the library is missing or the topic has no usable line.
        public bool TryPick(SpeechLibrary library, SpeechTopic topic, out string line)
        {
            line = null;
            if (library == null)
                return false;

            var lines = library.LinesFor(topic);
            if (lines.Count == 0)
                return false;

            // NOTE: On a repeat, shift by 1...count−1 — any other line, still uniformly random.
            var index = Random.Range(0, lines.Count);
            if (lines.Count > 1 && _last.TryGetValue(topic, out var last) && index == last)
                index = (last + 1 + Random.Range(0, lines.Count - 1)) % lines.Count;

            _last[topic] = index;
            line = lines[index];
            return !string.IsNullOrWhiteSpace(line);
        }
    }
}