namespace BookStore.Api.Dtos
{
    public class BookCreateDto
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
    }
}
