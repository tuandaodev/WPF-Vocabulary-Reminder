using System;

namespace VR.Utils
{
    public class Helper
    {
        public static string GetCambridgeWordUrl(string word)
        {
            string parseWord = word.Trim().Replace(" ", "-");
            return $"https://dictionary.cambridge.org/vi/dictionary/english-vietnamese/{Uri.EscapeDataString(parseWord.ToLower())}";
        }

        public static string GetOxfordWordUrl(string word)
        {
            string parseWord = word.Trim().Replace(" ", "-");
            return $"https://www.oxfordlearnersdictionaries.com/definition/english/{Uri.EscapeDataString(parseWord.ToLower())}";
        }

        public static string GetGoogleTranslateUrl(string word)
        {
            string parseWord = word.Trim();
            return $"https://translate.google.com/?sl=en&tl=vi&text={Uri.EscapeDataString(parseWord)}&op=translate";
        }

        public static string GetYouGlishUrl(string word)
        {
            string parseWord = word.Trim();
            return $"https://youglish.com/pronounce/{Uri.EscapeDataString(parseWord)}/us";
        }

        /// <summary>
        /// Converts full grammatical type names to their short forms
        /// </summary>
        /// <param name="type">The full type name (e.g., "noun", "verb")</param>
        /// <returns>Short form of the type (e.g., "N", "V") or the original value if no mapping exists</returns>
        public static string GetShortFormType(string type)
        {
            if (string.IsNullOrEmpty(type))
                return "";

            // Convert to lowercase for case-insensitive matching
            string lowerType = type.ToLower().Trim();

            switch (lowerType)
            {
                case "noun": return "Noun";
                case "verb": return "Verb";
                case "adjective": return "Adj";
                case "adverb": return "Adv";
                case "pronoun": return "Pron";
                case "preposition": return "Prep";
                case "conjunction": return "Conj";
                case "interjection": return "Inter";
                case "article": return "Art";
                case "determiner": return "Det";
                case "modal": return "Mod";
                case "auxiliary": return "Aux";
                case "participle": return "Part";
                case "gerund": return "Ger";
                case "infinitive": return "Inf";
                case "phrasal verb": return "PhV";
                case "idiom": return "Id";
                case "exclamation": return "Excl";
                case "abbreviation": return "Abbr";
                case "proper noun": return "PN";
                case "countable noun": return "C";
                case "uncountable noun": return "U";
                case "transitive verb": return "Vt";
                case "intransitive verb": return "Vi";
                case "linking verb": return "Vl";
                case "sentence": return "S";
                // If no mapping found, return the original type
                default: return type;
            }
        }
    }
}
