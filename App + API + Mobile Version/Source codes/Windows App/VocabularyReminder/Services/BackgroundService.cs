﻿using DesktopNotifications.Services;
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
        public static async Task ActionPlayAsync(int playId = 1)
        {
            Vocabulary _item;
            if (App.GlobalWordId > 0)
            {
                string _mp3Url;
                _item =  await DataAccess.GetVocabularyByIdAsync(App.GlobalWordId);
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
                    await Task.Run(async () => await Mp3.PlayFile(_mp3Url));
                }
            }
        }

        public static void HideToast()
        {
            App.isShowPopup = false;
            VocabularyToast.ClearApplicationToast();
        }

        public static async Task NextVocabularyAsync()
        {
            Vocabulary _item;
            if (App.isRandomWords)
            {
                _item = await DataAccess.GetRandomVocabularyAsync(App.GlobalWordId);
            }
            else
            {
                _item = await DataAccess.GetNextVocabularyAsync(App.GlobalWordId);
            }

            if (_item.Id == 0)
            {
                _item = await DataAccess.GetFirstVocabularyAsync();
            }
            App.GlobalWordId = _item.Id;
            VocabularyToast.ShowToastByVocabularyItem(_item);
            _item = null;
        }

        public static async Task DeleteVocabularyAsync()
        {
            await DataAccess.UpdateStatusAsync(App.GlobalWordId, 0);
            VocabularyToast.ClearApplicationToast();
        }

        public static async Task NextAndDeleteVocabularyAsync()
        {
            await DataAccess.UpdateStatusAsync(App.GlobalWordId, 0);  // skip this word

            Vocabulary _item;
            if (App.isRandomWords)
            {
                _item = await DataAccess.GetRandomVocabularyAsync(App.GlobalWordId);
            }
            else
            {
                _item = await DataAccess.GetNextVocabularyAsync(App.GlobalWordId);
            }

            if (_item.Id == 0)
            {
                _item = await DataAccess.GetFirstVocabularyAsync();
            }
            App.GlobalWordId = _item.Id;
            VocabularyToast.ShowToastByVocabularyItem(_item);
            _item = null;
        }

        public static async Task showCurrentToastAsync()
        {
            var _item = await DataAccess.GetVocabularyByIdAsync(App.GlobalWordId);
            VocabularyToast.ShowToastByVocabularyItem(_item);
            _item = null;
        }
    }
}
