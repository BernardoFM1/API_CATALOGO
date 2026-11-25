using APIVitrine.Context;
using APIVitrine.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "APIVitrine — Aplicacao Secundaria (PDV / Vitrine)",
        Version = "v1",
        Description = "Segunda aplicacao ASP.NET Core com banco SQL proprio, sincronizada via Webhook a partir do APICatalogo."
    });
});

builder.Services.AddSingleton<ChaosSimulationState>();

var databaseProvider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";

builder.Services.AddDbContext<VitrineDbContext>(options =>
{
    switch (databaseProvider.ToLowerInvariant())
    {
        case "sqlserver":
            var sqlServerConn = builder.Configuration.GetConnectionString("SqlServerConnection")
                ?? throw new InvalidOperationException("ConnectionString 'SqlServerConnection' nao configurada.");
            options.UseSqlServer(sqlServerConn);
            break;

        default:
            var sqliteConn = builder.Configuration.GetConnectionString("SqliteConnection")
                ?? "Data Source=vitrine.db";
            options.UseSqlite(sqliteConn);
            break;
    }
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://127.0.0.1:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VitrineDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors("FrontendPolicy");
app.UseAuthorization();

app.MapGet("/health", async (VitrineDbContext db, ChaosSimulationState chaos) =>
{
    var totalProdutos = await db.ProdutosVitrine.CountAsync();
    return Results.Ok(new
    {
        servico = "APIVitrine (Replica / PDV)",
        status = chaos.SimularFalha ? "Degraded" : "Healthy",
        simularFalha = chaos.SimularFalha,
        databaseProvider,
        totalProdutos,
        timestampUtc = DateTime.UtcNow
    });
}).WithTags("Health");

app.MapControllers();

app.Run();
