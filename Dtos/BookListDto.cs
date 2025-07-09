namespace BookStore.Api.Dtos
{
    public class BookListDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
        public string? FirstImageUrl { get; set; }
    }
}
