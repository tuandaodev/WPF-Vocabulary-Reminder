using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VR.Domain.Models;

namespace VR.Services
{
    public class SyncVocaService
    {
        public static async Task SyncVocabularyAsync(Vocabulary _item)
        {
            await TranslateService.GetWordDefineInformationAsync(_item);

            // Process to get more meanings of this word
            if (string.IsNullOrEmpty(_item.WordId)) return;

            // start with current wordId
            string currentWordId = _item.WordId;
            int currentId = 0;

            var parts = currentWordId.Split('_');
            if (parts.Length == 2) int.TryParse(parts[1], out currentId);

            var newWords = new List<int>();

            if (currentId == 0) return;
            while (true)
            {
                currentId++;
                currentWordId = $"{_item.Word}_{currentId}";

                try
                {
                    var existDB = await DataService.GetVocabularyByWordIdAsync(currentWordId);
                    if (existDB != null)
                        break;

                    var hasNextMeaning = await TranslateService.HasMeaningAsync(currentWordId);
                    if (!hasNextMeaning)
                        break;

                    // Generate next WordId and add vocabulary
                    var newVocaId = await DataService.AddVocabularyAsync(_item.Word, currentWordId);
                    if (newVocaId > 0)
                    {
                        newWords.Add(newVocaId);
                        // Map to dictionary if successful
                        var dicId = await DataService.GetDictionaryIdByVocabularyIdAsync(_item.Id);
                        if (dicId > 0)
                            await DataService.AddVocabularyMappingAsync(dicId, newVocaId);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                    break;
                }
            }

            if (newWords.Any())
            {
                var vocabularies = await DataService.GetVocabularyByIdsAsync(newWords);
                foreach (var _newItem in vocabularies)
                {
                    if (string.IsNullOrEmpty(_newItem.Translate))
                        await TranslateService.GetVocabularyVietnameseTranslateAsync(_newItem);

                    if (string.IsNullOrEmpty(_newItem.Data))
                        await TranslateService.GetWordDefineInformationAsync(_newItem);
                }
            }

        }
    }
}
