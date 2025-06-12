using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Tasks.v1;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GoogleTask = Google.Apis.Tasks.v1.Data.Task;
using System.Windows;

namespace PlanningApp.GoogleIntegration
{
    /// <summary>
    /// Klasa pomocnicza do integracji aplikacji z Google Tasks.
    /// </summary>
    public static class GoogleTasksHelper
    {
        static readonly string[] Scopes = { TasksService.Scope.Tasks }; 
        static readonly string ApplicationName = "ProductiveApp";
        static readonly string CredentialPath = "token_tasks.json";

        /// <summary>
        /// Zwraca ścieżkę do pliku client_secret.json
        /// </summary>
        private static string GetClientSecretPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "client_secret.json");
        }

        /// <summary>
        /// Tworzy i zwraca instancję TasksService
        /// </summary>
        private static async Task<TasksService> GetServiceAsync()
        {
            var secretPath = GetClientSecretPath();

            if (!File.Exists(secretPath))
            {
                MessageBox.Show($"Brakuje pliku client_secret.json:\n\n{secretPath}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            UserCredential credential;
            using (var stream = new FileStream(secretPath, FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(CredentialPath, true));
            }

            return new TasksService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });
        }

        /// <summary>
        /// Pobiera zadania z domyślnej listy Google Tasks.
        /// </summary>
        public static async Task<List<GoogleTask>> GetTasksFromGoogleAsync()
        {
            var service = await GetServiceAsync();
            if (service == null)
                return new List<GoogleTask>();

            var request = service.Tasks.List("@default");
            var result = await request.ExecuteAsync();
            return result.Items?.ToList() ?? new List<GoogleTask>();
        }

        /// <summary>
        /// Dodaje jedno zadanie do Google Tasks.
        /// </summary>
        public static async Task<bool> AddTaskAsync(string title, string description, DateTime deadline)
        {
            var service = await GetServiceAsync();
            if (service == null)
                return false;

            try
            {
                var task = new GoogleTask
                {
                    Title = title,
                    Notes = description,
                    Due = deadline.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'")
                };

                await service.Tasks.Insert(task, "@default").ExecuteAsync();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas dodawania zadania do Google Tasks:\n" + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}


