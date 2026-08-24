using System.Text;
using Calegrafia.Application.Auth.Handlers;
using Calegrafia.Application.GestaoContas.Handlers;
using Calegrafia.Application.Perfis.Handlers;
using Calegrafia.Infrastructure.Repositories;
using Calegrafia.Infrastructure.Services;
using DbUp;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using System.Threading.RateLimiting;

// ── Serilog bootstrap logger ─────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ───────────────────────────────────────────────────────────────
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

    // ── Controllers + OpenAPI ─────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    // ── JWT Authentication ────────────────────────────────────────────────────
    var jwtConfig = builder.Configuration.GetSection("Jwt");
    var publicKeyPem = jwtConfig["PublicKeyPem"]!;

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var rsa = System.Security.Cryptography.RSA.Create();
            rsa.ImportFromPem(publicKeyPem);
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtConfig["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtConfig["Audience"],
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new RsaSecurityKey(rsa),
                ClockSkew = TimeSpan.Zero,
                NameClaimType = "sub"
            };
        });

    builder.Services.AddAuthorization();

    // ── Rate Limiting (login: 10 req/min por IP) ──────────────────────────────
    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("login", limiter =>
        {
            limiter.PermitLimit = 10;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiter.QueueLimit = 0;
        });
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    // ── Configuração do banco ─────────────────────────────────────────────────
    var connectionString = builder.Configuration.GetConnectionString("Postgres")!;

    // ── Repositórios (Infrastructure) ────────────────────────────────────────
    builder.Services.AddScoped(_ => new ContaRepository(connectionString));
    builder.Services.AddScoped(_ => new PerfilRepository(connectionString));
    builder.Services.AddScoped(_ => new RefreshTokenRepository(connectionString));
    builder.Services.AddScoped(_ => new TokenConfirmacaoRepository(connectionString));
    builder.Services.AddScoped(_ => new ConsentimentoRepository(connectionString));
    builder.Services.AddScoped(_ => new LogAutenticacaoRepository(connectionString));
    builder.Services.AddScoped(_ => new ProvedorSocialRepository(connectionString));

    // ── Interfaces dos repositórios ───────────────────────────────────────────
    builder.Services.AddScoped<Calegrafia.Domain.Interfaces.IContaRepository>(
        sp => sp.GetRequiredService<ContaRepository>());
    builder.Services.AddScoped<Calegrafia.Domain.Interfaces.IPerfilRepository>(
        sp => sp.GetRequiredService<PerfilRepository>());
    builder.Services.AddScoped<Calegrafia.Domain.Interfaces.IRefreshTokenRepository>(
        sp => sp.GetRequiredService<RefreshTokenRepository>());
    builder.Services.AddScoped<Calegrafia.Domain.Interfaces.ITokenConfirmacaoRepository>(
        sp => sp.GetRequiredService<TokenConfirmacaoRepository>());
    builder.Services.AddScoped<Calegrafia.Domain.Interfaces.IConsentimentoRepository>(
        sp => sp.GetRequiredService<ConsentimentoRepository>());
    builder.Services.AddScoped<Calegrafia.Domain.Interfaces.ILogAutenticacaoRepository>(
        sp => sp.GetRequiredService<LogAutenticacaoRepository>());
    builder.Services.AddScoped<Calegrafia.Domain.Interfaces.IProvedorSocialRepository>(
        sp => sp.GetRequiredService<ProvedorSocialRepository>());

    // ── Serviços de infraestrutura ────────────────────────────────────────────
    var totpKey = builder.Configuration["Totp:EncryptionKeyBase64"]!;
    builder.Services.AddSingleton<Calegrafia.Domain.Interfaces.ITotpService>(
        _ => new TotpService(totpKey));

    builder.Services.AddSingleton<Calegrafia.Domain.Interfaces.IJwtService>(_ =>
        new JwtService(
            jwtConfig["PrivateKeyPem"]!,
            jwtConfig["PublicKeyPem"]!,
            jwtConfig["Issuer"]!,
            jwtConfig["Audience"]!));

    builder.Services.AddScoped<Calegrafia.Domain.Interfaces.IPasswordHasher, BcryptPasswordHasher>();

    builder.Services.AddScoped<Calegrafia.Domain.Interfaces.IEmailService>(_ =>
        new EmailService(new EmailOptions
        {
            Host = builder.Configuration["Email:Host"]!,
            Porta = int.Parse(builder.Configuration["Email:Porta"] ?? "587"),
            Usuario = builder.Configuration["Email:Usuario"] ?? "",
            Senha = builder.Configuration["Email:Senha"] ?? "",
            EmailRemetente = builder.Configuration["Email:EmailRemetente"]!,
            NomeRemetente = builder.Configuration["Email:NomeRemetente"] ?? "Calegrafia",
            UsarSsl = bool.Parse(builder.Configuration["Email:UsarSsl"] ?? "false")
        }));

    // Social login providers (vazios por ora — implementações na fase de providers)
    builder.Services.AddScoped<IEnumerable<Calegrafia.Domain.Interfaces.ISocialLoginProvider>>(
        _ => Array.Empty<Calegrafia.Domain.Interfaces.ISocialLoginProvider>());

    // ── Application Handlers — Auth ───────────────────────────────────────────
    var baseUrl = builder.Configuration["App:BaseUrl"]!;

    builder.Services.AddScoped(sp => new CadastrarContaHandler(
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IContaRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.ITokenConfirmacaoRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IConsentimentoRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IEmailService>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IPasswordHasher>(),
        baseUrl));

    builder.Services.AddScoped(sp => new ConfirmarEmailHandler(
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IContaRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.ITokenConfirmacaoRepository>()));

    builder.Services.AddScoped(sp => new LoginHandler(
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IContaRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IRefreshTokenRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.ILogAutenticacaoRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IJwtService>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IPasswordHasher>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.ITotpService>()));

    builder.Services.AddScoped(sp => new RefreshTokenHandler(
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IContaRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IRefreshTokenRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IJwtService>()));

    builder.Services.AddScoped(sp => new LogoutHandler(
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IRefreshTokenRepository>()));

    builder.Services.AddScoped(sp => new LogoutTodosHandler(
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IRefreshTokenRepository>()));

    builder.Services.AddScoped(sp => new RecuperarSenhaHandler(
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IContaRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.ITokenConfirmacaoRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IEmailService>(),
        baseUrl));

    builder.Services.AddScoped(sp => new RedefinirSenhaHandler(
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IContaRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.ITokenConfirmacaoRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IRefreshTokenRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IPasswordHasher>()));

    builder.Services.AddScoped(sp => new LoginSocialHandler(
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IContaRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IRefreshTokenRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IProvedorSocialRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IJwtService>(),
        sp.GetRequiredService<IEnumerable<Calegrafia.Domain.Interfaces.ISocialLoginProvider>>()));

    builder.Services.AddScoped(sp => new AtivarMfaHandler(
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IContaRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.ITotpService>()));

    builder.Services.AddScoped(sp => new DesativarMfaHandler(
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IContaRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.ITotpService>()));

    builder.Services.AddScoped(sp => new ResetMfaSolicitarHandler(
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IContaRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.ITokenConfirmacaoRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IEmailService>(),
        baseUrl));

    builder.Services.AddScoped(sp => new ResetMfaConfirmarHandler(
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IContaRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.ITokenConfirmacaoRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IRefreshTokenRepository>()));

    // ── Application Handlers — Perfis ─────────────────────────────────────────
    builder.Services.AddScoped(sp => new CriarPerfilHandler(
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IPerfilRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IConsentimentoRepository>()));

    builder.Services.AddScoped(sp => new ListarPerfisHandler(
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IPerfilRepository>()));

    builder.Services.AddScoped(sp => new EditarPerfilHandler(
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IPerfilRepository>()));

    builder.Services.AddScoped(sp => new ExcluirPerfilHandler(
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IPerfilRepository>()));

    // ── Application Handlers — Conta/LGPD ────────────────────────────────────
    builder.Services.AddScoped(sp => new ExportarDadosHandler(
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IContaRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IPerfilRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IEmailService>()));

    builder.Services.AddScoped(sp => new ExcluirContaHandler(
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IContaRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IPerfilRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IRefreshTokenRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.ILogAutenticacaoRepository>(),
        sp.GetRequiredService<Calegrafia.Domain.Interfaces.IPasswordHasher>()));

    // ── Build ─────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── DbUp migrations ───────────────────────────────────────────────────────
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
        Log.Fatal(migrationResult.Error, "Falha nas migrations");
        throw migrationResult.Error;
    }

    // ── Pipeline HTTP ─────────────────────────────────────────────────────────
    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    // Scalar em /scalar/{documentName}
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar/{documentName}", options =>
    {
        options.Title = "Calegrafia API";
    });

    // Rate limiting apenas no endpoint de login
    app.MapControllers();
    app.MapPost("/api/auth/login", () => { }).RequireRateLimiting("login");

    Log.Information("Calegrafia API iniciada");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "A API encerrou de forma inesperada");
}
finally
{
    Log.CloseAndFlush();
}
