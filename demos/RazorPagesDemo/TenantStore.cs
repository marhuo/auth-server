using System.Text.Json;

namespace RazorPagesDemo;

// The demo's own data about a tenant: whether it has paid. The name is in the auth server.
public record Tenant(Guid Id, bool Paid, DateTimeOffset? PaidAt);

// Stores the tenants in a JSON file instead of a database.
public class TenantStore(string path)
{
    private readonly Lock _lock = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public Tenant? Get(Guid id)
    {
        lock (_lock)
            return Load().GetValueOrDefault(id);
    }

    public void Save(Tenant tenant)
    {
        lock (_lock)
        {
            var all = Load();
            all[tenant.Id] = tenant;

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(all.Values, JsonOptions));
        }
    }

    private Dictionary<Guid, Tenant> Load()
    {
        if (!File.Exists(path))
            return [];

        var list = JsonSerializer.Deserialize<List<Tenant>>(File.ReadAllText(path)) ?? [];
        return list.ToDictionary(t => t.Id);
    }
}
