using PlanningApp.Models;
using PlanningApp.GoogleIntegration;
using PlanningApp.Helpers;
using PlanningApp.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Controls;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Win32;
using Google.Apis.Calendar.v3.Data;
using System.Windows.Media;

namespace PlanningApp
{
    /// <summary>
    /// Główne okno aplikacji ProductiveApp.
    /// Obsługuje interfejs użytkownika, bazę danych i integracje z Google oraz AI.
    /// </summary>
    public partial class MainWindow : Window
    {
        private AppDbContext _context;
        private List<TaskItem> _aktualnaLista;

        public SeriesCollection SeriesZadan { get; set; }
        public string[] KategorieLabels { get; set; }

        private DateTime _poczatekTygodnia = DateTime.Today;

        /// <summary>
        /// Inicjalizuje główne komponenty i ustawia domyślne dane (np. sortowanie, połączenie z bazą).
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            _context = new AppDbContext();
            OdswiezListe();
            SortowanieComboBox.SelectedIndex = 0;
            NavigationList.SelectedIndex = 0;
        }


        /// <summary>
        /// Odświeża dane w tabeli zadań i generuje widok tygodnia.
        /// </summary>
        private void OdswiezListe()
        {
            _aktualnaLista = _context.TaskItems.ToList();
            TaskListView.ItemsSource = _aktualnaLista;
            AktualizujStatystyki();
        }


        /// <summary>
        /// Dodaje nowe zadanie do bazy danych i resetuje formularz.
        /// </summary>
        private void Dodaj_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TytulTextBox.Text))
            {
                MessageBox.Show("Tytuł zadania jest wymagany.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (DeadlinePicker.SelectedDate == null)
            {
                MessageBox.Show("Wybierz termin zadania.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TimeSpan czasTrwania;
            if (!TimeSpan.TryParse(CzasTrwaniaTextBox.Text, out czasTrwania))
            {
                string kategoria = ((ComboBoxItem)KategoriaComboBox.SelectedItem)?.Content.ToString()?.ToLower();

                switch (kategoria)
                {
                    case "studia":
                        czasTrwania = TimeSpan.FromHours(3.5);
                        break;
                    case "praca":
                        czasTrwania = TimeSpan.FromHours(2);
                        break;
                    case "dom":
                        czasTrwania = TimeSpan.FromHours(1);
                        break;
                    default:
                        czasTrwania = TimeSpan.FromHours(1);
                        break;
                }
            }

            var noweZadanie = new TaskItem
            {
                Tytul = TytulTextBox.Text,
                Opis = OpisTextBox.Text,
                Priorytet = ((ComboBoxItem)PriorytetComboBox.SelectedItem)?.Content.ToString(),
                Kategoria = ((ComboBoxItem)KategoriaComboBox.SelectedItem)?.Content.ToString(),
                Deadline = DeadlinePicker.SelectedDate ?? DateTime.Now,
                CzasTrwania = czasTrwania.ToString(@"hh\:mm"),
                CzyZrealizowane = false
            };

            _context.TaskItems.Add(noweZadanie);
            _context.SaveChanges();
            OdswiezListe();

            TytulTextBox.Clear();
            OpisTextBox.Clear();
            CzasTrwaniaTextBox.Clear();
            PriorytetComboBox.SelectedIndex = 0;
            KategoriaComboBox.SelectedIndex = 0;
            DeadlinePicker.SelectedDate = null;
        }

        /// <summary>
        /// Usuwa zaznaczone zadanie z bazy danych.
        /// </summary>
        private void Usun_Click(object sender, RoutedEventArgs e)
        {
            if (TaskListView.SelectedItem is TaskItem zaznaczone)
            {
                _context.TaskItems.Remove(zaznaczone);
                _context.SaveChanges();
                OdswiezListe();
            }
            else
            {
                MessageBox.Show("Zaznacz zadanie do usunięcia.", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Obsługuje kliknięcie przycisku edycji zadania.
        /// Otwiera okno dialogowe <see cref="EditTaskWindow"/> z danymi wybranego zadania.
        /// Jeśli użytkownik zatwierdzi zmiany, zapisuje je do bazy danych i odświeża listę zadań.
        /// </summary>
        /// <param name="sender">Obiekt, który wywołał zdarzenie – przycisk edycji.</param>
        /// <param name="e">Dane zdarzenia kliknięcia przycisku.</param>
        private void Edytuj_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is TaskItem zadanie)
            {
                var okno = new EditTaskWindow(zadanie)
                {
                    Owner = this
                };

                if (okno.ShowDialog() == true)
                {
                    _context.SaveChanges();
                    OdswiezListe();
                }
            }
        }


        /// <summary>
        /// Filtruje zadania według wybranej kategorii.
        /// </summary>
        private void FiltrKategoriiComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_context == null) return;

            var wybrana = ((ComboBoxItem)FiltrKategoriiComboBox.SelectedItem)?.Content?.ToString();

            _aktualnaLista = wybrana == "Wszystkie"
                ? _context.TaskItems.ToList()
                : _context.TaskItems.Where(t => t.Kategoria == wybrana).ToList();

            TaskListView.ItemsSource = _aktualnaLista;
            AktualizujStatystyki();
        }

        /// <summary>
        /// Sortuje listę zadań według wybranego kryterium.
        /// </summary>
        private void SortowanieComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_context == null) return;

            var selected = ((ComboBoxItem)SortowanieComboBox.SelectedItem)?.Content?.ToString();

            if (selected == "Data rosnąco")
                _aktualnaLista = _aktualnaLista.OrderBy(t => t.Deadline).ToList();
            else if (selected == "Data malejąco")
                _aktualnaLista = _aktualnaLista.OrderByDescending(t => t.Deadline).ToList();
            else if (selected == "Priorytet: Wysoki -> Niski")
                _aktualnaLista = _aktualnaLista.OrderByDescending(t => t.Priorytet).ToList();
            else if (selected == "Priorytet: Niski -> Wysoki")
                _aktualnaLista = _aktualnaLista.OrderBy(t => t.Priorytet).ToList();
            else
                _aktualnaLista = _context.TaskItems.ToList();

            TaskListView.ItemsSource = _aktualnaLista;
            AktualizujStatystyki();
        }

        /// <summary>
        /// Zapisuje zmiany w zadaniu po edycji w tabeli.
        /// </summary>
        private void TaskDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Row.Item is TaskItem edytowane)
            {
                var zadanieZBazy = _context.TaskItems.Find(edytowane.Id);
                if (zadanieZBazy != null)
                {
                    zadanieZBazy.Tytul = edytowane.Tytul;
                    zadanieZBazy.Opis = edytowane.Opis;
                    zadanieZBazy.Priorytet = edytowane.Priorytet;
                    zadanieZBazy.Kategoria = edytowane.Kategoria;
                    zadanieZBazy.Deadline = edytowane.Deadline;
                    zadanieZBazy.CzyZrealizowane = edytowane.CzyZrealizowane;
                    zadanieZBazy.CzasTrwania = edytowane.CzasTrwania;

                    _context.SaveChanges();
                    AktualizujStatystyki();
                }
            }

        }

        /// <summary>
        /// Eksportuje listę zadań do pliku CSV.
        /// </summary>
        private void EksportujCsv_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = "zadania.csv" };
            if (dialog.ShowDialog() == true)
            {
                var csv = new StringBuilder();
                csv.AppendLine("Tytuł;Opis;Priorytet;Kategoria;Deadline;Zrobione");
                foreach (var zadanie in _aktualnaLista)
                    csv.AppendLine($"{zadanie.Tytul};{zadanie.Opis};{zadanie.Priorytet};{zadanie.Kategoria};{zadanie.Deadline:d};{zadanie.CzyZrealizowane}");
                File.WriteAllText(dialog.FileName, csv.ToString());
            }
        }

        /// <summary>
        /// Oblicza statystyki zadań i aktualizuje wykresy.
        /// </summary>
        private void AktualizujStatystyki()
        {
            var wszystkie = _aktualnaLista.Count;
            var wykonane = _aktualnaLista.Count(t => t.CzyZrealizowane);
            var niewykonane = wszystkie - wykonane;

            StatystykiTextBlock.Text = $"Zadania: {wszystkie} | Wykonane: {wykonane} | Do zrobienia: {niewykonane}";

            var grupy = _aktualnaLista
                .GroupBy(t => t.Kategoria)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToList();

            KategorieLabels = grupy.Select(g => g.Key).ToArray();
            SeriesZadan = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Zadania",
                    Values = new ChartValues<int>(grupy.Select(g => g.Count))
                }
            };

            WykresKategorii.AxisX.Clear();
            WykresKategorii.AxisX.Add(new Axis { Labels = KategorieLabels, Title = "Kategoria" });
            WykresKategorii.Series = SeriesZadan;

            PieZadan.Series = new SeriesCollection
            {
                new PieSeries { Title = "Wykonane", Values = new ChartValues<int> { wykonane }, DataLabels = true },
                new PieSeries { Title = "Do zrobienia", Values = new ChartValues<int> { niewykonane }, DataLabels = true }
            };
        }

        /// <summary>
        /// Generuje widok tygodnia na podstawie lokalnych zadań.
        /// </summary>
        private async void GenerujWidokTygodniaPołączony()
        {
            await SynchronizujZadaniaZGoogleAsync(); 

            GridKalendarza.Children.Clear();
            GridKalendarza.RowDefinitions.Clear();
            GridKalendarza.ColumnDefinitions.Clear();

            for (int i = 0; i < 7; i++)
                GridKalendarza.ColumnDefinitions.Add(new ColumnDefinition());

            for (int i = 0; i <= 12; i++)
                GridKalendarza.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });

            for (int i = 0; i < 7; i++)
            {
                var date = _poczatekTygodnia.AddDays(i);
                var naglowek = new TextBlock
                {
                    Text = date.ToString("dddd\n dd.MM"),
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetRow(naglowek, 0);
                Grid.SetColumn(naglowek, i);
                GridKalendarza.Children.Add(naglowek);
            }

            foreach (var zad in _aktualnaLista.Where(t =>
                t.Deadline >= _poczatekTygodnia && t.Deadline < _poczatekTygodnia.AddDays(7)))
            {
                int col = (zad.Deadline.Date - _poczatekTygodnia).Days;
                int row = Math.Max(1, zad.Deadline.Hour - 7); 

                Brush kolorTla = Brushes.LightGray;
                switch (zad.Priorytet?.ToLower())
                {
                    case "wysoki":
                        kolorTla = Brushes.LightCoral;
                        break;
                    case "średni":
                        kolorTla = Brushes.Khaki;
                        break;
                    case "niski":
                        kolorTla = Brushes.LightGreen;
                        break;
                }

                string ikona = "";
                switch (zad.Kategoria?.ToLower())
                {
                    case "studia":
                        ikona = "🎓 ";
                        break;
                    case "praca":
                        ikona = "💼 ";
                        break;
                    case "dom":
                        ikona = "🏠 ";
                        break;
                    case "google":
                        ikona = "🌐 ";
                        break;
                }

                var border = new Border
                {
                    BorderBrush = Brushes.DarkGreen,
                    BorderThickness = new Thickness(1),
                    Background = kolorTla,
                    Margin = new Thickness(2),
                    ToolTip = zad.Opis,
                    Child = new TextBlock
                    {
                        Text = ikona + zad.Tytul,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 12
                    }
                };

                Grid.SetColumn(border, col);
                Grid.SetRow(border, row);
                GridKalendarza.Children.Add(border);
            }
        }


        /// <summary>
        /// Pobiera dane z kalendarza Google 
        /// </summary>
        private async void OdswiezKalendarz_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var wydarzenia = await GoogleCalendarHelper.GetUpcomingEventsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas pobierania kalendarza: " + ex.Message);
            }
        }

        /// <summary>
        /// Zwraca datę poniedziałku dla podanej daty.
        /// </summary>
        private DateTime GetMonday(DateTime date)
        {
            int delta = DayOfWeek.Monday - date.DayOfWeek;
            return date.AddDays(delta > 0 ? delta - 7 : delta).Date;
        }

        /// <summary>
        /// Ustawia bieżący tydzień (poniedziałek aktualnego tygodnia) i generuje widok kalendarza.
        /// </summary>
        /// <param name="sender">Element, który wywołał zdarzenie – np. przycisk.</param>
        /// <param name="e">Dane zdarzenia kliknięcia.</param>
        private async void BiezacyTydzien_Click(object sender, RoutedEventArgs e)
        {
            _poczatekTygodnia = GetMonday(DateTime.Today);
            await GenerujWidokTygodniaGoogleAsync();
        }

        /// <summary>
        /// Przechodzi do następnego tygodnia i aktualizuje widok kalendarza.
        /// </summary>
        /// <param name="sender">Element wywołujący – np. przycisk.</param>
        /// <param name="e">Dane zdarzenia kliknięcia.</param>
        private async void NastepnyTydzien_Click(object sender, RoutedEventArgs e)
        {
            _poczatekTygodnia = _poczatekTygodnia.AddDays(7);
            await GenerujWidokTygodniaGoogleAsync();
        }

        /// <summary>
        /// Przechodzi do poprzedniego tygodnia i aktualizuje widok kalendarza.
        /// </summary>
        /// <param name="sender">Element wywołujący – np. przycisk.</param>
        /// <param name="e">Dane zdarzenia kliknięcia.</param>
        private async void PoprzedniTydzien_Click(object sender, RoutedEventArgs e)
        {
            _poczatekTygodnia = _poczatekTygodnia.AddDays(-7);
            await GenerujWidokTygodniaGoogleAsync();
        }

        /// <summary>
        /// Zmienia widok kalendarza na tydzień zawierający wybraną datę z DatePicker'a.
        /// </summary>
        /// <param name="sender">Element wywołujący – kontrolka DatePicker.</param>
        /// <param name="e">Dane zdarzenia zmiany daty.</param>
        private async void WybierzDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (WybierzDatePicker.SelectedDate.HasValue)
            {
                _poczatekTygodnia = GetMonday(WybierzDatePicker.SelectedDate.Value);
                await GenerujWidokTygodniaGoogleAsync();
            }
        }

        /// <summary>
        /// Generuje widok kalendarza na bieżący tydzień, uwzględniając lokalne zadania oraz wydarzenia z Google Calendar.
        /// </summary>
        /// <remarks>
        /// Metoda wykonuje automatyczną synchronizację z Google Tasks, pobiera listę zadań z bazy danych oraz wydarzeń z kalendarza Google,
        /// a następnie renderuje siatkę kalendarza tygodniowego z zadaniami i wydarzeniami.
        /// Zadania oznaczane są kolorami według priorytetu i ikoną wg kategorii.
        /// Wydarzenia z Google Calendar oznaczane są kolorem niebieskim.
        /// </remarks>
        /// <returns>Asynchroniczne zadanie typu Task reprezentujące zakończenie generowania widoku.</returns>
        private async Task GenerujWidokTygodniaGoogleAsync()
        {
            await SynchronizujZadaniaZGoogleAsync(); 
            _aktualnaLista = _context.TaskItems.ToList(); 

            GridKalendarza.Children.Clear();
            GridKalendarza.RowDefinitions.Clear();
            GridKalendarza.ColumnDefinitions.Clear();

            for (int i = 0; i < 7; i++)
                GridKalendarza.ColumnDefinitions.Add(new ColumnDefinition());
            for (int i = 0; i <= 12; i++)
                GridKalendarza.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });

            for (int i = 0; i < 7; i++)
            {
                var date = _poczatekTygodnia.AddDays(i);
                var header = new TextBlock
                {
                    Text = date.ToString("dddd\n dd.MM"),
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetRow(header, 0);
                Grid.SetColumn(header, i);
                GridKalendarza.Children.Add(header);
            }

            var komorki = new Dictionary<(int col, int row), StackPanel>();

            foreach (var zad in _aktualnaLista.Where(t =>
                t.Deadline >= _poczatekTygodnia && t.Deadline < _poczatekTygodnia.AddDays(7)))
            {
                int col = (zad.Deadline.Date - _poczatekTygodnia).Days;
                int row = Math.Max(1, zad.Deadline.Hour - 7);

                Brush kolorTla = Brushes.LightGray;
                switch (zad.Priorytet?.ToLower())
                {
                    case "wysoki":
                        kolorTla = Brushes.LightCoral;
                        break;
                    case "średni":
                        kolorTla = Brushes.Khaki;
                        break;
                    case "niski":
                        kolorTla = Brushes.LightGreen;
                        break;
                }

                string ikona = "";
                switch (zad.Kategoria?.ToLower())
                {
                    case "studia":
                        ikona = "🎓 ";
                        break;
                    case "praca":
                        ikona = "💼 ";
                        break;
                    case "dom":
                        ikona = "🏠 ";
                        break;
                    case "google":
                        ikona = "🌐 ";
                        break;
                }

                var border = new Border
                {
                    Background = kolorTla,
                    Margin = new Thickness(2),
                    ToolTip = zad.Opis,
                    BorderThickness = new Thickness(0),
                    Child = new TextBlock
                    {
                        Text = ikona + zad.Tytul,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 12
                    }
                };

                var key = (col, row);
                if (!komorki.ContainsKey(key))
                {
                    var stos = new StackPanel();
                    Grid.SetColumn(stos, col);
                    Grid.SetRow(stos, row);
                    GridKalendarza.Children.Add(stos);
                    komorki[key] = stos;
                }

                komorki[key].Children.Add(border);
            }

            var wydarzenia = await GoogleCalendarHelper.GetUpcomingEventsAsync();
            foreach (var ev in wydarzenia)
            {
                if (ev.Start == null || ev.Start.DateTime == null) continue;
                var start = ev.Start.DateTime.Value;

                if (start < _poczatekTygodnia || start >= _poczatekTygodnia.AddDays(7))
                    continue;

                int col = (start.Date - _poczatekTygodnia).Days;
                int row = Math.Max(1, start.Hour - 7);

                var border = new Border
                {
                    Background = Brushes.LightSkyBlue,
                    Margin = new Thickness(2),
                    ToolTip = ev.Description,
                    BorderThickness = new Thickness(0),
                    Child = new TextBlock
                    {
                        Text = "📅 " + ev.Summary,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 12
                    }
                };

                var key = (col, row);
                if (!komorki.ContainsKey(key))
                {
                    var stos = new StackPanel();
                    Grid.SetColumn(stos, col);
                    Grid.SetRow(stos, row);
                    GridKalendarza.Children.Add(stos);
                    komorki[key] = stos;
                }

                komorki[key].Children.Add(border);
            }
        }

        /// <summary>
        /// Asynchronicznie synchronizuje zadania z Google Tasks z lokalną bazą danych.
        /// </summary>
        /// <remarks>
        /// Metoda pobiera zadania z konta użytkownika w Google Tasks i sprawdza, czy zadanie nie istnieje już lokalnie,
        /// porównując tytuł, opis, datę i kategorię. Jeśli nie istnieje, zadanie jest dodawane do bazy.
        /// W przypadku braku pełnego dopasowania, użytkownik proszony jest o uzupełnienie szczegółów zadania
        /// w specjalnym oknie dialogowym (<see cref="GoogleTaskDetailsWindow"/>), które pozwala określić
        /// priorytet, kategorię i czas trwania.
        /// Po zakończeniu synchronizacji dane są zapisywane w bazie i odświeżana jest lista zadań.
        /// </remarks>
        /// <returns>Asynchroniczne zadanie typu <see cref="Task"/>, reprezentujące zakończenie procesu synchronizacji.</returns>
        private async Task SynchronizujZadaniaZGoogleAsync()
        {
            var googleTasks = await GoogleTasksHelper.GetTasksFromGoogleAsync();
            var lokalne = _context.TaskItems.ToList();

            foreach (var gtask in googleTasks)
            {
                var tytul = gtask.Title?.Trim();
                var opis = gtask.Notes?.Trim() ?? "";
                var deadline = DateTime.TryParse(gtask.Due, out var parsed) ? parsed.Date : (DateTime?)null;

                if (string.IsNullOrEmpty(tytul) || deadline == null)
                    continue;

                bool juzIstnieje = lokalne.Any(l =>
                    l.Tytul == tytul &&
                    (l.Opis?.Trim() ?? "") == opis &&
                    l.Deadline.Date == deadline.Value.Date &&
                    l.Kategoria == "Google");

                if (!juzIstnieje)
                {
                    _context.TaskItems.Add(new TaskItem
                    {
                        Tytul = tytul,
                        Opis = opis,
                        Deadline = deadline.Value,
                        Priorytet = "Średni",
                        Kategoria = "Google"
                    });
                }
            }

            foreach (var gtask in googleTasks)
            {
                var tytul = gtask.Title?.Trim();
                var deadline = DateTime.TryParse(gtask.Due, out var parsed) ? parsed.Date : (DateTime?)null;

                if (string.IsNullOrEmpty(tytul) || deadline == null)
                    continue;

                if (!lokalne.Any(l => l.Tytul == tytul && l.Deadline.Date == deadline.Value.Date))
                {
                    var okno = new GoogleTaskDetailsWindow(tytul, gtask.Notes) { Owner = this };
                    if (okno.ShowDialog() == true)
                    {
                        _context.TaskItems.Add(new TaskItem
                        {
                            Tytul = okno.Tytul,
                            Opis = okno.Opis,
                            Deadline = deadline.Value,
                            Priorytet = okno.Priorytet,
                            Kategoria = okno.Kategoria,
                            CzasTrwania = okno.CzasTrwania,
                            CzyZrealizowane = false
                        });
                    }
                }
            }
            _context.SaveChanges();
            OdswiezListe();
        }



        /// <summary>
        /// Synchronizuje zadania z Google Tasks i dodaje nowe do lokalnej bazy.
        /// </summary>
        private async void SynchronizujZadaniaZGoogle_Click(object sender, RoutedEventArgs e)
        {
            var googleTasks = await GoogleTasksHelper.GetTasksFromGoogleAsync();
            var lokalne = _context.TaskItems.ToList();

            foreach (var gtask in googleTasks)
            {
                var tytul = gtask.Title?.Trim();
                var opis = gtask.Notes?.Trim() ?? "";
                var deadline = DateTime.TryParse(gtask.Due, out var parsed) ? parsed.Date : (DateTime?)null;

                if (string.IsNullOrEmpty(tytul) || deadline == null)
                    continue;

                bool juzIstnieje = lokalne.Any(l =>
                    l.Tytul == tytul &&
                    (l.Opis?.Trim() ?? "") == opis &&
                    l.Deadline.Date == deadline.Value.Date);

                if (juzIstnieje)
                    continue;

                var okno = new GoogleTaskDetailsWindow(tytul, opis) { Owner = this };
                if (okno.ShowDialog() == true)
                {
                    _context.TaskItems.Add(new TaskItem
                    {
                        Tytul = okno.Tytul,
                        Opis = okno.Opis,
                        Deadline = deadline.Value,
                        Priorytet = okno.Priorytet,
                        Kategoria = okno.Kategoria,
                        CzasTrwania = okno.CzasTrwania,
                        CzyZrealizowane = false
                    });
                }
            }

            _context.SaveChanges();
            OdswiezListe();
            MessageBox.Show("Synchronizacja zakończona.");
        }

        /// <summary>
        /// Dodaje zaznaczone zadanie do konta Google Tasks.
        /// </summary>
        private async void DodajDoGoogle_Click(object sender, RoutedEventArgs e)
        {
            if (TaskListView.SelectedItem is TaskItem zaznaczone)
            {
                var sukces = await GoogleTasksHelper.AddTaskAsync(zaznaczone.Tytul, zaznaczone.Opis, zaznaczone.Deadline);
                if (sukces)
                {
                    MessageBox.Show("Zadanie dodane do Google Tasks.");
                }
               
            }
            else
            {
                MessageBox.Show("Zaznacz zadanie do eksportu.");
            }
        }


        /// <summary>
        /// Generuje plan dnia z pomocą OpenAI na podstawie zadań i kalendarza.
        /// </summary>
        private async void WygenerujPlan_Click(object sender, RoutedEventArgs e)
        {
            var data = DataPlanowaniaPicker.SelectedDate ?? DateTime.Today;

            var zadania = _context.TaskItems
                    .AsEnumerable()
                    .Where(t => t.Deadline.Date == data.Date)
                    .ToList();

            var wydarzenia = await GoogleCalendarHelper.GetEventsForDay(data);

            try
            {
                var prompt = AIPlannerHelper.StworzPrompt(zadania, wydarzenia, data);
                var plan = await AIPlannerHelper.WyslijZapytanieDoGPT(prompt, zadania, wydarzenia.ToList(), data);
                AIPlanTextBox.Text = plan;
            }
            catch
            {
                var lokalnyPlan = AIPlannerHelper.GenerujLokalnyPlanDnia(zadania, wydarzenia.ToList(), data);
                AIPlanTextBox.Text = lokalnyPlan;
            }
        }

        /// <summary>
        /// Obsługuje zdarzenie uzyskania fokusu przez <see cref="TextBox"/>. 
        /// Czyści domyślny tekst i ustawia czarny kolor tekstu.
        /// </summary>
        /// <param name="sender">Kontrolka, która uzyskała fokus.</param>
        /// <param name="e">Dane zdarzenia.</param>
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Foreground == Brushes.Gray)
            {
                tb.Text = "";
                tb.Foreground = Brushes.Black;
            }
        }

        /// <summary>
        /// Obsługuje zdarzenie utraty fokusu przez <see cref="TextBox"/>. 
        /// Przywraca domyślny tekst i kolor szary, jeśli pole jest puste.
        /// </summary>
        /// <param name="sender">Kontrolka, która straciła fokus.</param>
        /// <param name="e">Dane zdarzenia.</param>
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && string.IsNullOrWhiteSpace(tb.Text))
            {
                if (tb == TytulTextBox)
                    tb.Text = "Wpisz tytuł zadania";
                else if (tb == OpisTextBox)
                    tb.Text = "Wpisz opis (opcjonalnie)";
                else if (tb == CzasTrwaniaTextBox)
                    tb.Text = "hh:mm";

                tb.Foreground = Brushes.Gray;
            }
        }

        /// <summary>
        /// Obsługuje zmianę zakładki w nawigacji głównej aplikacji.
        /// Przełącza widoczność odpowiednich widoków (Zadań, Kalendarza, AI) w zależności od wybranej pozycji.
        /// W przypadku widoku kalendarza – automatycznie synchronizuje dane i generuje widok bieżącego tygodnia.
        /// </summary>
        /// <param name="sender">Lista nawigacyjna, w której zmieniono zaznaczenie.</param>
        /// <param name="e">Dane zdarzenia wyboru.</param>
        private async void NavigationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (WidokZadan == null || WidokKalendarza == null || WidokAI == null)
                return;

            WidokZadan.Visibility = Visibility.Collapsed;
            WidokKalendarza.Visibility = Visibility.Collapsed;
            WidokAI.Visibility = Visibility.Collapsed;

            switch (NavigationList.SelectedIndex)
            {
                case 0:
                    WidokZadan.Visibility = Visibility.Visible;
                    break;

                case 1:
                    WidokKalendarza.Visibility = Visibility.Visible;
                    _poczatekTygodnia = GetMonday(DateTime.Today);
                    await GenerujWidokTygodniaGoogleAsync(); 
                    break;

                case 2:
                    WidokAI.Visibility = Visibility.Visible;
                    break;
            }
        }

    }
}