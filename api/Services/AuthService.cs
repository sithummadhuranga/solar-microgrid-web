// File: AuthService.cs
// Purpose: checks logins and keeps the nic and email fields unique
// Author: H.M.T.S.M.Dissanayake

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using SolarMicrogrid.Api.Models;

namespace SolarMicrogrid.Api.Services;

public class AuthService
{
    private readonly IMongoCollection<UserDetail> users;
    private readonly PasswordHasher<UserDetail> hasher;
    private readonly IConfiguration config;

    // gets the user collection, the password hasher and the app settings
    public AuthService(IMongoDatabase db, PasswordHasher<UserDetail> hasher, IConfiguration config)
    {
        users = db.GetCollection<UserDetail>("UserDetail");
        this.hasher = hasher;
        this.config = config;
    }

    // only prosumers have a nic, so this stops two prosumers sharing one
    // only backoffice and grid operator have an email, so this stops two of them sharing one
    public async Task EnsureIndexes()
    {
        var nicIndex = new CreateIndexModel<UserDetail>(
            Builders<UserDetail>.IndexKeys.Ascending(u => u.Nic),
            new CreateIndexOptions<UserDetail>
            {
                Name = "nic_unique",
                Unique = true,
                PartialFilterExpression = Builders<UserDetail>.Filter.Exists(u => u.Nic)
            });

        var emailIndex = new CreateIndexModel<UserDetail>(
            Builders<UserDetail>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions<UserDetail>
            {
                Name = "email_unique",
                Unique = true,
                PartialFilterExpression = Builders<UserDetail>.Filter.Exists(u => u.Email)
            });

        await users.Indexes.CreateManyAsync([nicIndex, emailIndex]);
    }

    // checks the identifier and password, then gives back a token if it is right
    public async Task<(int Status, string? Error, string? Token, UserDetail? User)> Login(LoginRequest request)
    {
        var user = await users.Find(u => u.Nic == request.Identifier).FirstOrDefaultAsync();
        user ??= await users.Find(u => u.Email == request.Identifier).FirstOrDefaultAsync();

        if (user == null || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            return (401, "Wrong id or password", null, null);

        var allowed = request.Platform == "web"
            ? user.Role is "Backoffice" or "GridOperator"
            : request.Platform == "mobile" && user.Role is "Prosumer" or "GridOperator";

        if (!allowed)
            return (403, "This account cannot log in here", null, null);

        if (user.Status == "pending")
            return (403, "Your account is waiting for activation", null, null);

        if (user.Status == "deactivated")
            return (403, "Your account is deactivated", null, null);

        return (200, null, MakeToken(user), user);
    }

    // builds the token the user keeps after logging in
    private string MakeToken(UserDetail user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryHours = double.Parse(config["Jwt:ExpiryHours"]!);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
