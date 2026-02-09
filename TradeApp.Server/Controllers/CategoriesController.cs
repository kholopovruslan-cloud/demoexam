using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeApp.Server.Data;

namespace TradeApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly TradeDbContext _context;
    public CategoriesController(TradeDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetCategories() => Ok(await _context.Categories.ToListAsync());
}
