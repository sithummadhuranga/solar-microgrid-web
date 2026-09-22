// File: AuthService.cs
// Purpose: sets up user accounts, unique fields, password hashing, the first backoffice user
// Author: H.M.T.S.M.Dissanayake

using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using SolarMicrogrid.Api.Models;

namespace SolarMicrogrid.Api.Services;

public class AuthService
{
    private readonly IMongoCollection<UserDetail> users;
    private readonly PasswordHasher<UserDetail> hasher;

    // gets the user collection and the password hasher
    public AuthService(IMongoDatabase db, PasswordHasher<UserDetail> hasher)
    {
        users = db.GetCollection<UserDetail>("UserDetail");
        this.hasher = hasher;
    }

    // nic must be unique for prosumers, email must be unique for backoffice and grid operator
    // mongo partial indexes only allow equality checks, so email gets one index per role
    public async Task EnsureIndexes()
    {
        var nicIndex = new CreateIndexModel<UserDetail>(
            Builders<UserDetail>.IndexKeys.Ascending(u => u.Nic),
            new CreateIndexOptions<UserDetail>
            {
                Name = "nic_unique_prosumer",
                Unique = true,
                PartialFilterExpression = Builders<UserDetail>.Filter.Eq(u => u.Role, "Prosumer")
            });

        var backofficeEmailIndex = new CreateIndexModel<UserDetail>(
            Builders<UserDetail>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions<UserDetail>
            {
                Name = "email_unique_backoffice",
                Unique = true,
                PartialFilterExpression = Builders<UserDetail>.Filter.Eq(u => u.Role, "Backoffice")
            });

        var gridOperatorEmailIndex = new CreateIndexModel<UserDetail>(
            Builders<UserDetail>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions<UserDetail>
            {
                Name = "email_unique_gridoperator",
                Unique = true,
                PartialFilterExpression = Builders<UserDetail>.Filter.Eq(u => u.Role, "GridOperator")
            });

        await users.Indexes.CreateManyAsync([nicIndex, backofficeEmailIndex, gridOperatorEmailIndex]);
    }

    // makes the first backoffice user, only if one does not exist yet
    public async Task SeedBackofficeUser(string email, string fullName, string password)
    {
        var exists = await users.Find(u => u.Role == "Backoffice").AnyAsync();
        if (exists) return;

        var user = new UserDetail
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            Role = "Backoffice",
            FullName = fullName,
            Status = "active"
        };
        user.PasswordHash = hasher.HashPassword(user, password);

        await users.InsertOneAsync(user);
    }
}
