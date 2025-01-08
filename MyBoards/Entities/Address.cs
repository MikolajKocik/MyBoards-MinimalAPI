namespace MyBoards.Entities
{
    public class Address
    {
        public Guid Id { get; set; } // klucz główny
        public string? Country { get; set; }
        public string City { get; set; }
        public string Street { get; set; }
        public string? PostalCode { get; set; }

        public User User { get; set; }
        public Guid UserId { get; set; } // musi odpowiadać właściwości referencji do encji 'User'
    }
}
