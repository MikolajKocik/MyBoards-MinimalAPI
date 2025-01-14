namespace MyBoards.Dto
{
    public class EpicDto
    {
        public int Id { get; set; }
        public int Priority { get; set; }
        public string Area { get; set; }
        public DateTime? StarDate { get; set; }
        public string AuthorFullName { get; set; }
    }
}
