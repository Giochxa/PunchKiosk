using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Add services
builder.Services.AddControllersWithViews();

// Register DB context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register SyncService and HttpClient
builder.Services.AddHttpClient();
builder.Services.AddScoped<SyncService>();

// Register IConfiguration if needed in other services manually
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

var app = builder.Build();

// Background sync task
app.Lifetime.ApplicationStarted.Register(() =>
{
    Task.Run(async () =>
    {
        using var scope = app.Services.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<SyncService>();

        int minuteCounter = 0;
        while (true)
        {
            try
            {
                await syncService.PushPunchesAsync();

                // Sync employees every 10 minutes
                if (minuteCounter % 10 == 0)
                {
                    await syncService.SyncEmployeesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncService] Error: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromMinutes(1));
            minuteCounter++;
        }
    });
});


// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Intro}/{id?}");

app.Run();
