using DesktopNotifications;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace VR.Utils
{
    public class Helper
    {
        public static string GetCambridgeWordUrl(string word)
        {
            return $"https://dictionary.cambridge.org/vi/dictionary/english-vietnamese/{Uri.EscapeDataString(word.ToLower())}";
        }

        public static string GetOxfordWordUrl(string word)
        {
            return $"https://www.oxfordlearnersdictionaries.com/definition/english/{Uri.EscapeDataString(word.ToLower())}";
        }

        public static void ClearToast()
        {
            DesktopNotificationManagerCompat.History.Clear();
        }

        public static void ShowToast(string msg, string subMsg = null)
        {
            if (subMsg == null) subMsg = "";

            Console.WriteLine(msg + "\n" + subMsg);

            ToastContent content = getToastContent(msg, subMsg);

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(content.GetContent());

            var _toastItem = new ToastNotification(xmlDoc)
            {
                Tag = "Vocabulary",
                Group = "Reminder",
            };
            DesktopNotificationManagerCompat.CreateToastNotifier().Show(_toastItem);
        }

        private static ToastContent getToastContent(string msg, string subMsg = null)
        {
            if (subMsg == null) subMsg = "";
            ToastContent content = new ToastContent()
            {
                Launch = "vocabulary-notification",
                Audio = new ToastAudio() { Silent = true },
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = msg,
                            },

                            new AdaptiveText()
                            {
                                Text = subMsg,
                            },
                        },
                    },
                },
            };

            return content;
        }

    }
}
