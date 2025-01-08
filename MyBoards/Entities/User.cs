using System.Globalization;

namespace MyBoards.Entities
{
    public class User
    {
        public Guid Id { get; set; } // klucz główny
        public string Fullname { get; set; }
        public string Email { get; set; }

        public Address Address { get; set; }
        public List<WorkItem> WorkItems { get; set; } = new List<WorkItem>();

        public List<Comment> Comments { get; set; } = new List<Comment>();
    }
}