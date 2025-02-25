using System;
using System.Windows;
using VocabularyReminder.DataAccessLibrary;

namespace VocabularyReminder.Services
{
    public class VocabularyDisplay
    {
        private static VocaPopup currentPopup;

        public static void ShowVocabulary(Vocabulary vocabulary)
        {
            if (vocabulary == null) return;

            if (App.isUseCustomPopup)
            {
                ShowCustomPopup(vocabulary);
            }
            else
            {
                VocabularyToast.ShowToastByVocabularyItem(vocabulary);
            }
            App.isShowPopup = true;
        }

        private static void ShowCustomPopup(Vocabulary vocabulary)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (currentPopup != null)
                {
                    currentPopup.Close();
                }

                currentPopup = new VocaPopup();
                currentPopup.SetVocabulary(vocabulary);
                currentPopup.Show();
            });
        }

        public static void Hide()
        {
            if (App.isUseCustomPopup)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (currentPopup != null)
                    {
                        currentPopup.Close();
                        currentPopup = null;
                    }
                });
            }
            else
            {
                VocabularyToast.ClearApplicationToast();
            }
            App.isShowPopup = false;
        }
    }
}