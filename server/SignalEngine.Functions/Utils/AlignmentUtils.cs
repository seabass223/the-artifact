using SignalEngine.Functions.Models;

namespace SignalEngine.Functions.Utils;

public record WordTiming(string Word, double StartTime, double EndTime);

public static class AlignmentUtils
{
    /// <summary>
    /// Rolls up character-level ElevenLabs alignment into word-level timings.
    /// Words are delimited by spaces; punctuation attached to a word is included in it.
    /// </summary>
    public static List<WordTiming> ToWordTimings(
        string[] characters,
        double[] startTimes,
        double[] endTimes
    )
    {
        var words = new List<WordTiming>();
        var wordChars = new System.Text.StringBuilder();
        double wordStart = 0;
        double wordEnd = 0;

        for (int i = 0; i < characters.Length; i++)
        {
            var ch = characters[i];

            if (ch == " ")
            {
                if (wordChars.Length > 0)
                {
                    words.Add(new WordTiming(wordChars.ToString(), wordStart, wordEnd));
                    wordChars.Clear();
                }
            }
            else
            {
                if (wordChars.Length == 0)
                    wordStart = startTimes[i];

                wordChars.Append(ch);
                wordEnd = endTimes[i];
            }
        }

        // Flush the last word
        if (wordChars.Length > 0)
            words.Add(new WordTiming(wordChars.ToString(), wordStart, wordEnd));

        return words;
    }

    /// <summary>
    /// Cross-references a list of <see cref="NarrativeClassification"/> (text-only) with word-level
    /// timings to attach startTimeSeconds / endTimeSeconds to each section.
    /// Matching is done by finding the first and last word of each section's text span.
    /// </summary>
    public static List<NarrativeClassification> AttachTimings(
        List<NarrativeClassification> classifications,
        List<WordTiming> wordTimings
    )
    {
        // Build a flat list of word timings in order — we'll walk through it as we consume sections.
        // Split any tokens that contain embedded newlines (e.g. "systems.\n\nIn") so alignment
        // isn't thrown off by ElevenLabs combining cross-paragraph words into one token.
        var flatTimings = new List<WordTiming>(wordTimings.Count);
        foreach (var wt in wordTimings)
        {
            var parts = wt.Word.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length <= 1)
            {
                flatTimings.Add(wt);
            }
            else
            {
                // Distribute the timing window evenly across parts
                double span = (wt.EndTime - wt.StartTime) / parts.Length;
                for (int p = 0; p < parts.Length; p++)
                    flatTimings.Add(
                        new WordTiming(
                            parts[p],
                            wt.StartTime + p * span,
                            wt.StartTime + (p + 1) * span
                        )
                    );
            }
        }

        int wordIndex = 0;
        var result = new List<NarrativeClassification>(classifications.Count);

        foreach (var section in classifications)
        {
            // Tokenise the section text the same way ToWordTimings does (split on spaces)
            var sectionWords = section
                .Text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToArray();

            if (sectionWords.Length == 0)
            {
                result.Add(section);
                continue;
            }

            // Advance wordIndex until we find the first word of this section
            while (
                wordIndex < flatTimings.Count
                && !WordMatches(flatTimings[wordIndex].Word, sectionWords[0])
            )
                wordIndex++;

            if (wordIndex >= flatTimings.Count)
            {
                result.Add(section);
                continue;
            }

            double start = flatTimings[wordIndex].StartTime;

            // Advance through all words in this section
            int sectionWordIndex = 0;
            int lastMatchedIndex = wordIndex;

            while (wordIndex < flatTimings.Count && sectionWordIndex < sectionWords.Length)
            {
                if (WordMatches(flatTimings[wordIndex].Word, sectionWords[sectionWordIndex]))
                {
                    lastMatchedIndex = wordIndex;
                    sectionWordIndex++;
                }
                wordIndex++;
            }

            double end = flatTimings[lastMatchedIndex].EndTime;

            result.Add(section with { StartTimeSeconds = start, EndTimeSeconds = end });
        }

        return result;
    }

    // Strip punctuation from both sides for a loose match
    private static bool WordMatches(string timedWord, string sectionWord)
    {
        static string Strip(string w) =>
            w.Trim('.', ',', '!', '?', ';', ':', '"', '\'', '\u2014', '\u2013', '\u2026');
        return string.Equals(
            Strip(timedWord),
            Strip(sectionWord),
            StringComparison.OrdinalIgnoreCase
        );
    }
}
