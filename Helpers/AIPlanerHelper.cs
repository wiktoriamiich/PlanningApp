using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Google.Apis.Calendar.v3.Data;
using PlanningApp.Models;

namespace PlanningApp.Helpers
{
    /// <summary>
    /// Klasa pomocnicza odpowiedzialna za generowanie promptów i wysyłanie ich do modelu GPT.
    /// Obsługuje również lokalne tworzenie planu dnia na podstawie zadań i wydarzeń.
    /// </summary>
    public static class AIPlannerHelper
    {
        /// <summary>
        /// Pobiera klucz API z pliku launchSettings.json przy pierwszym użyciu.
        /// </summary>
        private static readonly string apiKey = GetApiKeyFromLaunchSettings();

        /// <summary>
        /// Tworzy prompt tekstowy na podstawie zadań, wydarzeń i daty planowania.
        /// </summary>
        /// <param name="zadania">Lista zadań użytkownika.</param>
        /// <param name="wydarzenia">Lista wydarzeń z kalendarza Google.</param>
        /// <param name="data">Wybrana data planowania.</param>
        /// <returns>Prompt tekstowy gotowy do wysłania do GPT.</returns>
        public static string StworzPrompt(List<TaskItem> zadania, IList<Event> wydarzenia, DateTime data)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Dzisiaj jest {data:dddd, dd MMMM yyyy}.");
            sb.AppendLine("Oto lista wydarzeń z kalendarza:");
            foreach (var ev in wydarzenia)
            {
                var start = ev.Start?.DateTime?.ToString("HH:mm") ?? "brak godziny";
                var end = ev.End?.DateTime?.ToString("HH:mm") ?? "brak godziny";
                sb.AppendLine($"- {start}–{end}: {ev.Summary}");
            }

            sb.AppendLine();
            sb.AppendLine("Lista zadań do zaplanowania:");
            foreach (var z in zadania)
            {
                sb.AppendLine($"- {z.Tytul} ({z.Priorytet}, {z.CzasTrwania})");
            }

            sb.AppendLine();
            sb.AppendLine("Na podstawie powyższych informacji zaplanuj mój dzień, tak by był jak najbardziej produktywny. Weź pod uwagę priorytety zadań, czas trwania oraz dostępne okna czasowe. Uporządkuj plan w formie harmonogramu godzinowego.");

            return sb.ToString();
        }

        /// <summary>
        /// Wysyła prompt do modelu GPT przez OpenAI API i zwraca wygenerowany plan dnia.
        /// Jeśli połączenie nie powiedzie się, zwraca lokalnie wygenerowany plan.
        /// </summary>
        /// <param name="prompt">Treść zapytania do GPT.</param>
        /// <param name="zadania">Lista zadań.</param>
        /// <param name="wydarzenia">Lista wydarzeń z kalendarza.</param>
        /// <param name="data">Data planowania.</param>
        /// <returns>Wygenerowany plan dnia jako tekst.</returns>
        public static async Task<string> WyslijZapytanieDoGPT(string prompt, List<TaskItem> zadania, List<Event> wydarzenia, DateTime data)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                    var content = new
                    {
                        model = "gpt-3.5-turbo",
                        messages = new[]
                        {
                            new { role = "system", content = "Jesteś osobistym asystentem ds. produktywności." },
                            new { role = "user", content = prompt }
                        },
                        temperature = 0.7
                    };

                    var json = JsonConvert.SerializeObject(content);
                    var response = await client.PostAsync("https://api.openai.com/v1/chat/completions",
                        new StringContent(json, Encoding.UTF8, "application/json"));

                    response.EnsureSuccessStatusCode();
                    var responseString = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(responseString);
                    return result.choices[0].message.content;
                }
            }
            catch (Exception)
            {
                return GenerujLokalnyPlanDnia(zadania, wydarzenia, data);
            }
        }

        /// <summary>
        /// Generuje plan dnia lokalnie bez połączenia z API.
        /// Uwzględnia priorytety zadań oraz dostępne okna czasowe.
        /// </summary>
        /// <param name="zadania">Lista zadań.</param>
        /// <param name="wydarzenia">Lista wydarzeń.</param>
        /// <param name="data">Data dnia planowania.</param>
        /// <returns>Plan dnia jako tekst.</returns>
        public static string GenerujLokalnyPlanDnia(List<TaskItem> zadania, List<Event> wydarzenia, DateTime data)
        {
            var startDnia = data.Date.AddHours(8);
            var koniecDnia = data.Date.AddHours(18);

            var blokiZajete = wydarzenia
                .Where(e => e.Start?.DateTime != null && e.End?.DateTime != null)
                .Select(e => Tuple.Create(e.Start.DateTime.Value, e.End.DateTime.Value))
                .OrderBy(b => b.Item1)
                .ToList();

            var wolneSloty = new List<Tuple<DateTime, DateTime>>();
            var aktualny = startDnia;

            foreach (var blok in blokiZajete)
            {
                if (blok.Item1 > aktualny)
                    wolneSloty.Add(Tuple.Create(aktualny, blok.Item1));
                if (blok.Item2 > aktualny)
                    aktualny = blok.Item2;
            }

            if (aktualny < koniecDnia)
                wolneSloty.Add(Tuple.Create(aktualny, koniecDnia));

            var priorytetMap = new Dictionary<string, int> { { "wysoki", 0 }, { "średni", 1 }, { "niski", 2 } };
            var zadaniaDoZaplanowania = zadania
                .Where(z => z.Deadline.Date == data.Date)
                .OrderBy(z =>
                {
                    var key = z.Priorytet?.ToLower() ?? "średni";
                    return priorytetMap.TryGetValue(key, out int val) ? val : 1;
                })

                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("**(tryb offline – plan lokalny)**");

            foreach (var zad in zadaniaDoZaplanowania)
            {
                if (!TimeSpan.TryParse(zad.CzasTrwania, out var trwanie))
                    trwanie = TimeSpan.FromMinutes(30);

                for (int i = 0; i < wolneSloty.Count; i++)
                {
                    var slot = wolneSloty[i];
                    if (slot.Item2 - slot.Item1 >= trwanie)
                    {
                        var start = slot.Item1;
                        var end = start + trwanie;
                        sb.AppendLine($"{start:HH:mm}–{end:HH:mm} – {zad.Tytul} ({zad.Priorytet}, {zad.Kategoria})");
                        wolneSloty[i] = Tuple.Create(end, slot.Item2);
                        break;
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Pomocnicza metoda do pobrania klucza OpenAI API z launchSettings.json (Environment Variable).
        /// </summary>
        /// <returns>Klucz API jako string.</returns>
        private static string GetApiKeyFromLaunchSettings()
        {
            var key = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(key))
            {
                throw new InvalidOperationException("Brak klucza API. Ustaw zmienną środowiskową OPENAI_API_KEY w launchSettings.json.");
            }
            return key;
        }
    }
}

