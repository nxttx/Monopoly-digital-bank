using MonopolyBank.Api;
using MonopolyBank.Api.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(_ => new CommandLog(
    builder.Configuration.GetConnectionString("MonopolyBank") ?? "Data Source=monopolybank.db"));
builder.Services.AddSingleton<GameStore>();

var app = builder.Build();

// Route registrations go here, one per user-tested handler.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })); // temporary scaffold check

app.Run();

public partial class Program;
