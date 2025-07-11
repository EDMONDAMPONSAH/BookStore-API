using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookStore.Api.Data;
using BookStore.Api.Dtos;
using System.Linq;

namespace BookStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /api/home
        [HttpGet]
        public async Task<IActionResult> GetAllBooks(
            string? search,
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.Books
                .Include(b => b.Images)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalized = search.Trim().ToLower();
                query = query.Where(b =>
                    b.Name.ToLower().Contains(normalized) ||
                    b.Category.ToLower().Contains(normalized));
            }

            var total = await query.CountAsync();

            var booksData = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var books = booksData.Select(b => new BookListDto
            {
                Id = b.Id,
                Name = b.Name,
                Price = b.Price,
                FirstImageUrl = b.Images.FirstOrDefault()?.Url
            }).ToList();

            return Ok(new
            {
                total,
                page,
                pageSize,
                data = books
            });
        }



        // GET: /api/home/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookById(int id)
        {
            var book = await _context.Books
                .Include(b => b.Images)
                .Where(b => b.Id == id)
                .Select(b => new BookDetailDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Price = b.Price,
                    Description = b.Description,
                    Category = b.Category,
                    Images = b.Images.Select(img => img.Url).ToList()
                })
                .FirstOrDefaultAsync();

            if (book == null)
                return NotFound("Book not found");

            return Ok(book);
        }

    }
}
