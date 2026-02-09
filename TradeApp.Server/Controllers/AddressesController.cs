using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeApp.Server.Data;

namespace TradeApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AddressesController : ControllerBase
{
    private readonly TradeDbContext _context;
    public AddressesController(TradeDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAddresses() => Ok(await _context.Addresses.ToListAsync());
}
