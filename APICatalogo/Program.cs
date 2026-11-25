using APICatalogo.Context;
using APICatalogo.Extensions;
using APICatalogo.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "APICatalogo — Catalogo Master",
        Version = "v1",
        Description = "API principal de cadastro e consulta de produtos/categorias com sincronizacao via Webhook para a APIVitrine."
    });
});

// Servico de estudo preservado como referencia
builder.Services.AddTransient<IMeuServico, MeuServico>();

// Cliente HTTP e servico de sincronizacao por Webhook
builder.Services.AddHttpClient("VitrineWebhook");
builder.Services.AddScoped<IWebhookSyncService, WebhookSyncService>();

// Provedor de banco configuravel via appsettings.json (Sqlite padrao | SqlServer | MySql)
var databaseProvider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    switch (databaseProvider.ToLowerInvariant())
    {
        case "sqlserver":
            var sqlServerConn = builder.Configuration.GetConnectionString("SqlServerConnection")
                ?? throw new InvalidOperationException("ConnectionString 'SqlServerConnection' nao configurada.");
            options.UseSqlServer(sqlServerConn);
            break;

        case "mysql":
            var mySqlConn = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' nao configurada.");
            options.UseMySql(mySqlConn, ServerVersion.AutoDetect(mySqlConn));
            break;

        default:
            var sqliteConn = builder.Configuration.GetConnectionString("SqliteConnection")
                ?? "Data Source=catalogo.db";
            options.UseSqlite(sqliteConn);
            break;
    }
});

// CORS para permitir chamadas do Frontend Vue + Bootstrap
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

// Inicializacao automatica do banco SQLite/SQL Server com seed de dados
// (As migrations antigas na pasta Migrations/ foram mantidas como historico de estudo MySQL)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DbInitializer.Initialize(db);
}

app.ConfigureExceptionHandler(app.Environment);

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

app.MapGet("/health", async (AppDbContext db) =>
{
    var totalProdutos = await db.Produtos.CountAsync();
    var totalCategorias = await db.Categorias.CountAsync();

    return Results.Ok(new
    {
        servico = "APICatalogo (Master)",
        status = "Healthy",
        databaseProvider,
        totalProdutos,
        totalCategorias,
        timestampUtc = DateTime.UtcNow
    });
}).WithTags("Health");

app.MapControllers();

app.Run();
