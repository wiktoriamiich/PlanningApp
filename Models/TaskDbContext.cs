using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using PlanningApp.Models;
using System.Runtime.Remoting.Contexts;

namespace PlanningApp.Models
{
    /// <summary>
    /// Kontekst bazy danych aplikacji zarządzający zadaniami.
    /// </summary>
    public class TaskDbContext : DbContext
    {
        /// <summary>
        /// Kolekcja zadań przechowywana w bazie danych.
        /// </summary>
        public DbSet<TaskItem> TaskItems { get; set; }

        /// <summary>
        /// Inicjalizuje nowy kontekst z połączeniem do domyślnej bazy danych.
        /// </summary>
        public TaskDbContext() : base("DefaultConnection") { }
    }

}