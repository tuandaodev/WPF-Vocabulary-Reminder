using VocabularyReminder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;
using VocabularyReminder.DataAccessLibrary;

namespace DesktopNotifications.Services
{
    public class Mp3
    {
        public static WMPLib.WindowsMediaPlayer Player;

        public static async Task PlayFile(string url)
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
            try
            {
                var directory = Directory.CreateDirectory(ApplicationIO.GetMp3Folder());

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
                //Player.close();
            }
        }

        private static void Player_MediaError(object pMediaObject)
        { 
            Console.WriteLine("Cannot play media file.");
            //Player.close();
        }

        public static void preloadMp3FileSingle(Vocabulary _item)
        {
            Task.Factory.StartNew(() =>
            {
                if (!String.IsNullOrEmpty(_item.PlayURL))
                {
                    Task.Factory.StartNew(async () =>
                    {
                        await DownloadMp3ToDisk(_item.PlayURL);
                    });
                }
                if (!String.IsNullOrEmpty(_item.PlayURL2))
                {
                    Task.Factory.StartNew(async () =>
                    {
                        await DownloadMp3ToDisk(_item.PlayURL2);
                    });
                }
            });
        }


        public static async Task preloadMp3MultipleAsync(Vocabulary _item)
        {
            if (!String.IsNullOrEmpty(_item.PlayURL))
            {
                await DownloadMp3ToDisk(_item.PlayURL);
            }
            if (!String.IsNullOrEmpty(_item.PlayURL2))
            {
                await DownloadMp3ToDisk(_item.PlayURL2);
            }
        }
    }
}
