using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using VocabularyReminder.DataAccessLibrary;

namespace VocabularyReminder.Services
{

    public class IPA {
    
        private Dictionary<string, string> dictionary;

        public IPA () {

            this.dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var filePath = ApplicationIO.GetIPACSV();
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"IPA dictionary file not found at {filePath}");
            }

            using(var reader = new StreamReader(filePath)) {
                string line = null;
                while ((line = reader.ReadLine()) != null) {
                    var parts = line.Split(',', (char)2);
                    if (parts.Length == 2)
                        dictionary[parts[0].Trim()] = parts[1].Trim();
                }
            }
            
        }

        public IPA (Dictionary<string, string> dictionary) {
            this.dictionary = dictionary;
        }

        public string EnglishToIPA(string text) {
            var builder = new StringBuilder();
            string[] words = Regex.Split(text, @"([\s\p{P}])"); // Split on spaces or punctuation

            foreach (var match in words) {
                var lower = match.ToLower();
                if (dictionary.ContainsKey(lower)) {
                    builder.Append(dictionary[lower]);
                } else {
                    builder.Append(lower);
                }
            }
            return builder.ToString();
        }

    }
}
