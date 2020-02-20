using DesktopNotifications.Services;
using Microsoft.QueryStringDotNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VocabularyReminder.DataAccessLibrary;

namespace VocabularyReminder.Services
{
    class BackgroundService
    {
        public static void ActionPlay()
        {
            Vocabulary _item;
            int playId = 1;
            if (App.GlobalWordId > 0)
            {
                string _mp3Url;
                _item = DataAccess.GetVocabularyById(App.GlobalWordId);
                //VocabularyToast.showToastByVocabularyItem(_item);
                if (playId == 2)
                {
                    _mp3Url = _item.PlayURL2;
                }
                else
                {
                    _mp3Url = _item.PlayURL;
                }

                if (!String.IsNullOrEmpty(_mp3Url))
                {
                    Task.Run(() => Mp3.PlayFile(_mp3Url));
                }
            }
        }
    }
}
