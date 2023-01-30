using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VocabularyReminder.DataAccessLibrary
{
    class ApplicationIO
    {
        private const string LocalFolder = "VocabularyReminder";
        public const string DatabaseFileName = "vocabulary.db";

        private const string Mp3Folder = "Mp3";
        private const string ImagesFolder = "Images";

        public static string GetApplicationFolderPath()
        {
            return Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), LocalFolder);
        }

        public static string GetMp3Folder()
        {
            return Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), LocalFolder, Mp3Folder);
        }

        public static string GetImageFolder()
        {
            return Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), LocalFolder, ImagesFolder);
        }

        public static string GetChildFolder(string folderName)
        {
            return Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), LocalFolder, folderName);
        }

        public static string GetFilePath(string fileName)
        {
            return Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), LocalFolder, fileName);
        }

        public static string GetDatabasePath()
        {
            return Path.Combine(ApplicationIO.GetApplicationFolderPath(), DatabaseFileName);
        }
    }
}
