using DbUp;
using Scalar.AspNetCore;
using Serilog;

// ── Serilog bootstrap logger (captura erros no startup) ─────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog (arquivo + console, substituindo o logging padrão) ───────────
    builder.Host.UseSerilog((ctx, services, config) =>
    {
        config
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                path: "logs/calegrafia-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14);
    });

    // ── Serviços ─────────────────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddOpenApi(); // OpenAPI nativo do .NET 10 (necessário para Scalar)

    var app = builder.Build();

    // ── DbUp: executa migrations no startup ──────────────────────────────────
    var connectionString = app.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("Connection string 'Postgres' não encontrada.");

    var upgrader = DeployChanges.To
        .PostgresqlDatabase(connectionString)
        .WithScriptsEmbeddedInAssembly(
            typeof(Calegrafia.Infrastructure.Migrations.MigrationsMarker).Assembly)
        .WithTransaction()
        .LogToAutodetectedLog()
        .Build();

    var migrationResult = upgrader.PerformUpgrade();
    if (!migrationResult.Successful)
    {
        Log.Fatal(migrationResult.Error, "Falha ao executar migrations do banco de dados");
        throw migrationResult.Error;
    }

    // ── Pipeline HTTP ────────────────────────────────────────────────────────
    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging();

    // Scalar acessível em /scalar/{documentName}
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar/{documentName}", options =>
    {
        options.Title = "Calegrafia API";
    });

    app.UseAuthorization();
    app.MapControllers();

    Log.Information("Calegrafia API iniciada");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "A aplicação falhou no startup");
    Environment.Exit(1);
}
finally
{
    Log.CloseAndFlush();
}
