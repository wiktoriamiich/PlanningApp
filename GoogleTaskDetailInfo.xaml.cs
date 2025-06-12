using System;
using System.Windows;
using System.Windows.Controls;
using PlanningApp.Models;

namespace PlanningApp
{
    /// <summary>
    /// Okno dialogowe do uzupełnienia szczegółów zadania pobranego z Google Tasks.
    /// Umożliwia użytkownikowi ustawienie priorytetu, kategorii i czasu trwania.
    /// </summary>
    public partial class GoogleTaskDetailsWindow : Window
    {
        /// <summary>
        /// Tytuł zadania wprowadzony przez użytkownika.
        /// </summary>
        public string Tytul => TytulTextBox.Text;

        /// <summary>
        /// Opis zadania wprowadzony przez użytkownika.
        /// </summary>
        public string Opis => OpisTextBox.Text;

        /// <summary>
        /// Wybrany priorytet zadania.
        /// </summary>
        public string Priorytet => (PriorytetComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

        /// <summary>
        /// Wybrana kategoria zadania.
        /// </summary>
        public string Kategoria => (KategoriaComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

        /// <summary>
        /// Czas trwania zadania w formacie hh:mm.
        /// </summary>
        public string CzasTrwania => CzasTrwaniaTextBox.Text;

        /// <summary>
        /// Inicjalizuje nowe okno dialogowe i wypełnia pola domyślnymi wartościami
        /// pobranymi z Google Task (tytuł, opis).
        /// </summary>
        /// <param name="tytul">Tytuł zadania z Google Tasks.</param>
        /// <param name="opis">Opis zadania z Google Tasks.</param>
        public GoogleTaskDetailsWindow(string tytul, string opis)
        {
            InitializeComponent();
            TytulTextBox.Text = tytul;
            OpisTextBox.Text = opis;
            PriorytetComboBox.SelectedIndex = 1; // Średni
            KategoriaComboBox.SelectedIndex = 3; // Inne
        }

        /// <summary>
        /// Obsługuje kliknięcie przycisku OK. Zamyka okno i ustawia wynik DialogResult na true.
        /// </summary>
        /// <param name="sender">Źródło zdarzenia (przycisk).</param>
        /// <param name="e">Argumenty zdarzenia kliknięcia.</param>
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        /// <summary>
        /// Obsługuje kliknięcie przycisku Anuluj. Zamyka okno bez zapisania zmian.
        /// </summary>
        /// <param name="sender">Źródło zdarzenia (przycisk).</param>
        /// <param name="e">Argumenty zdarzenia kliknięcia.</param>
        private void Anuluj_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
