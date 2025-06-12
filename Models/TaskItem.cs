using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlanningApp.Models
{

    /// <summary>
    /// Reprezentuje pojedyncze zadanie w aplikacji.
    /// </summary>
    public class TaskItem
    {
        /// <summary>
        /// Id zadania.
        /// </summary>
        [Key]
        public int Id { get; set; }
        /// <summary>Tytuł zadania.</summary>
        public string Tytul { get; set; }

        /// <summary>Opis zadania.</summary>
        public string Opis { get; set; }

        /// <summary>Priorytet zadania (Niski, Średni, Wysoki).</summary>
        public string Priorytet { get; set; }

        /// <summary>Kategoria zadania (Studia, Praca, Dom).</summary>
        public string Kategoria { get; set; }

        /// <summary>Termin wykonania zadania.</summary>
        public DateTime Deadline { get; set; }

        /// <summary>Czy zadanie zostało zrealizowane.</summary>
        public bool CzyZrealizowane { get; set; }

        /// <summary>Planowany czas trwania zadania (hh:mm).</summary>
        public string CzasTrwania { get; set; }
    }

}


