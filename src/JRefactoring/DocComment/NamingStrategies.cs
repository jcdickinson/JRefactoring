using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JRefactoring.DocComment
{
    public static class NamingStrategies
    {
        private static readonly Dictionary<string, string> _acronyms = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase)
        {
            { "XML", "XML" }
        };

        private static readonly HashSet<char> _vowels = new HashSet<char>()
        {
            'a', 'e', 'i', 'o', 'u'
        };

        private static readonly HashSet<char> _consonants = new HashSet<char>()
        {
            'b', 'c', 'd', 'f', 'g', 'h', 'j', 'k', 'l', 'm', 'n', 'p', 'q', 'r', 's', 't', 'v', 'w', 'x', 'y', 'z'
        };

        private static readonly HashSet<char> _acronymVowel = new HashSet<char>()
        {
            'M', 'N', 'S'
        };

        public static string CheckAcronym(string value)
        {
            if (_acronyms.TryGetValue(value, out var acronym)) return acronym;
            return value;
        }

        public static bool StartsWithWord(string name, string word)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (name.Length <= word.Length) return false; // Don't check for equality.
            if (!name.StartsWith(word, StringComparison.CurrentCultureIgnoreCase)) return false;
            var c = name[word.Length];

            return char.IsUpper(c) || c == '_';
        }

        public static IEnumerable<string> Split(string name, bool capitalize)
        {
            if (string.IsNullOrEmpty(name)) yield break;

            var sb = new StringBuilder(name.Length);
            var i = name[0] == '@' ? 1 : 0;
            var lastWasLower = false;

            for (; i < name.Length; i++)
            {
                var c = name[i];
                if (name[i] == '_')
                {
                    if (sb.Length > 0) yield return CheckAcronym(sb.ToString());
                    sb.Clear();
                }
                else if (char.IsUpper(c) && lastWasLower)
                {
                    if (sb.Length > 0) yield return CheckAcronym(sb.ToString());
                    sb.Clear();
                    sb.Append(char.ToLower(c));
                }
                else
                {
                    if (capitalize) sb.Append(char.ToUpper(c));
                    else sb.Append(char.ToLower(c));
                    capitalize = false;
                }

                lastWasLower = char.IsLower(c);
            }

            if (sb.Length > 0)
                yield return CheckAcronym(sb.ToString());
        }

        public static string PastTense(string presentTense)
        {
            if (string.IsNullOrEmpty(presentTense)) return presentTense;
            if (presentTense.EndsWith("ed")) return presentTense;

            if (presentTense.EndsWith("e", StringComparison.CurrentCulture))
                return $"{presentTense}d";
            else if (presentTense.Length > 2 &&
                _consonants.Contains(presentTense[presentTense.Length - 3]) &&
                _vowels.Contains(presentTense[presentTense.Length - 2]) &&
                _consonants.Contains(presentTense[presentTense.Length - 1]))
            {
                var lastConsonant = presentTense[presentTense.Length - 1];

                // L: US rules
                if (lastConsonant == 'w' || lastConsonant == 'x' || lastConsonant == 'y' || lastConsonant == 'l')
                    return $"{presentTense}ed";
                else
                    return $"{presentTense}{presentTense[presentTense.Length - 1]}ed";
            }

            return $"{presentTense}ed";
        }

        public static int SyllableCount(string word)
        {
            if (string.IsNullOrEmpty(word)) return 0;

            var lastWasVowel = false;
            var count = 0;

            for (var i = 0; i < word.Length; i++)
            {
                var c = word[i];
                if (_vowels.Contains(char.ToLower(c)))
                {
                    if (!lastWasVowel) count++;
                    lastWasVowel = true;
                }
                else
                {
                    lastWasVowel = false;
                }
            }

            if ((word.EndsWith("e") || (word.EndsWith("es") || word.EndsWith("ed")))
                  && !word.EndsWith("le"))
                count--;

            return count;
        }

        public static string AorAn(string word)
        {
            if (string.IsNullOrEmpty(word)) return "a";

            var caps = true;
            for (var i = 0; i < word.Length; i++)
                caps &= !char.IsLetter(word[i]) || char.IsUpper(word[i]);
            if (caps) return _acronymVowel.Contains(word[0]) ? "an" : "a";

            return word[0] != 'u' && _vowels.Contains(char.ToLower(word[0]))
                ? "an"
                : "a";
        }
    }
}
