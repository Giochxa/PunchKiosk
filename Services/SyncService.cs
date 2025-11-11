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

        if (!unsynced.Any())
        {
            Console.WriteLine("[PushPunchesAsync] No unsynced punches found.");
            return;
        }

        foreach (var punch in unsynced)
        {
            // ✅ Null-safe file handling
            var imageFileName = Path.GetFileName(punch.ImagePath ?? string.Empty);
            string base64Image = string.Empty;

            if (!string.IsNullOrEmpty(imageFileName))
            {
                var fullImagePath = Path.Combine("wwwroot", "punch_images", imageFileName);

                if (File.Exists(fullImagePath))
                {
                    try
                    {
                        base64Image = Convert.ToBase64String(await File.ReadAllBytesAsync(fullImagePath));
                    }
                    catch (Exception ioEx)
                    {
                        Console.WriteLine($"[PushPunchesAsync] Error reading image file: {ioEx.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"[PushPunchesAsync] ⚠️ Image file missing: {fullImagePath}");
                }
            }

            // ✅ Build payload using correct server-side JSON property names
            var payload = new PunchSyncRequest
            {
                PersonalId = punch.PersonalId ?? punch.Employee?.PersonalId,
                PunchTime = punch.PunchTime.ToUniversalTime(),  // send UTC time
                ImageBase64 = base64Image
            };

            // ✅ Log outgoing payload
            Console.WriteLine($"➡️ Sync payload: {System.Text.Json.JsonSerializer.Serialize(payload)}");

            try
            {
                var response = await _http.PostAsJsonAsync($"{_serverUrl}/api/punches", payload);

                if (response.IsSuccessStatusCode)
                {
                    punch.IsSynced = true;
                    punch.SyncedAt = DateTime.UtcNow;
                    Console.WriteLine($"✅ Synced punch for PersonalId: {payload.PersonalId}");
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ [PushPunchesAsync] Server error: {response.StatusCode} → {errorBody}");
                }
            }
            catch (Exception httpEx)
            {
                Console.WriteLine($"[PushPunchesAsync] HTTP error: {httpEx.Message}");
            }
        }

        await _context.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[PushPunchesAsync] Error: {ex.Message}");
        if (ex.InnerException != null)
            Console.WriteLine($"[PushPunchesAsync] Inner: {ex.InnerException.Message}");
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
