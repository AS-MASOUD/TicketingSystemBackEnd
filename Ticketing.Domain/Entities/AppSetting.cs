using Ticketing.Domain.Common;

namespace Ticketing.Domain.Entities;

public class AppSetting : BaseEntity<int>
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
