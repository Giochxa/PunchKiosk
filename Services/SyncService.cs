using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

public class SyncService
{
    private readonly AppDbContext _context;
    private readonly HttpClient _http;
    private readonly string _serverUrl;

    public SyncService(AppDbContext context, IHttpClientFactory httpFactory, IConfiguration config)
    {
        _context = context;
        _http = httpFactory.CreateClient();
        _serverUrl = config["SyncSettings:MainServerUrl"] 
                     ?? throw new ArgumentNullException("MainServerUrl not found in config.");
    }

    public async Task SyncEmployeesAsync()
    {
        try
        {
            var remote = await _http.GetFromJsonAsync<List<Employee>>($"{_serverUrl}/api/employees");
            if (remote == null) return;

            var local = await _context.Employees.ToListAsync();

            foreach (var remoteEmp in remote)
            {
                var localEmp = local.FirstOrDefault(e => e.UniqueId == remoteEmp.UniqueId);

                if (localEmp != null)
                {
                    localEmp.FullName = remoteEmp.FullName;
                    localEmp.IsActive = remoteEmp.IsActive;
                    localEmp.PersonalId = remoteEmp.PersonalId;
                }
                else
                {
                    var newEmp = new Employee
                    {
                        UniqueId = remoteEmp.UniqueId,
                        FullName = remoteEmp.FullName,
                        IsActive = remoteEmp.IsActive,
                        PersonalId = remoteEmp.PersonalId
                    };

                    _context.Employees.Add(newEmp);
                }
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SyncEmployeesAsync] Error: {ex.Message}");
            // TODO: Add logging if needed
        }
    }

    public async Task PushPunchesAsync()
{
    try
    {
        var unsynced = await _context.PunchRecords
            .Include(p => p.Employee)
            .Where(p => !p.IsSynced)
            .ToListAsync();

        if (!unsynced.Any()) return;

        foreach (var punch in unsynced)
        {
            var imageFileName = Path.GetFileName(punch.ImagePath);
            var fullImagePath = Path.Combine("wwwroot", "punch_images", imageFileName);

            if (!File.Exists(fullImagePath))
            {
                Console.WriteLine($"[PushPunchesAsync] Image file missing: {fullImagePath}");
                continue; // or punch.IsSynced = true; to skip
            }

            var base64Image = Convert.ToBase64String(await File.ReadAllBytesAsync(fullImagePath));

            var payload = new PunchRequest
            {
                UniqueId = punch.Employee?.UniqueId,
                PunchTime = punch.PunchTime,
                ImageBase64 = base64Image
            };

            var response = await _http.PostAsJsonAsync($"{_serverUrl}/api/punches", payload);
                if (response.IsSuccessStatusCode)
                {
                    punch.IsSynced = true;
                    punch.SyncedAt = DateTime.UtcNow;
                }
                else
                {
                    Console.WriteLine($"[PushPunchesAsync] Server error: {response.StatusCode}");
                }
        }

        await _context.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[PushPunchesAsync] Error: {ex.Message}");
    }
}

    public async Task SubmitPunchAsync(int employeeId)
    {
        var punch = new PunchRecord
        {
            EmployeeId = employeeId,
            PunchTime = DateTime.UtcNow,
            IsSynced = false
        };

        _context.PunchRecords.Add(punch);
        await _context.SaveChangesAsync();

        try
        {
            await PushPunchesAsync();
        }
        catch
        {
            // Safe to ignore
        }
    }
}
