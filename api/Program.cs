// File: Program.cs
// Purpose: creates the web app, registers services and starts listening
// Author: H.M.T.S.M.Dissanayake

using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// one mongo client for the whole app, reused everywhere
builder.Services.AddSingleton<IMongoClient>(
    new MongoClient(builder.Configuration["Mongo:ConnectionString"]));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase(builder.Configuration["Mongo:Database"]));

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
