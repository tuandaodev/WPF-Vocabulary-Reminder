﻿using DesktopNotifications.Services;
using System;
using System.Threading.Tasks;
using VocabularyReminder.DataAccessLibrary;
using VocabularyReminder.Utils;

namespace VocabularyReminder.Services
{
    public class BackgroundService
    {
        public static async Task ActionPlay(int playId = 1)
        {
            Vocabulary _item;
            if (App.GlobalWordId > 0)
            {
                string _mp3Url;
                _item = await DataAccess.GetVocabularyByIdAsync(App.GlobalWordId);
                //VocabularyToast.showToastByVocabularyItem(_item);
                if (playId == 2)
                {
                    _mp3Url = _item.PlayURL;
                }
                else
                {
                    _mp3Url = _item.PlayURL2;
                }

                if (!String.IsNullOrEmpty(_mp3Url))
                {
                    _ = Task.Run(() => Mp3.PlayFile(_mp3Url));
                }
            }
        }

        public static void HideToast()
        {
            App.isShowPopup = false;
            VocabularyDisplay.Hide();
        }

        public static async Task NextVocabulary()
        {
            BackgroundService.HideToast();
            Vocabulary _item;
            if (App.isRandomWords)
            {
                _item = await DataAccess.GetRandomVocabularyAsync(App.GlobalDicId, App.GlobalWordId);
            }
            else
            {
                _item = await DataAccess.GetNextVocabularyAsync(App.GlobalDicId, App.GlobalWordId);
            }

            if (_item == null || _item.Id == 0)
            {
                _item = await DataAccess.GetFirstVocabularyAsync(App.GlobalDicId);
            }
            App.GlobalWordId = _item != null ? _item.Id : 0;
            VocabularyDisplay.ShowVocabulary(_item);
            if (App.isAutoPlaySounds)
            {
                await Mp3.PlayFile(_item);

                _item.ViewedDate = DateTime.Now.ToUnixTimeInSeconds();
                await DataAccess.UpdateViewDateAsync(App.GlobalWordId);
            }
            _item = null;
        }

        public static async Task DeleteVocabularyAsync()
        {
            await DataAccess.UpdateStatusAsync(App.GlobalWordId, 0);
            VocabularyDisplay.Hide();
        }

        public static async Task NextAndDeleteVocabulary()
        {
            BackgroundService.HideToast();
            await DataAccess.UpdateStatusAsync(App.GlobalWordId, 0);  // skip this word

            Vocabulary _item;
            if (App.isRandomWords)
            {
                _item = await DataAccess.GetRandomVocabularyAsync(App.GlobalDicId, App.GlobalWordId);
            }
            else
            {
                _item = await DataAccess.GetNextVocabularyAsync(App.GlobalDicId, App.GlobalWordId);
            }

            if (_item == null || _item.Id == 0)
            {
                _item = await DataAccess.GetFirstVocabularyAsync(App.GlobalDicId);
            }
            App.GlobalWordId = _item != null ? _item.Id : 0;
            VocabularyDisplay.ShowVocabulary(_item);
            if (App.isAutoPlaySounds)
            {
                await Mp3.PlayFile(_item);
                await DataAccess.UpdateViewDateAsync(App.GlobalWordId);
            }
                              
            _item = null;
        }

        public static async Task ShowCurrentToast()
        {
            var _item = await DataAccess.GetVocabularyByIdAsync(App.GlobalWordId);
            VocabularyDisplay.ShowVocabulary(_item);
            await DataAccess.UpdateViewDateAsync(App.GlobalWordId);
            _item = null;
        }
    }
}
