namespace MyBoards.Entities
{
    public class WorkItemTag // relacja wiele do wielu (tabela łącząca)
    {
        public WorkItem WorkItem { get; set; }
        public int WorkItemId { get; set; }

        public Tag Tag { get; set; }
        public int TagId { get; set; }
        public DateTime PublicationDate { get; set; }

    }
}
