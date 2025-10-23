using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Controllers;

public class MapController : Controller
{
    private readonly ApplicationDbContext _context;

    public MapController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetVotingData()
    {
        try
        {
            var votingData = await _context.Stemmers.ToListAsync();
            return Json(votingData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching voting data: {ex.Message}");
            return Json(new List<object>());
        }
    }
}