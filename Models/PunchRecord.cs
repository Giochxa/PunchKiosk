public class PunchRecord
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateTime PunchTime { get; set; }
    public string? ImagePath { get; set; }

    // public bool IsPunchIn { get; set; }
    public bool IsSynced { get; set; } = false;
    // ✅ New field to keep PersonalId for sync reference
    public string? PersonalId { get; set; }
    public Employee Employee { get; set; }
    public DateTime? SyncedAt { get; set; }

}
