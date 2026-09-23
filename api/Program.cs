// File: Program.cs
// Purpose: creates the web app, registers services and starts listening
// Author: H.M.T.S.M.Dissanayake

using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using SolarMicrogrid.Api.Models;
using SolarMicrogrid.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// one mongo client for the whole app, reused everywhere
builder.Services.AddSingleton<IMongoClient>(
    new MongoClient(builder.Configuration["Mongo:ConnectionString"]));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase(builder.Configuration["Mongo:Database"]));

builder.Services.AddSingleton<PasswordHasher<UserDetail>>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<StationService>();
builder.Services.AddScoped<SlotService>();

// checks the token on every request, and that the account is still active
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateLifetime = true
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var db = context.HttpContext.RequestServices.GetRequiredService<IMongoDatabase>();
                var users = db.GetCollection<UserDetail>("UserDetail");
                var user = await users.Find(u => u.Id == id).FirstOrDefaultAsync();

                if (user == null || user.Status != "active")
                    context.Fail("account not active");
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// makes the unique indexes and the first backoffice user before taking requests
using (var scope = app.Services.CreateScope())
{
    var auth = scope.ServiceProvider.GetRequiredService<AuthService>();
    await auth.EnsureIndexes();

    var db = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
    var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher<UserDetail>>();
    var users = db.GetCollection<UserDetail>("UserDetail");

    var backofficeExists = await users.Find(u => u.Role == "Backoffice").AnyAsync();
    if (!backofficeExists)
    {
        var admin = new UserDetail
        {
            Id = Guid.NewGuid().ToString(),
            Email = app.Configuration["Seed:BackofficeEmail"],
            Role = "Backoffice",
            FullName = app.Configuration["Seed:BackofficeName"] ?? "",
            Status = "active"
        };
        admin.PasswordHash = hasher.HashPassword(admin, app.Configuration["Seed:BackofficePassword"]!);
        await users.InsertOneAsync(admin);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
