namespace BookStore.Api.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime DatePaid { get; set; }
        public string Status { get; set; } = "pending"; // success, failed, pending
        public int BookId { get; set; }
        public int BuyerId { get; set; }

        public Book Book { get; set; }
        public User Buyer { get; set; }
    }
}
