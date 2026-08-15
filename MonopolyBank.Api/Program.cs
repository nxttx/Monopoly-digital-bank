using System.Text.Json.Serialization;
using MonopolyBank.Api;
using MonopolyBank.Api.Endpoints;
using MonopolyBank.Api.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new HttpStatusCodeJsonConverter());
    o.SerializerOptions.Converters.Add(new HttpMethodJsonConverter());
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddOpenApi();
builder.Services.AddSingleton(_ => new CommandLog(
    builder.Configuration.GetConnectionString("MonopolyBank") ?? "Data Source=monopolybank.db"));
builder.Services.AddSingleton<GameStore>();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();
app.MapGet("/", () => Results.Redirect("/scalar"));

GamesEndpoints.RegisterApiRoutes(app);
GamesUsersEndpoints.RegisterApiRoutes(app);

app.Run();

public partial class Program;
