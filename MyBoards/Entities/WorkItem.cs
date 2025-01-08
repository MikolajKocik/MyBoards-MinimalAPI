using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MyBoards.Entities
{
    public class Epic : WorkItem
    {
        public DateTime? StartDate { get; set; } // wartości w tej kolumnie są opcjonalne "DateTime"'?'
        public DateTime? EndDate { get; set; } // mogą być nullem

    }

    public class Issue : WorkItem
    {
        public decimal Efford { get; set; }
    }

    public class Task : WorkItem
    {
        public string Activity { get; set; }
        public decimal RemaingWork { get; set; }
    }

    public abstract class WorkItem
    {

        public int Id { get; set; } // klucz główny
        public WorkItemState State { get; set; }
        public int StateId { get; set; }
        public string Area { get; set; }
        public string IterationPath { get; set; }
        public int Priority { get; set; }


        // relacja 1 do wielu -> dla comments
        public List<Comment> Comments { get; set; } = new List<Comment>(); // pusta zamiast null

        // relacja jeden do wielu -> dla user + workItem

        public User Author { get; set; }

        public Guid AuthorId { get; set; } // klucz obcy dla tabeli 'User', typ musi się zgadzać dlatego 'Guid'

        public List<Tag> Tags { get; set; }
    }
}
