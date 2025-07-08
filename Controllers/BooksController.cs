using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookStore.Api.Data;
using BookStore.Api.Models;
using System.Security.Claims;
using BookStore.Api.Dtos;


namespace BookStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BooksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BooksController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /api/books
        [HttpGet]
        public async Task<IActionResult> GetBooks()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid or missing user ID");

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var books = role == "Admin"
                ? await _context.Books.Include(b => b.User).ToListAsync()
                : await _context.Books.Include(b => b.User).Where(b => b.UserId == userId).ToListAsync();

            var result = books.Select(b => new BookDto
            {
                Id = b.Id,
                Name = b.Name,
                Category = b.Category,
                Price = b.Price,
                Description = b.Description,
                AddedBy = b.AddedBy,
                UpdatedBy = b.UpdatedBy
            }).ToList();

            return Ok(result);
        }


        // GET: /api/books/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBook(int id)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("Invalid or missing user ID claim.");

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var book = await _context.Books.Include(b => b.User).FirstOrDefaultAsync(b => b.Id == id);
            if (book == null) return NotFound();

            if (role != "Admin" && book.UserId != userId)
                return Forbid();

            var result = new BookDto
            {
                Id = book.Id,
                Name = book.Name,
                Category = book.Category,
                Price = book.Price,
                Description = book.Description,
                AddedBy = book.AddedBy,
                UpdatedBy = book.UpdatedBy
            };

            return Ok(result);
        }


        // POST: /api/books
        [HttpPost]
        public async Task<IActionResult> CreateBook(BookCreateDto bookDto)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claim, out var userId))
                return Unauthorized("Invalid or missing user ID");

            var username = User.FindFirst(ClaimTypes.Name)?.Value
    ?? throw new UnauthorizedAccessException("Missing username claim.");


            var book = new Book
            {
                Name = bookDto.Name,
                Category = bookDto.Category,
                Price = bookDto.Price,
                Description = bookDto.Description,
                UserId = userId,
                AddedBy = username,
                UpdatedBy = username,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow

            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            var result = new BookDto
            {
                Id = book.Id,
                Name = book.Name,
                Category = book.Category,
                Price = book.Price,
                Description = book.Description,
                AddedBy = book.AddedBy,
                UpdatedBy = book.UpdatedBy
            };

            return CreatedAtAction(nameof(GetBook), new { id = book.Id }, result);
        }


        // PUT: /api/books/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBook(int id, BookCreateDto bookDto)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("Invalid or missing user ID claim.");

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var existing = await _context.Books.FindAsync(id);
            if (existing == null) return NotFound();

            if (role != "Admin" && existing.UserId != userId)
                return Forbid();

            existing.Name = bookDto.Name;
            existing.Category = bookDto.Category;
            existing.Price = bookDto.Price;
            existing.Description = bookDto.Description;
            existing.UpdatedBy = User.FindFirst(ClaimTypes.Name)?.Value
    ?? throw new UnauthorizedAccessException("Username claim is missing.");

            existing.UpdatedAt = DateTime.UtcNow;


            await _context.SaveChangesAsync();
            return NoContent();
        }


        // DELETE: /api/books/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("Invalid or missing user ID claim.");

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            // Only owner or admin can delete
            if (role != "Admin" && book.UserId != userId)
                return Forbid();

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
