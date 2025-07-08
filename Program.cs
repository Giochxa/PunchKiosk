using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Services
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpClient();
builder.Services.AddScoped<SyncService>();
builder.Services.AddTransient<DataSeeder>();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

var app = builder.Build();

// 🟢 Apply migrations and seed database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

// 🔁 Background Sync Task
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

// 🔧 Middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
