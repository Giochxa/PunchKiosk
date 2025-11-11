using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json.Serialization;

[ApiController]
[Route("api/[controller]")]
public class PunchController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public PunchController(AppDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public class PunchRequest
    {
        [JsonPropertyName("employeeId")]
        public string? UniqueId { get; set; }  // ✅ now matches kiosk input

        [JsonPropertyName("punchTime")]
        public DateTime PunchTime { get; set; } = DateTime.MinValue;

        [JsonPropertyName("photoData")]
        public string? ImageBase64 { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> Punch([FromBody] PunchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UniqueId))
            return BadRequest(new { error = "UniqueId is required" });

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.UniqueId == request.UniqueId && e.IsActive); // ✅ changed from PersonalId

        if (employee == null)
            return BadRequest(new { error = "Invalid or inactive employee ID" });

        var punchTime = request.PunchTime == default || request.PunchTime == DateTime.MinValue
            ? DateTime.UtcNow
            : request.PunchTime;

        string? imageFileName = null;
        if (!string.IsNullOrWhiteSpace(request.ImageBase64))
        {
            try
            {
                var base64Data = request.ImageBase64.Contains(",")
                    ? request.ImageBase64.Split(',').Last()
                    : request.ImageBase64;

                var imageBytes = Convert.FromBase64String(base64Data);
                var imagesFolder = Path.Combine(_environment.WebRootPath, "punch_images");

                if (!Directory.Exists(imagesFolder))
                    Directory.CreateDirectory(imagesFolder);

                imageFileName = $"punch_{employee.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.png";
                var imagePath = Path.Combine(imagesFolder, imageFileName);
                await System.IO.File.WriteAllBytesAsync(imagePath, imageBytes);
            }
            catch (FormatException)
            {
                return BadRequest(new { error = "Invalid image format" });
            }
        }

        var punch = new PunchRecord
        {
            EmployeeId = employee.Id,
            PersonalId = employee.PersonalId,  // ✅ keep PersonalId for sync
            PunchTime = punchTime,
            IsSynced = false,
            ImagePath = imageFileName
        };

        _context.PunchRecords.Add(punch);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Punch recorded successfully",
            image = imageFileName != null ? $"/punch_images/{imageFileName}" : null,
            employeeInitials = GetInitials(employee.FullName)
        });
    }

    private string GetInitials(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "";

        var names = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var initialsWithDots = string.Join(".", names.Select(n => char.ToUpperInvariant(n[0]))) + ".";
        return initialsWithDots;
    }
}
