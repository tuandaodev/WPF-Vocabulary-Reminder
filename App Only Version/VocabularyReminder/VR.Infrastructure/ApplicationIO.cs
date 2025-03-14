﻿using System;
using System.IO;

namespace VR.Infrastructure
{
    public class ApplicationIO
    {
        private const string LocalFolder = "VocabularyReminder";
        public const string DatabaseFileName = "vocabulary.db";

        private const string Mp3Folder = "Mp3";
        private const string ImagesFolder = "Images";
        private const string SelfData = "Data";

        public static string GetDictionaryCSV()
        {
            return Path.Combine(SelfData, "dictionary.csv");
        }

        public static string GetIPACSV()
        {
            return Path.Combine(SelfData, "IPA.txt");
        }

        public static string GetApplicationFolderPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), LocalFolder);
        }

        public static string GetMp3Folder()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), LocalFolder, Mp3Folder);
        }

        public static string GetImageFolder()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), LocalFolder, ImagesFolder);
        }

        public static string GetChildFolder(string folderName)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), LocalFolder, folderName);
        }

        public static string GetFilePath(string fileName)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), LocalFolder, fileName);
        }

        public static string GetDatabasePath()
        {
            return Path.Combine(GetApplicationFolderPath(), DatabaseFileName);
        }

        public static string GetEVDatabasePath()
        {
            return Path.Combine(SelfData, "dict_ev.db");
        }

        public static string GetSettingsPath()
        {
            return Path.Combine(GetApplicationFolderPath(), "settings.json");
        }
    }
}
