namespace Calegrafia.Domain.Interfaces;

/// <summary>
/// Abstração para validar tokens de provedores de social login.
/// Cada provedor (Google, Apple) tem sua implementação na Infrastructure.
/// </summary>
public interface ISocialLoginProvider
{
    string Provedor { get; } // "google" | "apple"

    /// <summary>
    /// Valida o token do provedor e retorna os dados do usuário autenticado.
    /// Retorna null se o token for inválido.
    /// </summary>
    Task<SocialUserInfo?> ValidarTokenAsync(string token, CancellationToken ct = default);
}
