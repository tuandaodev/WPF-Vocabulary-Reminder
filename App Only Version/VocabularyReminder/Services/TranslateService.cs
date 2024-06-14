using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using VocabularyReminder.DataAccessLibrary;

namespace DesktopNotifications.Services
{
    class TranslateService
    {
        public static string mainTranslateUrl = "https://dictionary.cambridge.org/vi/dictionary/english-vietnamese/";
        const string xpath_translate = "//span[@class='trans dtrans']";
        const string xpath_ipa = "//span[@class='ipa dipa']";
        const string xpath_type = "//span[@class='pos dpos']";

        public static string mainGetPlayUrl = "https://www.oxfordlearnersdictionaries.com/definition/english/";
        const string xpath_mp3 = "//span[@class='phonetics']/*";

        public static string relatedAPIUrl = "https://relatedwords.org/api/related?term=";

        public static async Task GetVocabularyTranslate(Vocabulary item)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                string _wordUrl = mainTranslateUrl + item.Word.ToLower();
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

                await DataAccess.UpdateVocabularyAsync(item);
                //Console.WriteLine(String.Format("Translate Completed: {0}", item.Word));
            }
        }

        public static async Task GetWordDefineInformation(Vocabulary item)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                string _wordUrl = mainGetPlayUrl + item.Word.ToLower();
                HttpResponseMessage response = await httpClient.GetAsync(_wordUrl);
                HttpContent content = response.Content;
                HtmlDocument document = new HtmlDocument();
                document.LoadHtml(await content.ReadAsStringAsync());
                // Get Title

                var DefineNode = document.DocumentNode.SelectSingleNode("(//span[@class='def'])[1]");
                if (DefineNode != null)
                {
                    item.Define = DefineNode.InnerText;
                }

                var ExampleNodes = document.DocumentNode.SelectNodes("(//ul[@class='examples']//span[@class='x'])[position()<3]");
                if (ExampleNodes != null && ExampleNodes.Count > 0)
                {
                    int _count = 0;
                    foreach (var node in ExampleNodes)
                    {
                        _count++;
                        if (_count == 1)
                        {
                            item.Example = (node != null) ? node.InnerText : "";
                        }
                        else if (_count == 2)
                        {
                            item.Example2 = (node != null) ? node.InnerText : "";
                        }
                    }
                }

                var IpaNodes = document.DocumentNode.SelectNodes("//span[@class='phonetics']//div[contains(@class, 'phon')]");
                if (IpaNodes != null && IpaNodes.Count > 0)
                {
                    int _count = 0;
                    foreach (var node in IpaNodes)
                    {
                        _count++;
                        if (_count == 1)
                        {
                            item.Ipa = (node != null) ? node.InnerText : "";
                        }
                        else if (_count == 2)
                        {
                            item.Ipa2 = (node != null) ? node.InnerText : "";
                        }
                    }
                }

                var soundNodes = document.DocumentNode.SelectNodes("//span[@class='phonetics']/div/div[contains(@class, 'sound')][1]");
                if (soundNodes != null && soundNodes.Count > 0)
                {
                    int _count = 0;
                    foreach (var node in soundNodes)
                    {
                        _count++;
                        if (_count == 1)
                        {
                            item.PlayURL = (node != null) ? node.GetAttributeValue("data-src-mp3", "") : "";
                        } else if (_count == 2)
                        {
                            item.PlayURL2 = (node != null) ? node.GetAttributeValue("data-src-mp3", "") : "";
                        }
                    }
                }

                if (!String.IsNullOrEmpty(item.PlayURL))
                {
                    await DataAccess.UpdateVocabularyAsync(item);
                }
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
                await DataAccess.UpdateVocabularyAsync(item);
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
