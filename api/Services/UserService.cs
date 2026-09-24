// File: UserService.cs
// Purpose: rules for creating and updating backoffice and grid operator accounts
// Author: H.M.T.S.M.Dissanayake

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using SolarMicrogrid.Api.Models;

namespace SolarMicrogrid.Api.Services;

public class UserService
{
    // shortest password accepted, checked again here even though the client already checks it
    private const int MinPasswordLength = 8;

    // a plain email shape, not a full spec, good enough to catch a typo
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    private readonly IMongoCollection<UserDetail> users;
    private readonly PasswordHasher<UserDetail> hasher;

    // gets the user collection and the password hasher
    public UserService(IMongoDatabase db, PasswordHasher<UserDetail> hasher)
    {
        users = db.GetCollection<UserDetail>("UserDetail");
        this.hasher = hasher;
    }

    // lists backoffice and grid operator users, prosumers are not staff accounts
    public async Task<List<UserDetail>> GetAll()
    {
        return await users.Find(u => u.Role == "Backoffice" || u.Role == "GridOperator")
            .SortBy(u => u.FullName)
            .ToListAsync();
    }

    // gets one staff user by id, null if it is not a staff account
    public async Task<UserDetail?> Get(string id)
    {
        return await users.Find(u => u.Id == id && (u.Role == "Backoffice" || u.Role == "GridOperator")).FirstOrDefaultAsync();
    }

    // adds a new backoffice or grid operator user, it starts active
    public async Task<(int Status, string? Error, UserDetail? User)> Create(UserRequest request)
    {
        var error = await Check(request, null);
        if (error != null) return (400, error, null);

        var user = new UserDetail
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            Role = request.Role,
            FullName = request.FullName,
            Status = "active"
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password!);

        await users.InsertOneAsync(user);
        return (201, null, user);
    }

    // changes the name, email and role of a staff user, changes the password only when one is sent
    public async Task<(int Status, string? Error, UserDetail? User)> Update(string id, UserRequest request)
    {
        var user = await Get(id);
        if (user == null) return (404, "User not found", null);

        var error = await Check(request, id);
        if (error != null) return (400, error, null);

        user.Email = request.Email;
        user.Role = request.Role;
        user.FullName = request.FullName;
        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = hasher.HashPassword(user, request.Password);

        await users.ReplaceOneAsync(u => u.Id == id, user);
        return (200, null, user);
    }

    // checks the user fields, returns an error message or null when they are fine
    private async Task<string?> Check(UserRequest request, string? excludingId)
    {
        if (string.IsNullOrWhiteSpace(request.Email)) return "Email is required";
        if (!EmailPattern.IsMatch(request.Email)) return "Email is not a valid address";
        if (string.IsNullOrWhiteSpace(request.FullName)) return "Full name is required";
        if (request.Role != "Backoffice" && request.Role != "GridOperator") return "Role must be Backoffice or GridOperator";

        if (excludingId == null && string.IsNullOrWhiteSpace(request.Password)) return "Password is required";
        if (!string.IsNullOrWhiteSpace(request.Password) && request.Password.Length < MinPasswordLength)
            return $"Password must be at least {MinPasswordLength} characters";

        var existing = await users.Find(u => u.Email == request.Email).FirstOrDefaultAsync();
        if (existing != null && existing.Id != excludingId) return "Email is already used";

        return null;
    }
}
