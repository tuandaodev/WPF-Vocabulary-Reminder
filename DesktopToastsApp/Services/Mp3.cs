using DataAccessLibrary;
using VocabularyReminderApp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace DesktopNotifications.Services
{
    public class Mp3
    {
        public static WMPLib.WindowsMediaPlayer Player;
        private static bool _hasPerformedCleanup = true;

        public static async void PlayFile(String url)
        {
            if (Player == null)
            {
                Player = new WMPLib.WindowsMediaPlayer();
            }
            
            Player.PlayStateChange += new WMPLib._WMPOCXEvents_PlayStateChangeEventHandler(Player_PlayStateChange);
            Player.MediaError += new WMPLib._WMPOCXEvents_MediaErrorEventHandler(Player_MediaError);
            //Player.URL = url;
            Player.URL = await DownloadMp3ToDisk(url);
            Player.controls.play();
        }

        public static async Task<string> DownloadMp3ToDisk(string Mp3Url)
        {
            // Toasts can live for up to 3 days, so we cache images for up to 3 days.
            // Note that this is a very simple cache that doesn't account for space usage, so
            // this could easily consume a lot of space within the span of 3 days.

            try
            {
                var directory = Directory.CreateDirectory(DataAccess.GetMp3Folder());

                if (!_hasPerformedCleanup)
                {
                    // First time we run, we'll perform cleanup of old images
                    _hasPerformedCleanup = true;

                    //foreach (var d in directory.EnumerateDirectories())
                    //{
                    //    if (d.CreationTimeUtc.Date < DateTime.UtcNow.Date.AddDays(-3))
                    //    {
                    //        d.Delete(true);
                    //    }
                    //}
                }

                string filename = System.IO.Path.GetFileName(Mp3Url);

                string mp3Path = Path.Combine(directory.FullName, filename);
                if (File.Exists(mp3Path))
                {
                    return mp3Path;
                }

                HttpClient c = new HttpClient();
                using (var stream = await c.GetStreamAsync(Mp3Url))
                {
                    using (var fileStream = File.OpenWrite(mp3Path))
                    {
                        stream.CopyTo(fileStream);
                    }
                }

                return mp3Path;
            }
            catch { return ""; }
        }


        private static void Player_PlayStateChange(int NewState)
        {
            if ((WMPLib.WMPPlayState)NewState == WMPLib.WMPPlayState.wmppsStopped)
            {
                Player.close();
            }
        }

        private static void Player_MediaError(object pMediaObject)
        { 
            Console.WriteLine("Cannot play media file.");
            Player.close();
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
