using System.Text.RegularExpressions;
using AuthServer.Web.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Web.Application.Plans;

public class PlanService(AuthDbContext db)
{
    private static readonly Regex CodeRegex = new(@"^[a-z0-9][a-z0-9._-]{2,99}\z", RegexOptions.Compiled);

    public async Task<(bool Ok, string? Error, Guid? PlanId)> CreateAsync(
        Guid resourceServerId, string code, string displayName, PlanDurations duration, int? maxUsers, bool isActive = true)
    {
        code = code?.Trim().ToLowerInvariant() ?? "";
        displayName = displayName?.Trim() ?? "";

        if (!CodeRegex.IsMatch(code))
            return (false, "Code may contain only a-z, 0-9, '.', '_' and '-' (3-100 characters).", null);

        if (displayName.Length is 0 or > 200)
            return (false, "Display name must be 1-200 characters.", null);

        if (maxUsers is <= 0)
            return (false, "Max users must be positive.", null);

        if (!await db.ResourceServers.AnyAsync(r => r.Id == resourceServerId))
            return (false, "Resource server does not exist.", null);

        if (await db.Plans.AnyAsync(p => p.Code == code))
            return (false, "Code is already taken.", null);

        var plan = new PlanEntity
        {
            Id = Guid.CreateVersion7(),
            ResourceServerId = resourceServerId,
            Code = code,
            DisplayName = displayName,
            MaxUsers = maxUsers,
            IsActive = isActive,
            Duration = duration
        };

        db.Plans.Add(plan);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            if (await db.Plans.AnyAsync(p => p.Code == code))
                return (false, "Code is already taken.", null);

            throw;
        }

        return (true, null, plan.Id);
    }

    public async Task<(bool Ok, string? Error)> UpdateAsync(Guid id, string displayName, int? maxUsers)
    {
        displayName = displayName?.Trim() ?? "";

        if (displayName.Length is 0 or > 200)
            return (false, "Display name must be 1-200 characters.");

        if (maxUsers is <= 0)
            return (false, "Max users must be positive.");

        var plan = await db.Plans.SingleOrDefaultAsync(p => p.Id == id);
        if (plan is null)
            return (false, "Plan not found.");

        plan.DisplayName = displayName;
        plan.MaxUsers = maxUsers;
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> SetActiveAsync(Guid id, bool isActive)
    {
        var plan = await db.Plans.SingleOrDefaultAsync(p => p.Id == id);
        if (plan is null)
            return (false, "Plan not found.");

        if (plan.IsActive != isActive)
        {
            plan.IsActive = isActive;
            await db.SaveChangesAsync();
        }

        return (true, null);
    }

    public Task<Guid?> GetIdByCodeAsync(string code)
    {
        code = code?.Trim().ToLowerInvariant() ?? "";
        return db.Plans.Where(p => p.Code == code).Select(p => (Guid?)p.Id).SingleOrDefaultAsync();
    }
}