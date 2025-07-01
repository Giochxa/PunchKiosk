using System.Threading.Tasks;
using System.Linq;

public class DataSeeder
{
    private readonly AppDbContext _context;

    public DataSeeder(AppDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        // Seed Employees only if none exist yet
        if (!_context.Employees.Any())
        {
            _context.Employees.AddRange(
                new Employee { UniqueId = "1001", FullName = "Alice Smith", IsActive = true },
                new Employee { UniqueId = "1002", FullName = "Bob Johnson", IsActive = true },
                new Employee { UniqueId = "1003", FullName = "Boby Fox", IsActive = true },
                new Employee { UniqueId = "1004", FullName = "Charlie Brown", IsActive = false }
            );

            await _context.SaveChangesAsync();
        }

        // You can seed other data similarly
    }
}
