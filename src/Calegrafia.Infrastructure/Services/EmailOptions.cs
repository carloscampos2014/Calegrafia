namespace Calegrafia.Infrastructure.Services;

/// <summary>
/// Configurações de SMTP para o EmailService.
/// Carregar via appsettings.json — nunca hardcoded.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; init; } = string.Empty;
    public int Porta { get; init; } = 587;
    public string Usuario { get; init; } = string.Empty;
    public string Senha { get; init; } = string.Empty;
    public string EmailRemetente { get; init; } = string.Empty;
    public string NomeRemetente { get; init; } = "Calegrafia";
    public bool UsarSsl { get; init; } = false;
}
