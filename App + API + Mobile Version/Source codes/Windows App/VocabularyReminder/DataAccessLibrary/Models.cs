﻿using System.Collections.Generic;

namespace VocabularyReminder.DataAccessLibrary
{
    public enum ShowListType
    {
        SHOW_ALL = 0,
        NEW_WORD,
        LEARN_WORD
    }

    public class Dictionary
    {
        public int DictionaryId { get; set; }
        public string Name { get; set; }

        public List<Vocabulary> Words { get; } = new List<Vocabulary>();
    }

    public class Stats
    {
        public int Total { get; set; }
        public int Remembered { get; set; }
    }

    public class Vocabulary
    {
        public int Id { get; set; }
        public string Word { get; set; }
        public string Type { get; set; }
        public string Ipa { get; set; }
        public string Ipa2 { get; set; }
        public string Translate { get; set; }
        public string Define { get; set; }
        public string Example { get; set; }
        public string Example2 { get; set; }
        public string PlayURL { get; set; }
        public string PlayURL2 { get; set; }
        public string Related { get; set; }
        public int Status { get; set; }
        public int Idx { get; set; }
        public string UserId { get; set; }
    }
}