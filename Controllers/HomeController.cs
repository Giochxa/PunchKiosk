using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

   [HttpPost]
[HttpPost]
public async Task<IActionResult> SubmitPunch(string uniqueId)
{
    if (string.IsNullOrWhiteSpace(uniqueId))
        return BadRequest("Please enter an employee ID.");

    try
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.UniqueId == uniqueId && e.IsActive);

        if (employee == null)
            return BadRequest("Employee not found or inactive.");

        var punch = new PunchRecord
        {
            EmployeeId = employee.Id,
            PersonalId = employee.PersonalId,
            PunchTime = DateTime.UtcNow,
            IsSynced = false
        };

        _context.PunchRecords.Add(punch);
        await _context.SaveChangesAsync();

        return Ok($"✅ Punch saved for {GetInitials(employee.FullName)} at {punch.PunchTime.ToLocalTime():g}");
    }
    catch (Exception ex)
    {
        return BadRequest($"Error: {ex.Message}");
    }
}


  
    public IActionResult Index()
    {
        return View();
    }
   public IActionResult Intro()
    {
        return View(); // Loads Views/Punch/Intro.cshtml
    }


    private string GetInitials(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "";

        var names = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join("", names.Select(n => char.ToUpperInvariant(n[0])));
    }
}
