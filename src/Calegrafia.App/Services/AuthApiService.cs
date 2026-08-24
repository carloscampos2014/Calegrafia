using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Calegrafia.App.Services;

/// <summary>
/// HTTP client for the Calegrafia authentication API.
/// BaseAddress is configured via DI in MauiProgram.
/// </summary>
public sealed class AuthApiService : IAuthApiService
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AuthApiService(HttpClient http)
    {
        _http = http;
    }

    // -------------------------------------------------------------------------
    // Login
    // -------------------------------------------------------------------------

    public async Task<LoginResult?> LoginAsync(
        string email,
        string senha,
        string? codigoMfa,
        CancellationToken ct = default)
    {
        var payload = new LoginRequest(email, senha, codigoMfa);

        try
        {
            using var response = await _http.PostAsJsonAsync(
                "/api/auth/login", payload, _jsonOptions, ct);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                return null; // caller checks for null + maps the error message

            if (!response.IsSuccessStatusCode)
                return null;

            var dto = await response.Content
                .ReadFromJsonAsync<LoginResponse>(_jsonOptions, ct);

            if (dto is null)
                return null;

            return new LoginResult(
                dto.AccessToken,
                dto.RefreshToken,
                dto.ExpiresIn,
                dto.MfaRequired);
        }
        catch (Exception)
        {
            return null;
        }
    }

    // -------------------------------------------------------------------------
    // Cadastro
    // -------------------------------------------------------------------------

    public async Task<bool> CadastrarAsync(
        string email,
        string senha,
        bool aceitouTermos,
        bool aceitouPolitica,
        CancellationToken ct = default)
    {
        var payload = new CadastroRequest(
            email,
            senha,
            aceitouTermos,
            aceitouPolitica,
            VersaoTermos: "1.0");

        try
        {
            using var response = await _http.PostAsJsonAsync(
                "/api/auth/cadastro", payload, _jsonOptions, ct);

            return response.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // -------------------------------------------------------------------------
    // Recuperar senha
    // -------------------------------------------------------------------------

    public async Task<bool> RecuperarSenhaAsync(string email, CancellationToken ct = default)
    {
        var payload = new RecuperarSenhaRequest(email);

        try
        {
            using var response = await _http.PostAsJsonAsync(
                "/api/auth/recuperar-senha", payload, _jsonOptions, ct);

            // Always return true — never reveal whether the email exists.
            return true;
        }
        catch (Exception)
        {
            return true; // same — security: no enumeration
        }
    }

    // -------------------------------------------------------------------------
    // Reset MFA
    // -------------------------------------------------------------------------

    public async Task<bool> ResetMfaSolicitarAsync(string email, CancellationToken ct = default)
    {
        var payload = new ResetMfaRequest(email);

        try
        {
            using var response = await _http.PostAsJsonAsync(
                "/api/auth/mfa/reset-solicitar", payload, _jsonOptions, ct);

            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // -------------------------------------------------------------------------
    // Private DTOs (request / response)
    // -------------------------------------------------------------------------

    private sealed record LoginRequest(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("senha")] string Senha,
        [property: JsonPropertyName("codigoMfa")] string? CodigoMfa);

    private sealed record LoginResponse(
        [property: JsonPropertyName("accessToken")] string? AccessToken,
        [property: JsonPropertyName("refreshToken")] string? RefreshToken,
        [property: JsonPropertyName("expiresIn")] int ExpiresIn,
        [property: JsonPropertyName("mfaRequired")] bool MfaRequired);

    private sealed record CadastroRequest(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("senha")] string Senha,
        [property: JsonPropertyName("aceitouTermos")] bool AceitouTermos,
        [property: JsonPropertyName("aceitouPoliticaPrivacidade")] bool AceitouPoliticaPrivacidade,
        [property: JsonPropertyName("versaoTermos")] string VersaoTermos);

    private sealed record RecuperarSenhaRequest(
        [property: JsonPropertyName("email")] string Email);

    private sealed record ResetMfaRequest(
        [property: JsonPropertyName("email")] string Email);
}
