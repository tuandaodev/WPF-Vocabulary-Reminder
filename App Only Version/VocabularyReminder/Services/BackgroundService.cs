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
        public static void ActionPlay(int playId = 1)
        {
            Vocabulary _item;
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

        public static void HideToast()
        {
            App.isShowPopup = false;
            VocabularyToast.ClearApplicationToast();
        }

        public static void NextVocabulary()
        {
            Vocabulary _item;
            if (App.isRandomWords)
            {
                _item = DataAccess.GetRandomVocabulary(App.GlobalWordId);
            }
            else
            {
                _item = DataAccess.GetNextVocabulary(App.GlobalWordId);
            }

            if (_item.Id == 0)
            {
                _item = DataAccess.GetFirstVocabulary();
            }
            App.GlobalWordId = _item.Id;
            VocabularyToast.ShowToastByVocabularyItem(_item);
            _item = null;
        }

        public static void DeleteVocabulary()
        {
            DataAccess.UpdateStatus(App.GlobalWordId, 0);
            VocabularyToast.ClearApplicationToast();
        }

        public static void NextAndDeleteVocabulary()
        {
            DataAccess.UpdateStatus(App.GlobalWordId, 0);  // skip this word

            Vocabulary _item;
            if (App.isRandomWords)
            {
                _item = DataAccess.GetRandomVocabulary(App.GlobalWordId);
            }
            else
            {
                _item = DataAccess.GetNextVocabulary(App.GlobalWordId);
            }

            if (_item.Id == 0)
            {
                _item = DataAccess.GetFirstVocabulary();
            }
            App.GlobalWordId = _item.Id;
            VocabularyToast.ShowToastByVocabularyItem(_item);
            _item = null;
        }

        public static void showCurrentToast()
        {
            var _item = DataAccess.GetVocabularyById(App.GlobalWordId);
            VocabularyToast.ShowToastByVocabularyItem(_item);
            _item = null;
        }
    }
}
