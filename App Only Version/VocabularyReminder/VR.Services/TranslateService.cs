using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using VR.Domain.Models;
using VR.Utils;

namespace VR.Services
{
    public class TranslateService
    {
        const string xpath_translate = "//span[@class='trans dtrans']";
        const string xpath_ipa = "//span[@class='ipa dipa']";
        const string xpath_type = "//span[@class='pos dpos']";
        const string xpath_mp3 = "//span[@class='phonetics']/*";
        public static string relatedAPIUrl = "https://relatedwords.org/api/related?term=";

        public static async Task<string> GetGoogleTranslate(string text)
        {
            // Check cache first
            if (VR.Services.CacheService.TryGetTranslation(text, out string cachedTranslation))
            {
                return cachedTranslation;
            }

            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=en&tl=vi&dt=t&q={Uri.EscapeDataString(text)}";
                    var response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    var result = await response.Content.ReadAsStringAsync();
                    
                    // Parse Google Translate response
                    var jsonResult = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(result);
                    if (jsonResult.ValueKind == JsonValueKind.Array &&
                        jsonResult[0].ValueKind == JsonValueKind.Array &&
                        jsonResult[0][0].ValueKind == JsonValueKind.Array)
                    {
                        var translatedText = jsonResult[0][0][0].GetString();
                        if (translatedText != null)
                        {
                            // Cache the successful translation
                            VR.Services.CacheService.CacheTranslation(text, translatedText);
                            return translatedText;
                        }
                    }
                    return text;
                }
                catch
                {
                    return text;
                }
            }
        }

        public static async Task<Vocabulary> GetVocabularyTranslateAsync(Vocabulary item)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                string _wordUrl = Helper.GetCambridgeWordUrl(item.Word);
                HttpResponseMessage response = await httpClient.GetAsync(_wordUrl);
                HttpContent content = response.Content;
                HtmlDocument document = new HtmlDocument();
                document.LoadHtml(await content.ReadAsStringAsync());

                List<string> listTrans = new List<string>();
                var translates = document.DocumentNode.SelectNodes(xpath_translate);
                if (translates != null && translates.Count > 0)
                foreach (var node in translates)
                {
                    if (!String.IsNullOrEmpty(node.InnerText))
                    {
                        listTrans.Add(node.InnerText);
                    }
                }

                List<string> listTypes = new List<string>();
                var types = document.DocumentNode.SelectNodes(xpath_type);
                if (types != null && types.Count > 0)
                {
                    foreach (var node in types)
                    {
                        if (!String.IsNullOrEmpty(node.InnerText))
                        {
                            listTypes.Add(node.InnerText);
                        }
                    }
                }

                if (listTrans.Count > 0)
                {
                    item.Translate = String.Join(", ", listTrans);
                }
                else
                {
                    item.Translate = String.Empty;
                }

                if (listTypes.Count > 0)
                {
                    item.Type = String.Join(", ", listTypes);
                }
                else
                {
                    item.Type = String.Empty;
                }

                await DataService.UpdateVocabularyAsync(item);
                return item;
            }
        }

        public static async Task GetWordDefineInformationAsync(Vocabulary item)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                string _wordUrl = Helper.GetOxfordWordUrl(item.Word);
                HttpResponseMessage response = await httpClient.GetAsync(_wordUrl);
                HttpContent content = response.Content;
                HtmlDocument document = new HtmlDocument();
                string htmlContent = await content.ReadAsStringAsync();
                document.LoadHtml(htmlContent);

                var extendedData = new ExtendedWordData
                {
                    Source = "OF"
                };
                // Get word type from pos span
                var posSpan = document.DocumentNode.SelectSingleNode("//span[@class='pos']");
                if (posSpan != null)
                {
                    extendedData.Type = posSpan.InnerText?.Trim();
                }

                // Get main word's CEFR level from Oxford 3000 symbol
                var symbolsDiv = document.DocumentNode.SelectSingleNode("//div[@class='symbols']");
                if (symbolsDiv != null)
                {
                    var levelLink = symbolsDiv.SelectSingleNode(".//a[contains(@href, 'level=')]");
                    if (levelLink != null)
                    {
                        var href = levelLink.GetAttributeValue("href", "");
                        var levelMatch = System.Text.RegularExpressions.Regex.Match(href, @"level=(\w\d)");
                        if (levelMatch.Success)
                        {
                            extendedData.Level = levelMatch.Groups[1].Value.ToUpper();
                        }
                    }
                }

                // Extract word identity from the redirected URL
                string responseUrl = response.RequestMessage.RequestUri.AbsolutePath;
                string[] urlParts = responseUrl.Split('/');
                if (urlParts.Length > 0)
                {
                    extendedData.ID = urlParts[urlParts.Length - 1];
                }

                // Get IPA and audio information
                var IpaNodes = document.DocumentNode.SelectNodes("//span[@class='phonetics']//div[contains(@class, 'phon')]");
                if (IpaNodes != null && IpaNodes.Count > 0)
                {
                    extendedData.Ipa = IpaNodes[0]?.InnerText?.Trim().Trim('/') ?? null;
                    if (IpaNodes.Count > 1)
                        extendedData.Ipa2 = IpaNodes[1]?.InnerText?.Trim().Trim('/') ?? null;
                }

                var soundNodes = document.DocumentNode.SelectNodes("//span[@class='phonetics']/div/div[contains(@class, 'sound')][1]");
                if (soundNodes != null && soundNodes.Count > 0)
                {
                    extendedData.Audio = soundNodes[0]?.GetAttributeValue("data-src-mp3", null);
                    if (soundNodes.Count > 1)
                        extendedData.Audio2 = soundNodes[1]?.GetAttributeValue("data-src-mp3", null);
                }

                // Try to get multiple senses first
                var multipleSensesOl = document.DocumentNode.SelectNodes("//ol[@class='senses_multiple']");
                var singleSense = document.DocumentNode.SelectNodes("//ol[@class='sense_single']/li[@class='sense']");

                // Handle multiple senses
                if (multipleSensesOl != null)
                {
                    foreach (var ol in multipleSensesOl)
                    {
                        // Handle direct senses (without part of speech)
                        var directSenses = ol.SelectNodes("./li[@class='sense']");
                        if (directSenses != null)
                        {
                            foreach (var sense in directSenses)
                            {
                                var definition = new DefinitionDto();
                                definition.PartOfSpeech = null; // No part of speech for direct senses

                                // Get CEFR level
                                var cefr = sense.GetAttributeValue("cefr", null);
                                if (!string.IsNullOrEmpty(cefr))
                                {
                                    definition.Level = cefr.ToUpper();
                                }

                                // Get definition text
                                var defNode = sense.SelectSingleNode(".//span[@class='def']");
                                definition.Definition = defNode?.InnerText?.Trim() ?? null;

                                // Get examples
                                var examplesUl = sense.SelectNodes(".//ul[@class='examples']");
                                if (examplesUl != null)
                                {
                                    foreach (var ul in examplesUl)
                                    {
                                        var examples = new List<ExampleDto>();
                                        var items = ul.SelectNodes(".//li");
                                        if (items != null)
                                        {
                                            foreach (var li in items)
                                            {
                                                var example = new ExampleDto();
                                                var structNode = li.SelectSingleNode(".//span[@class='cf']");
                                                example.Struct = structNode?.InnerText?.Trim() ?? null;
                                                var exampleNode = li.SelectSingleNode(".//span[@class='x']");
                                                example.Example = exampleNode?.InnerText?.Trim() ?? null;
                                                if (!string.IsNullOrEmpty(example.Example))
                                                {
                                                    examples.Add(example);
                                                }
                                            }
                                        }

                                        if (definition.Examples == null)
                                            definition.Examples = new List<ExampleDto>();

                                        definition.Examples.AddRange(examples);
                                    }
                                }

                                // Get topic
                                var topicNode = sense.SelectSingleNode(".//span[@class='topic_name']");
                                definition.Topic = topicNode?.InnerText?.Trim() ?? null;

                                if (!string.IsNullOrEmpty(definition.Definition))
                                {
                                    extendedData.Definitions.Add(definition);
                                }
                            }
                        }

                        // Handle senses grouped by part of speech
                        var posGroups = ol.SelectNodes("./span[@class='shcut-g']");
                        if (posGroups != null)
                        {
                            foreach (var posSection in posGroups)
                            {
                                // Get heading for part of speech group
                                var shcutTitle = posSection.SelectSingleNode(".//h2[@class='shcut']")?.InnerText;

                                var senses = posSection.SelectNodes(".//li[@class='sense']");
                                if (senses != null)
                                {
                                    foreach (var sense in senses)
                                    {
                                        var definition = new DefinitionDto();

                                        definition.PartOfSpeech = shcutTitle;

                                        // Get CEFR level
                                        var cefr = sense.GetAttributeValue("cefr", null);
                                        if (!string.IsNullOrEmpty(cefr))
                                        {
                                            definition.Level = cefr.ToUpper();
                                        }

                                        // Get definition text
                                        var defNode = sense.SelectSingleNode(".//span[@class='def']");
                                        definition.Definition = defNode?.InnerText?.Trim() ?? null;

                                        // Get examples
                                        var examplesUl = sense.SelectNodes(".//ul[@class='examples']");
                                        if (examplesUl != null)
                                        {
                                            foreach (var ul in examplesUl)
                                            {
                                                var examples = new List<ExampleDto>();
                                                var exampleItems = ul.SelectNodes(".//li");
                                                if (exampleItems != null)
                                                {
                                                    foreach (var exampleItem in exampleItems)
                                                    {
                                                        var example = new ExampleDto();
                                                        var structNode = exampleItem.SelectSingleNode(".//span[@class='cf']");
                                                        example.Struct = structNode?.InnerText?.Trim() ?? null;
                                                        var exampleNode = exampleItem.SelectSingleNode(".//span[@class='x']");
                                                        example.Example = exampleNode?.InnerText?.Trim() ?? null;
                                                        if (!string.IsNullOrEmpty(example.Example))
                                                        {
                                                            examples.Add(example);
                                                        }
                                                    }
                                                }

                                                if (definition.Examples == null)
                                                    definition.Examples = new List<ExampleDto>();

                                                definition.Examples.AddRange(examples);
                                            }
                                        }

                                        // Get topic
                                        var topicNode = sense.SelectSingleNode(".//span[@class='topic_name']");
                                        definition.Topic = topicNode?.InnerText?.Trim() ?? null;

                                        if (!string.IsNullOrEmpty(definition.Definition))
                                        {
                                            extendedData.Definitions.Add(definition);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                // Handle single sense
                else if (singleSense != null && singleSense.Count > 0)
                {
                    foreach (var sense in singleSense)
                    {
                        var definition = new DefinitionDto();

                        // Get CEFR level
                        var cefr = sense.GetAttributeValue("cefr", null);
                        if (!string.IsNullOrEmpty(cefr))
                        {
                            definition.Level = cefr.ToUpper();
                        }

                        // Get definition text
                        var defNode = sense.SelectSingleNode(".//span[@class='def']");
                        definition.Definition = defNode?.InnerText?.Trim() ?? null;

                        // Get examples
                        var examplesUl = sense.SelectNodes(".//ul[@class='examples']");
                        if (examplesUl != null)
                        {
                            foreach (var ul in examplesUl)
                            {
                                var examples = new List<ExampleDto>();
                                var exampleItems = ul.SelectNodes(".//li");
                                if (exampleItems != null)
                                {
                                    foreach (var exampleItem in exampleItems)
                                    {
                                        var example = new ExampleDto();
                                        var structNode = exampleItem.SelectSingleNode(".//span[@class='cf']");
                                        example.Struct = structNode?.InnerText?.Trim() ?? null;
                                        var exampleNode = exampleItem.SelectSingleNode(".//span[@class='x']");
                                        example.Example = exampleNode?.InnerText?.Trim() ?? null;
                                        if (!string.IsNullOrEmpty(example.Example))
                                        {
                                            examples.Add(example);
                                        }
                                    }
                                }

                                if (definition.Examples == null)
                                    definition.Examples = new List<ExampleDto>();

                                definition.Examples.AddRange(examples);
                            }
                        }

                        // Get topic
                        var topicNode = sense.SelectSingleNode(".//span[@class='topic_name']");
                        definition.Topic = topicNode?.InnerText?.Trim() ?? null;

                        if (!string.IsNullOrEmpty(definition.Definition))
                        {
                            extendedData.Definitions.Add(definition);
                        }
                    }
                }

                // Get idioms section
                var idiomsSection = document.DocumentNode.SelectSingleNode("//div[@class='idioms']");
                if (idiomsSection != null)
                {
                    var idiomBlocks = idiomsSection.SelectNodes(".//span[@class='idm-g']");
                    if (idiomBlocks != null)
                    {
                        foreach (var idiomBlock in idiomBlocks)
                        {
                            var idiom = new IdiomDataDto();

                            // Get idiom phrase
                            var phraseNode = idiomBlock.SelectSingleNode(".//span[@class='idm']");
                            idiom.Phrase = phraseNode?.InnerText?.Trim() ?? null;

                            // Get CEFR level
                            var cefr = phraseNode?.GetAttributeValue("cefr", null);
                            if (!string.IsNullOrEmpty(cefr))
                            {
                                idiom.Level = cefr.ToUpper();
                            }

                            // Get labels (informal, humorous, etc.)
                            var labelsNode = idiomBlock.SelectSingleNode(".//span[@class='labels']");
                            if (labelsNode != null)
                            {
                                idiom.Labels = labelsNode.InnerText.Split(',')
                                    .Select(l => l.Trim().Trim("()".ToCharArray()))
                                    .Where(l => !string.IsNullOrEmpty(l))
                                    .ToList();
                            }

                            // Get definition
                            var defNode = idiomBlock.SelectSingleNode(".//span[@class='def']");
                            idiom.Definition = defNode?.InnerText?.Trim() ?? null;

                            // Get examples
                            var examples = idiomBlock.SelectNodes(".//ul[@class='examples']//span[@class='x']");
                            if (examples != null)
                                idiom.Examples = examples.Select(x => x.InnerText.Trim()).ToList();

                            if (!string.IsNullOrEmpty(idiom.Phrase) && !string.IsNullOrEmpty(idiom.Definition))
                            {
                                if (extendedData.Idioms == null)
                                    extendedData.Idioms = new List<IdiomDataDto>();

                                extendedData.Idioms.Add(idiom);
                            }
                        }
                    }
                }

                // Replace media URL
                if (!string.IsNullOrEmpty(extendedData.Audio) && extendedData.Audio.Contains("https://www.oxfordlearnersdictionaries.com"))
                    extendedData.Audio = extendedData.Audio.Replace("https://www.oxfordlearnersdictionaries.com", "");

                if (!string.IsNullOrEmpty(extendedData.Audio2) && extendedData.Audio2.Contains("https://www.oxfordlearnersdictionaries.com"))
                    extendedData.Audio2 = extendedData.Audio2.Replace("https://www.oxfordlearnersdictionaries.com", "");

                // Store legacy data for backward compatibility
                if (extendedData.Definitions != null && extendedData.Definitions.Any())
                {
                    item.Define = extendedData.Definitions[0].Definition;

                    if (extendedData.Definitions[0].Examples != null && extendedData.Definitions[0].Examples.Count > 0)
                        item.Example = extendedData.Definitions[0].Examples[0].Example;

                    if (extendedData.Definitions[0].Examples != null && extendedData.Definitions[0].Examples.Count > 1)
                        item.Example2 = extendedData.Definitions[0].Examples[1].Example;
                }

                item.Ipa = extendedData.Ipa;
                item.Ipa2 = extendedData.Ipa2;
                item.PlayURL = extendedData.Audio;
                item.PlayURL2 = extendedData.Audio2;

                // Store complete data as JSON, ignoring null values
                var jsonSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                item.Data = JsonConvert.SerializeObject(extendedData, jsonSettings);

                item.ViewedDate = DateTime.Now.ToUnixTimeInSeconds();
                await DataService.UpdateVocabularyAsync(item);
            }
        }

        public static async Task GetRelatedWord(Vocabulary item)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                string _wordUrl = relatedAPIUrl + item.Word.ToLower();
                HttpResponseMessage response = await httpClient.GetAsync(_wordUrl);
                HttpContent content = response.Content;

                httpClient.DefaultRequestHeaders.ConnectionClose = true;

                var rawJon = await content.ReadAsStringAsync();
                var RelatedObj = JsonConvert.DeserializeObject<List<RelatedItem>>(rawJon);

                int _Count = 0;
                List<string> RelatedList = new List<string>();
                foreach (RelatedItem _relatedItem in RelatedObj)
                {
                    _Count++;
                    if (_Count > 10) break;
                    RelatedList.Add(_relatedItem.word);
                }

                item.Related = (RelatedList.Count > 0) ? String.Join(", ", RelatedList) : String.Empty;
                await DataService.UpdateVocabularyAsync(item);
            }
        }
    }

    class RelatedItem
    {
        [JsonProperty("word")]
        public string word { get; set; }

        [JsonProperty("score")]
        public string score { get; set; }

        [JsonProperty("from")]
        public string from { get; set; }
    }
}
