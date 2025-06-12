using PlanningApp.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace PlanningApp
{
    /// <summary>
    /// Okno edycji pojedynczego zadania. Pozwala użytkownikowi zmienić tytuł, opis, termin,
    /// priorytet, kategorię oraz czas trwania.
    /// </summary>
    public partial class EditTaskWindow : Window
    {
        /// <summary>
        /// Edytowane zadanie, którego właściwości mogą zostać zmienione w oknie.
        /// </summary>
        public TaskItem EdytowaneZadanie { get; private set; }

        /// <summary>
        /// Inicjalizuje okno edycji i ustawia dane zadania w odpowiednich polach.
        /// </summary>
        /// <param name="zadanie">Zadanie przekazane do edycji.</param>
        public EditTaskWindow(TaskItem zadanie)
        {
            InitializeComponent();
            EdytowaneZadanie = zadanie;

            TytulBox.Text = zadanie.Tytul;
            OpisBox.Text = zadanie.Opis;
            DeadlinePicker.SelectedDate = zadanie.Deadline;

            if (TimeSpan.TryParse(zadanie.CzasTrwania, out var czas))
                CzasTrwaniaTextBox.Text = czas.ToString(@"hh\:mm");
            else
                CzasTrwaniaTextBox.Text = "00:00";

            foreach (ComboBoxItem item in PriorytetBox.Items)
                if ((string)item.Content == zadanie.Priorytet)
                    PriorytetBox.SelectedItem = item;

            foreach (ComboBoxItem item in KategoriaBox.Items)
                if ((string)item.Content == zadanie.Kategoria)
                    KategoriaBox.SelectedItem = item;
        }

        /// <summary>
        /// Obsługuje kliknięcie przycisku „Zapisz”. Aktualizuje dane zadania na podstawie pól w formularzu
        /// i zamyka okno z wynikiem DialogResult = true.
        /// </summary>
        /// <param name="sender">Obiekt źródłowy zdarzenia (przycisk).</param>
        /// <param name="e">Argumenty zdarzenia kliknięcia.</param>
        private void Zapisz_Click(object sender, RoutedEventArgs e)
        {
            EdytowaneZadanie.Tytul = TytulBox.Text;
            EdytowaneZadanie.Opis = OpisBox.Text;
            EdytowaneZadanie.Priorytet = (PriorytetBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            EdytowaneZadanie.Kategoria = (KategoriaBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            EdytowaneZadanie.Deadline = DeadlinePicker.SelectedDate ?? DateTime.Now;

            if (TimeSpan.TryParse(CzasTrwaniaTextBox.Text, out var czas))
                EdytowaneZadanie.CzasTrwania = czas.ToString(@"hh\:mm");

            DialogResult = true;
        }

        /// <summary>
        /// Obsługuje kliknięcie przycisku „Anuluj”. Anuluje edycję i zamyka okno bez zapisywania zmian.
        /// </summary>
        /// <param name="sender">Obiekt źródłowy zdarzenia.</param>
        /// <param name="e">Argumenty zdarzenia kliknięcia.</param>
        private void Anuluj_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

