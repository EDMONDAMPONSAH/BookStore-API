namespace BookStore.Api.Dtos
{
    public class PaymentDto
    {
        public string Email { get; set; } = string.Empty;
        public int BookId { get; set; }
        public int BuyerId { get; set; }
    }
}
