// File: Program.cs
// Purpose: creates the web app, registers services and starts listening
// Author: H.M.T.S.M.Dissanayake

using Microsoft.AspNetCore.Identity;
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

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// makes the unique indexes and the first backoffice user before taking requests
using (var scope = app.Services.CreateScope())
{
    var auth = scope.ServiceProvider.GetRequiredService<AuthService>();
    await auth.EnsureIndexes();
    await auth.SeedBackofficeUser(
        app.Configuration["Seed:BackofficeEmail"]!,
        app.Configuration["Seed:BackofficeName"]!,
        app.Configuration["Seed:BackofficePassword"]!);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
