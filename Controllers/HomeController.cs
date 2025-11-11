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
public async Task<IActionResult> SubmitPunch(string uniqueId)
{
    if (string.IsNullOrWhiteSpace(uniqueId))
    {
        ViewBag.Result = "Please enter an employee ID.";
        return View("Index");
    }

    try
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.UniqueId == uniqueId && e.IsActive);

        if (employee == null)
        {
            ViewBag.Result = "Employee not found or inactive.";
            return View("Index");
        }

        // ✅ Store PersonalId for sync
        var punch = new PunchRecord
{
    EmployeeId = employee.Id,
    PersonalId = employee.PersonalId,   // ✅ store PersonalId for syncing
    PunchTime = DateTime.UtcNow,
    IsSynced = false,
    Employee = employee
};

        _context.PunchRecords.Add(punch);
        await _context.SaveChangesAsync();

        ViewBag.Result = $"Punch saved for {GetInitials(employee.FullName)} at {punch.PunchTime.ToLocalTime():g}";
    }
    catch (Exception ex)
    {
        ViewBag.Result = $"Error: {ex.Message}";
    }

    return View("Index");
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
