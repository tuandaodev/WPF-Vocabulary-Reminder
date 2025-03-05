using System.Windows;
using VR.Domain.Models;

namespace VR.Services
{
    public class VocabularyDisplayService
    {

        private static VocaPopup currentPopup;

        public static void ShowVocabulary(Vocabulary vocabulary)
        {
            if (vocabulary == null) return;
            ShowCustomPopup(vocabulary);
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
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (currentPopup != null)
                {
                    currentPopup.Close();
                    currentPopup = null;
                }
            });
            App.isShowPopup = false;
        }
    }
}