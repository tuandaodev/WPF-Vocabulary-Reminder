using System.Collections.Generic;
using System.IO;

namespace VocabularyReminder.DataAccessLibrary
{
    public class StaticDataAccess
    {
        public static (HashSet<string>, int) ReadDictionaryCSV(string filePath)
        {
            var dictionary = new HashSet<string>();
            int maxWordLength = 0;

            using (var reader = new StreamReader(filePath))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line != null)
                    {
                        var values = line.Split(',');
                        if (values.Length > 0)
                        {
                            string word = values[0].Trim().ToLower();
                            dictionary.Add(word);
                            int wordLength = word.Split(' ').Length;
                            if (wordLength > maxWordLength)
                            {
                                maxWordLength = wordLength;
                            }
                        }
                    }
                }
            }

            return (dictionary, maxWordLength);
        }
    }
}
