namespace MyBoards.Entities
{
    public class Tag
    {
        public int Id { get; set; } // klucz główny
        public string Value { get; set; }
        public string Category { get; set; }    

        public List<WorkItem> WorkItems { get; set; } 
    }
}
