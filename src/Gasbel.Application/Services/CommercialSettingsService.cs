using System.Text.Json;
using Gasbel.Domain.Entities;

namespace Gasbel.Application.Services;

public interface ICommercialSettingsService
{
    Task<CommercialSettings> GetSettingsAsync(CancellationToken ct = default);
    Task<CommercialSettings> UpdateSettingsAsync(CommercialSettings settings, CancellationToken ct = default);
    Task<List<SpecialDiscountClient>> GetSpecialClientsAsync(CancellationToken ct = default);
    Task<SpecialDiscountClient?> GetSpecialClientByIdAsync(Guid id, CancellationToken ct = default);
    Task<SpecialDiscountClient> AddSpecialClientAsync(SpecialDiscountClient client, CancellationToken ct = default);
    Task<SpecialDiscountClient?> UpdateSpecialClientAsync(SpecialDiscountClient client, CancellationToken ct = default);
    Task<bool> DeleteSpecialClientAsync(Guid id, CancellationToken ct = default);
}

public class CommercialSettingsService : ICommercialSettingsService
{
    private readonly string _settingsFilePath;
    private readonly string _clientsFilePath;
    private static readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public CommercialSettingsService()
    {
        var baseDir = Directory.GetCurrentDirectory();
        var dataDir = Path.Combine(baseDir, "data");
        if (!Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }

        _settingsFilePath = Path.Combine(dataDir, "commercial_settings.json");
        _clientsFilePath = Path.Combine(dataDir, "special_clients.json");
    }

    public async Task<CommercialSettings> GetSettingsAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                var defaults = new CommercialSettings();
                var json = JsonSerializer.Serialize(defaults, _jsonOptions);
                await File.WriteAllTextAsync(_settingsFilePath, json, ct);
                return defaults;
            }

            var content = await File.ReadAllTextAsync(_settingsFilePath, ct);
            return JsonSerializer.Deserialize<CommercialSettings>(content, _jsonOptions) ?? new CommercialSettings();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<CommercialSettings> UpdateSettingsAsync(CommercialSettings settings, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json, ct);
            return settings;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<SpecialDiscountClient>> GetSpecialClientsAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!File.Exists(_clientsFilePath))
            {
                var initialClients = new List<SpecialDiscountClient>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Sandra Marzán",
                        DiscountPercentage = 25m,
                        Phone = "2616799084",
                        Address = "Mendoza Capital",
                        Notes = "Cliente preferencial línea perfumería y descartables",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Distribuidora Mayorista Cuyo",
                        DiscountPercentage = 30m,
                        Phone = "2613890532",
                        Address = "Godoy Cruz, Mendoza",
                        Notes = "Cuenta especial 30% volumen recurrente",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                var json = JsonSerializer.Serialize(initialClients, _jsonOptions);
                await File.WriteAllTextAsync(_clientsFilePath, json, ct);
                return initialClients;
            }

            var content = await File.ReadAllTextAsync(_clientsFilePath, ct);
            return JsonSerializer.Deserialize<List<SpecialDiscountClient>>(content, _jsonOptions) ?? new List<SpecialDiscountClient>();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<SpecialDiscountClient?> GetSpecialClientByIdAsync(Guid id, CancellationToken ct = default)
    {
        var clients = await GetSpecialClientsAsync(ct);
        return clients.FirstOrDefault(c => c.Id == id);
    }

    public async Task<SpecialDiscountClient> AddSpecialClientAsync(SpecialDiscountClient client, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            List<SpecialDiscountClient> clients = new();
            if (File.Exists(_clientsFilePath))
            {
                var content = await File.ReadAllTextAsync(_clientsFilePath, ct);
                clients = JsonSerializer.Deserialize<List<SpecialDiscountClient>>(content, _jsonOptions) ?? new();
            }

            if (client.Id == Guid.Empty)
            {
                client.Id = Guid.NewGuid();
            }
            client.CreatedAt = DateTime.UtcNow;

            clients.Add(client);
            var json = JsonSerializer.Serialize(clients, _jsonOptions);
            await File.WriteAllTextAsync(_clientsFilePath, json, ct);
            return client;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<SpecialDiscountClient?> UpdateSpecialClientAsync(SpecialDiscountClient client, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!File.Exists(_clientsFilePath)) return null;

            var content = await File.ReadAllTextAsync(_clientsFilePath, ct);
            var clients = JsonSerializer.Deserialize<List<SpecialDiscountClient>>(content, _jsonOptions) ?? new();

            var idx = clients.FindIndex(c => c.Id == client.Id);
            if (idx == -1) return null;

            clients[idx] = client;
            var json = JsonSerializer.Serialize(clients, _jsonOptions);
            await File.WriteAllTextAsync(_clientsFilePath, json, ct);
            return client;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> DeleteSpecialClientAsync(Guid id, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!File.Exists(_clientsFilePath)) return false;

            var content = await File.ReadAllTextAsync(_clientsFilePath, ct);
            var clients = JsonSerializer.Deserialize<List<SpecialDiscountClient>>(content, _jsonOptions) ?? new();

            var removed = clients.RemoveAll(c => c.Id == id) > 0;
            if (removed)
            {
                var json = JsonSerializer.Serialize(clients, _jsonOptions);
                await File.WriteAllTextAsync(_clientsFilePath, json, ct);
            }
            return removed;
        }
        finally
        {
            _lock.Release();
        }
    }
}
