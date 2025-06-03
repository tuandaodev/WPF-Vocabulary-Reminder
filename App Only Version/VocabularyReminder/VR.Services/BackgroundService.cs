using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VocabularyReminder.VR.Common;
using VocabularyReminder.VR.Utils;
using VR.Domain.Models;
using VR.Utils;

namespace VR.Services
{
    public class BackgroundService
    {
        public static async Task ActionPlay(ActionPlayEnum playId = ActionPlayEnum.US)
        {
            Vocabulary _item;
            if (App.GlobalWordId > 0)
            {
                string _mp3Url;
                _item = await DataService.GetVocabularyByIdAsync(App.GlobalWordId);
                if (_item == null)
                    return;

                _mp3Url = playId == ActionPlayEnum.US ? _item.PlayURL2 : _item.PlayURL;
                if (!string.IsNullOrEmpty(App.GlobalJsonDataId))
                {
                    var currentJsonData = _item?.JsonData?.FirstOrDefault(e => e.ID == App.GlobalJsonDataId);
                    if (currentJsonData != null && !string.IsNullOrEmpty(currentJsonData.Audio)
                        && !string.IsNullOrEmpty(currentJsonData.Audio2))
                    {
                        _mp3Url = playId == ActionPlayEnum.US ? currentJsonData.Audio2 : currentJsonData.Audio;
                    }
                }

                if (_item?.JsonData.FirstOrDefault()?.Source == SourceVocabulary.Oxford.GetDescription() && !string.IsNullOrEmpty(_mp3Url) && !_mp3Url.StartsWith("http"))
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

        private static readonly Random _random = new Random();

        public static async Task NextVocabularyAsync(List<Vocabulary> vocabularies = null)
        {
            BackgroundService.HideToast();
            Vocabulary _item = null;

            // If vocabularies list is provided, use it first
            if (vocabularies?.Any() == true)
            {
                _item = GetVocabularyFromExistList(vocabularies, App.GlobalWordId);
            }
            else
            {
                // Try to get vocabulary due for review
                _item = await SpacedRepetitionService.LoadVocabulariesForReview(App.GlobalDicId);

                // If no due vocabulary, get next vocabulary
                if (_item == null)
                {
                    _item = App.isRandomWords
                        ? await DataService.GetRandomVocabularyAsync(App.GlobalDicId, App.GlobalWordId)
                        : await DataService.GetNextVocabularyAsync(App.GlobalDicId, App.GlobalWordId);
                    // Fallback to first vocabulary if needed
                    if (_item?.Id == 0)
                    {
                        _item = await DataService.GetFirstVocabularyAsync(App.GlobalDicId);
                    }
                }
            }

            // Update state if vocabulary was found
            if (_item?.Id > 0)
            {
                App.GlobalWordId = _item.Id;
                VocabularyDisplayService.ShowVocabulary(_item);
                
                if (App.isAutoPlaySounds)
                {
                    await Mp3Service.PlayFileAsync(_item);
                }

                _item.ViewedDate = DateTime.Now.ToUnixTimeInSeconds();
                await DataService.UpdateViewDateAsync(App.GlobalWordId);
            }
            else
            {
                App.GlobalWordId = 0;
                System.Windows.MessageBox.Show("No vocabulary found.", "Information", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        private static Vocabulary GetVocabularyFromExistList(List<Vocabulary> _vocabularies, int currentVocabularyId)
        {
            if (!_vocabularies.Any()) return null;

            if (App.isRandomWords)
            {
                return _vocabularies[_random.Next(_vocabularies.Count)];
            }

            var index = _vocabularies.FindIndex(e => e.Id == currentVocabularyId);
            index = (index + 1) % _vocabularies.Count; // Wrap around to beginning if needed
            return _vocabularies[index];
        }

        public static async Task DeleteVocabularyAsync()
        {
            await DataService.UpdateStatusAsync(App.GlobalWordId, 0);
            VocabularyDisplayService.Hide();
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
