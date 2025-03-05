using System;
using System.Threading.Tasks;
using VR.Domain.Models;
using VR.Utils;

namespace VR.Services
{
    public class BackgroundService
    {
        public static async Task ActionPlay(int playId = 1)
        {
            Vocabulary _item;
            if (App.GlobalWordId > 0)
            {
                string _mp3Url;
                _item = await DataService.GetVocabularyByIdAsync(App.GlobalWordId);
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
                    _ = Task.Run(() => Mp3Service.PlayFileAsync(_mp3Url));
                }
            }
        }

        public static void HideToast()
        {
            App.isShowPopup = false;
            VocabularyDisplayService.Hide();
        }

        public static async Task NextVocabularyAsync()
        {
            BackgroundService.HideToast();
            Vocabulary _item = null;

            // First, try to get vocabularies due for review
            var dueVocabs = await SpacedRepetitionService.LoadVocabulariesForReview(App.GlobalDicId);
            if (dueVocabs != null && dueVocabs.Count > 0)
            {
                // If random mode is on, pick a random vocabulary from due items
                if (App.isRandomWords)
                {
                    Random rnd = new Random();
                    _item = dueVocabs[rnd.Next(dueVocabs.Count)];
                }
                else
                {
                    // Take the first due item (oldest review date)
                    _item = dueVocabs[0];
                }
            }

            // If no due vocabularies, fall back to normal behavior
            if (_item == null)
            {
                if (App.isRandomWords)
                {
                    _item = await DataService.GetRandomVocabularyAsync(App.GlobalDicId, App.GlobalWordId);
                }
                else
                {
                    _item = await DataService.GetNextVocabularyAsync(App.GlobalDicId, App.GlobalWordId);
                }

                if (_item == null || _item.Id == 0)
                {
                    _item = await DataService.GetFirstVocabularyAsync(App.GlobalDicId);
                }
            }

            if (_item != null)
            {
                App.GlobalWordId = _item.Id;
                VocabularyDisplayService.ShowVocabulary(_item);
                if (App.isAutoPlaySounds)
                    await Mp3Service.PlayFileAsync(_item);

                _item.ViewedDate = DateTime.Now.ToUnixTimeInSeconds();
                await DataService.UpdateViewDateAsync(App.GlobalWordId);
            }
            else
            {
                App.GlobalWordId = 0;
            }
        }

        public static async Task DeleteVocabularyAsync()
        {
            await DataService.UpdateStatusAsync(App.GlobalWordId, 0);
            VocabularyDisplayService.Hide();
        }

        public static async Task NextAndDeleteVocabulary()
        {
            BackgroundService.HideToast();
            await DataService.UpdateStatusAsync(App.GlobalWordId, 0);  // skip this word

            Vocabulary _item;
            if (App.isRandomWords)
            {
                _item = await DataService.GetRandomVocabularyAsync(App.GlobalDicId, App.GlobalWordId);
            }
            else
            {
                _item = await DataService.GetNextVocabularyAsync(App.GlobalDicId, App.GlobalWordId);
            }

            if (_item == null || _item.Id == 0)
            {
                _item = await DataService.GetFirstVocabularyAsync(App.GlobalDicId);
            }
            App.GlobalWordId = _item != null ? _item.Id : 0;
            VocabularyDisplayService.ShowVocabulary(_item);
            await DataService.UpdateViewDateAsync(App.GlobalWordId);
            if (App.isAutoPlaySounds)
            {
                await Mp3Service.PlayFileAsync(_item);
            }
            _item = null;
        }

        public static async Task ShowCurrentToast()
        {
            var _item = await DataService.GetVocabularyByIdAsync(App.GlobalWordId);
            VocabularyDisplayService.ShowVocabulary(_item);
            await DataService.UpdateViewDateAsync(App.GlobalWordId);
            _item = null;
        }
    }
}
