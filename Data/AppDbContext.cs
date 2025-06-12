using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlanningApp.Models;
using System.Data.Entity;
using System.Runtime.Remoting.Contexts;

namespace PlanningApp.Data
{
    /// <summary>
    /// Główny kontekst bazy danych aplikacji ProductiveApp.
    /// Umożliwia komunikację z lokalną bazą danych SQLite poprzez Entity Framework.
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Inicjalizuje nowy kontekst bazy danych z użyciem połączenia o nazwie "DefaultConnection".
        /// </summary>
        public AppDbContext() : base("DefaultConnection") { }

        /// <summary>
        /// Zestaw zadań dostępnych w aplikacji – odpowiada tabeli TaskItems w bazie danych.
        /// </summary>
        public DbSet<TaskItem> TaskItems { get; set; }
    }
}
