namespace AuthServer.Web.Persistence;

public enum PlanDurations { Month, Year, Infinite }

public static class PlanDuartionExtensionMethods
{
    extension(PlanDurations duration)
    {
        public DateTimeOffset? GetEndDate(DateTimeOffset start) =>
            duration switch
            {
                PlanDurations.Month => start.AddMonths(1),
                PlanDurations.Year => start.AddYears(1),
                PlanDurations.Infinite => null,
                _ => throw new ArgumentOutOfRangeException(nameof(duration)),
            };
    }
}

public class PlanEntity
{
    public Guid Id { get; set; }
    public Guid ResourceServerId { get; set; }
    public PlanDurations Duration { get; set; }
    public virtual ResourceServerEntity ResourceServer { get; set; } = null!;
    public string Code { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int? MaxUsers { get; set; }
    public bool IsActive { get; set; } = true;
}
