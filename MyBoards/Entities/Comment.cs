namespace MyBoards.Entities
{
    public class Comment
    {
        public int Id { get; set; } // klucz główny
        public string Message { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; } // nie każdy komentarz musi mieć update '?'

        // relacja jeden do wielu

        public  WorkItem WorkItem { get; set; }

        public int WorkItemId { get; set; }

        public  User Author { get; set; }
        public Guid AuthorId { get; set; }


    }
}
