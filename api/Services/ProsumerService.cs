// File: ProsumerService.cs
// Purpose: rules for registering, updating, activating and deactivating prosumers
// Author: H.M.T.S.M.Dissanayake

using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using SolarMicrogrid.Api.Models;

namespace SolarMicrogrid.Api.Services;

public class ProsumerService
{
    // shortest password accepted
    private const int MinPasswordLength = 8;

    private readonly IMongoCollection<UserDetail> users;
    private readonly PasswordHasher<UserDetail> hasher;

    // gets the user collection and the password hasher
    public ProsumerService(IMongoDatabase db, PasswordHasher<UserDetail> hasher)
    {
        users = db.GetCollection<UserDetail>("UserDetail");
        this.hasher = hasher;
    }

    // registers a new prosumer, it starts pending until backoffice activates it
    public async Task<(int Status, string? Error, UserDetail? Prosumer)> Register(ProsumerRegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nic)) return (400, "NIC is required", null);
        if (string.IsNullOrWhiteSpace(request.Password)) return (400, "Password is required", null);
        if (request.Password.Length < MinPasswordLength) return (400, $"Password must be at least {MinPasswordLength} characters", null);
        if (string.IsNullOrWhiteSpace(request.FullName)) return (400, "Full name is required", null);
        if (string.IsNullOrWhiteSpace(request.Phone)) return (400, "Phone is required", null);
        if (string.IsNullOrWhiteSpace(request.Address)) return (400, "Address is required", null);

        if (await users.Find(u => u.Nic == request.Nic).AnyAsync())
            return (400, "NIC is already registered", null);

        var prosumer = new UserDetail
        {
            Id = Guid.NewGuid().ToString(),
            Nic = request.Nic,
            Role = "Prosumer",
            FullName = request.FullName,
            Phone = request.Phone,
            Address = request.Address,
            Status = "pending"
        };
        prosumer.PasswordHash = hasher.HashPassword(prosumer, request.Password);

        await users.InsertOneAsync(prosumer);
        return (201, null, prosumer);
    }

    // lists every prosumer, pending, active and deactivated
    public async Task<List<UserDetail>> GetAll()
    {
        return await users.Find(u => u.Role == "Prosumer").SortBy(u => u.FullName).ToListAsync();
    }

    // lists prosumers waiting for backoffice to activate them
    public async Task<List<UserDetail>> GetPending()
    {
        return await users.Find(u => u.Role == "Prosumer" && u.Status == "pending").SortBy(u => u.FullName).ToListAsync();
    }

    // gets one prosumer by id, null if it does not exist
    public async Task<UserDetail?> Get(string id)
    {
        return await users.Find(u => u.Id == id && u.Role == "Prosumer").FirstOrDefaultAsync();
    }

    // looks a prosumer up by nic, a query on the unique nic field, not the record id
    public async Task<UserDetail?> GetByNic(string nic)
    {
        return await users.Find(u => u.Nic == nic && u.Role == "Prosumer").FirstOrDefaultAsync();
    }

    // backoffice updates any prosumer's profile, including the nic
    public async Task<(int Status, string? Error, UserDetail? Prosumer)> UpdateByBackoffice(string id, ProsumerUpdateRequest request)
    {
        var prosumer = await Get(id);
        if (prosumer == null) return (404, "Prosumer not found", null);

        if (string.IsNullOrWhiteSpace(request.Nic)) return (400, "NIC is required", null);
        if (string.IsNullOrWhiteSpace(request.FullName)) return (400, "Full name is required", null);
        if (string.IsNullOrWhiteSpace(request.Phone)) return (400, "Phone is required", null);
        if (string.IsNullOrWhiteSpace(request.Address)) return (400, "Address is required", null);

        var existing = await GetByNic(request.Nic);
        if (existing != null && existing.Id != id) return (400, "NIC is already registered", null);

        prosumer.Nic = request.Nic;
        prosumer.FullName = request.FullName;
        prosumer.Phone = request.Phone;
        prosumer.Address = request.Address;

        await users.ReplaceOneAsync(u => u.Id == id, prosumer);
        return (200, null, prosumer);
    }

    // a prosumer updates their own profile, the nic cannot be changed this way
    public async Task<(int Status, string? Error, UserDetail? Prosumer)> UpdateOwn(string id, ProsumerProfileRequest request)
    {
        var prosumer = await Get(id);
        if (prosumer == null) return (404, "Prosumer not found", null);
        if (string.IsNullOrWhiteSpace(request.FullName)) return (400, "Full name is required", null);
        if (string.IsNullOrWhiteSpace(request.Phone)) return (400, "Phone is required", null);
        if (string.IsNullOrWhiteSpace(request.Address)) return (400, "Address is required", null);

        prosumer.FullName = request.FullName;
        prosumer.Phone = request.Phone;
        prosumer.Address = request.Address;

        await users.ReplaceOneAsync(u => u.Id == id, prosumer);
        return (200, null, prosumer);
    }

    // a prosumer changes their own password, checks the current password first
    public async Task<(int Status, string? Error)> ChangePassword(string id, ProsumerPasswordRequest request)
    {
        var prosumer = await Get(id);
        if (prosumer == null) return (404, "Prosumer not found");
        if (string.IsNullOrWhiteSpace(request.NewPassword)) return (400, "New password is required");
        if (request.NewPassword.Length < MinPasswordLength) return (400, $"Password must be at least {MinPasswordLength} characters");

        if (hasher.VerifyHashedPassword(prosumer, prosumer.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
            return (400, "Current password is wrong");

        var newHash = hasher.HashPassword(prosumer, request.NewPassword);
        await users.UpdateOneAsync(u => u.Id == id, Builders<UserDetail>.Update.Set(u => u.PasswordHash, newHash));
        return (200, null);
    }

    // marks that a prosumer asked to be deactivated, backoffice still has to act on it
    public async Task<(int Status, string? Error)> RequestDeactivation(string id)
    {
        var prosumer = await Get(id);
        if (prosumer == null) return (404, "Prosumer not found");

        await users.UpdateOneAsync(u => u.Id == id, Builders<UserDetail>.Update.Set(u => u.DeactivationRequested, true));
        return (200, null);
    }

    // activates a pending prosumer, so they can log in
    public async Task<(int Status, string? Error)> Activate(string id)
    {
        var prosumer = await Get(id);
        if (prosumer == null) return (404, "Prosumer not found");
        if (prosumer.Status != "pending") return (400, "Prosumer is not pending activation");

        await users.UpdateOneAsync(u => u.Id == id, Builders<UserDetail>.Update.Set(u => u.Status, "active"));
        return (200, null);
    }

    // deactivates an active prosumer
    public async Task<(int Status, string? Error)> Deactivate(string id)
    {
        var prosumer = await Get(id);
        if (prosumer == null) return (404, "Prosumer not found");
        if (prosumer.Status != "active") return (400, "Prosumer is not active");

        await users.UpdateOneAsync(u => u.Id == id, Builders<UserDetail>.Update.Set(u => u.Status, "deactivated"));
        return (200, null);
    }

    // reactivates a deactivated prosumer, only backoffice can call this
    public async Task<(int Status, string? Error)> Reactivate(string id)
    {
        var prosumer = await Get(id);
        if (prosumer == null) return (404, "Prosumer not found");
        if (prosumer.Status != "deactivated") return (400, "Prosumer is not deactivated");

        await users.UpdateOneAsync(u => u.Id == id,
            Builders<UserDetail>.Update.Set(u => u.Status, "active").Set(u => u.DeactivationRequested, false));
        return (200, null);
    }
}
