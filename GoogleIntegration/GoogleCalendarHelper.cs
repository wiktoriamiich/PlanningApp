using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Tasks.v1;
using Google.Apis.Tasks.v1.Data;
using PlanningApp.Properties;
using System.Windows;

namespace PlanningApp.GoogleIntegration
{

    /// <summary>
    /// Klasa pomocnicza do integracji aplikacji z kalendarzem Google.
    /// Umożliwia pobieranie nadchodzących wydarzeń z kalendarza.
    /// </summary>
    public class GoogleCalendarHelper
    {
        static readonly string[] Scopes = { CalendarService.Scope.CalendarReadonly };
        static readonly string ApplicationName = "ProductiveApp";
        static readonly string CredentialPath = "token_calendar.json";

        /// <summary>
        /// Pobiera listę nadchodzących wydarzeń z domyślnego kalendarza Google użytkownika.
        /// </summary>
        /// <returns>Lista obiektów Event z nadchodzącymi wydarzeniami.</returns>
        public static async Task<IList<Event>> GetUpcomingEventsAsync()
        {
            UserCredential credential;

            string secretPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "client_secret.json");

            if (!File.Exists(secretPath))
            {
                MessageBox.Show($"Nie znaleziono pliku client_secret.json:\n{secretPath}", "Błąd autoryzacji", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<Event>();
            }

            using (var stream = new FileStream(secretPath, FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(CredentialPath, true));
            }

            var service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            var request = service.Events.List("primary");
            request.TimeMin = DateTime.UtcNow.Date; 
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.MaxResults = 250; 
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            var events = await request.ExecuteAsync();
            return events.Items ?? new List<Event>();
        }

        /// <summary>
        /// Pobiera wydarzenia z kalendarza Google dla konkretnego dnia.
        /// </summary>
        /// <param name="day">Data, dla której mają zostać pobrane wydarzenia.</param>
        /// <returns>Lista wydarzeń przypadających na wskazany dzień.</returns>
        public static async Task<IList<Event>> GetEventsForDay(DateTime day)
        {
            var allEvents = await GetUpcomingEventsAsync();
            var start = day.Date;
            var end = start.AddDays(1);

            return allEvents
                .Where(ev => ev.Start?.DateTime >= start &&
                             ev.Start?.DateTime < end)
                .ToList();
        }

    }
}
