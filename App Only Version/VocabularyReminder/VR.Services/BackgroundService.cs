using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VocabularyReminder.VR.Utils;
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
                if (playId == 2)
                    _mp3Url = _item.PlayURL;
                else
                    _mp3Url = _item.PlayURL2;

                if (_item?.JsonData?.Source == SourceVocabulary.Oxford.GetDescription() && !string.IsNullOrEmpty(_mp3Url) && !_mp3Url.StartsWith("http"))
                    _mp3Url = "https://www.oxfordlearnersdictionaries.com" + _mp3Url;

                if (!String.IsNullOrEmpty(_mp3Url))
                    _ = Task.Run(() => Mp3Service.PlayFileAsync(_mp3Url));
                else
                    _ = Task.Run(() => TextToSpeechService.SpeakTextAsync(_item.Word));
            }
        }

        public static void HideToast()
        {
            App.isShowPopup = false;
            VocabularyDisplayService.Hide();
        }

        public static async Task NextVocabularyAsync(List<Vocabulary> vocabularies = null)
        {
            BackgroundService.HideToast();
            Vocabulary _item = null;

            if (vocabularies != null && vocabularies.Any())
                _item = GetVocabularyFromExistList(vocabularies, App.GlobalWordId);

            // First, try to get vocabularies due for review
            if (_item == null)
            {
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

        private static Vocabulary GetVocabularyFromExistList(List<Vocabulary> _vocabularies, int currentVocabularyId)
        {
            Vocabulary _item;

            if (App.isRandomWords)
            {
                var random = new Random();
                var index = random.Next(_vocabularies.Count);
                _item = _vocabularies.ElementAt(index);
            }
            else
            {
                var index = _vocabularies.FindIndex(e => e.Id == currentVocabularyId);
                index += 1;
                if (index >= _vocabularies.Count) index = 0;
                _item = _vocabularies.ElementAt(index);
            }

            if (_item == null || _item.Id == 0)
            {
                _item = _vocabularies.FirstOrDefault();
            }

            return _item;
        }

        public static async Task DeleteVocabularyAsync()
        {
            await DataService.UpdateStatusAsync(App.GlobalWordId, 0);
            VocabularyDisplayService.Hide();
        }

        //public static async Task NextAndDeleteVocabulary()
        //{
        //    BackgroundService.HideToast();
        //    await DataService.UpdateStatusAsync(App.GlobalWordId, 0);  // skip this word

        //    Vocabulary _item;
        //    if (App.isRandomWords)
        //    {
        //        _item = await DataService.GetRandomVocabularyAsync(App.GlobalDicId, App.GlobalWordId);
        //    }
        //    else
        //    {
        //        _item = await DataService.GetNextVocabularyAsync(App.GlobalDicId, App.GlobalWordId);
        //    }

        //    if (_item == null || _item.Id == 0)
        //    {
        //        _item = await DataService.GetFirstVocabularyAsync(App.GlobalDicId);
        //    }
        //    App.GlobalWordId = _item != null ? _item.Id : 0;
        //    VocabularyDisplayService.ShowVocabulary(_item);
        //    await DataService.UpdateViewDateAsync(App.GlobalWordId);
        //    if (App.isAutoPlaySounds)
        //    {
        //        await Mp3Service.PlayFileAsync(_item);
        //    }
        //    _item = null;
        //}

        public static async Task ShowCurrentToast()
        {
            var _item = await DataService.GetVocabularyByIdAsync(App.GlobalWordId);
            VocabularyDisplayService.ShowVocabulary(_item);
            await DataService.UpdateViewDateAsync(App.GlobalWordId);
            _item = null;
        }
    }
}
