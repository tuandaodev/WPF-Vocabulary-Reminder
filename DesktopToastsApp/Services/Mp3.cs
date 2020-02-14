using DataAccessLibrary;
using DesktopToastsApp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopNotifications.Services
{
    public class Mp3
    {
        public static void play(string rawUrl)
        {
            Task.Factory.StartNew(() =>
            {
                var url = new Uri(rawUrl);
                string filename = System.IO.Path.GetFileName(url.LocalPath);
                if (IsFilePresent(filename))
                {
                    //StorageFolder Folder = ApplicationData.Current.LocalFolder;
                    ////StorageFile sf = await Folder.GetFileAsync(filename);
                    //App.mediaPlayer.Source = MediaSource.CreateFromStorageFile(await ApplicationData.Current.LocalFolder.GetFileAsync(filename));
                    //App.mediaPlayer.Play();
                }
                else
                {
                    //var destinationFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                    //var download = new BackgroundDownloader().CreateDownload(url, destinationFile);
                    ////download.IsRandomAccessRequired = true;
                    //App.mediaPlayer.Source = MediaSource.CreateFromDownloadOperation(download);
                    //App.mediaPlayer.AutoPlay = true;
                    //App.mediaPlayer.Play();
                }
            });
        }

        public static void preloadMp3FileSingle(Vocabulary _item)
        {
            Task.Factory.StartNew(() =>
            {
                if (!String.IsNullOrEmpty(_item.PlayURL))
                {
                    Task.Factory.StartNew(() =>
                    {
                        PreloadMp3Single(_item.PlayURL);
                    });
                }
                if (!String.IsNullOrEmpty(_item.PlayURL2))
                {
                    Task.Factory.StartNew(() =>
                    {
                        PreloadMp3Single(_item.PlayURL2);
                    });
                }
            });
        }

        public static bool IsFilePresent(string fileName)
        {
            if (File.Exists(DataAccess.GetFilePath(fileName)))
            {
                //BasicProperties props = await item.GetBasicPropertiesAsync();
                //if (props.Size == 0)
                //{
                //    await item.DeleteAsync();
                //    return false;
                //}
                return true;
            }

            return false;
        }

        public static void PreloadMp3Single(string mp3RemoteUrl)
        {
            var url = new Uri(mp3RemoteUrl);
            string filename = System.IO.Path.GetFileName(url.LocalPath);
            if (IsFilePresent(filename))
            {
                // do nothing
            }
            else
            {
                //var destinationFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                //var download = new BackgroundDownloader().CreateDownload(url, destinationFile);
                //await download.StartAsync().AsTask();
            }
        }

        public static void preloadMp3Multiple(Vocabulary _item)
        {
            if (!String.IsNullOrEmpty(_item.PlayURL))
            {
                PreloadMp3Single(_item.PlayURL);
            }
            if (!String.IsNullOrEmpty(_item.PlayURL2))
            {
                PreloadMp3Single(_item.PlayURL2);
            }
        }
    }
}
