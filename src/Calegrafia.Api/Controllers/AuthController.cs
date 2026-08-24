using Calegrafia.Application.Auth.Commands;
using Calegrafia.Application.Auth.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Calegrafia.Api.Controllers;

/// <summary>Autenticação — cadastro, login, tokens, MFA e recuperação de senha.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    // ── Cadastro ──────────────────────────────────────────────────────────────

    /// <summary>Cria uma nova conta. Envia email de confirmação.</summary>
    [HttpPost("cadastro")]
    [ProducesResponseType(typeof(CadastrarContaResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cadastro(
        [FromBody] CadastrarContaRequest request,
        [FromServices] CadastrarContaHandler handler,
        CancellationToken ct)
    {
        var command = new CadastrarContaCommand(
            request.Email, request.Senha,
            request.AceitouTermos, request.AceitouPoliticaPrivacidade,
            request.VersaoTermos,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var result = await handler.HandleAsync(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Cadastro), result.Value)
            : BadRequest(new { error = result.Error });
    }

    /// <summary>Confirma o email com o token recebido por email.</summary>
    [HttpPost("confirmar-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmarEmail(
        [FromBody] TokenRequest request,
        [FromServices] ConfirmarEmailHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ConfirmarEmailCommand(request.Token), ct);
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    /// <summary>Autentica com email e senha. Retorna JWT + refresh token ou solicita MFA.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] LoginHandler handler,
        CancellationToken ct)
    {
        var command = new LoginCommand(
            request.Email, request.Senha, request.CodigoMfa,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            request.Dispositivo);

        var result = await handler.HandleAsync(command, ct);
        return result.IsSuccess ? Ok(result.Value) : Unauthorized(new { error = result.Error });
    }

    /// <summary>Renova o access token usando o refresh token.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshTokenResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] TokenRequest request,
        [FromServices] RefreshTokenHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new RefreshTokenCommand(request.Token), ct);
        return result.IsSuccess ? Ok(result.Value) : Unauthorized(new { error = result.Error });
    }

    /// <summary>Revoga o refresh token fornecido (logout do dispositivo atual).</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(
        [FromBody] TokenRequest request,
        [FromServices] LogoutHandler handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new LogoutCommand(request.Token), ct);
        return Ok();
    }

    /// <summary>Revoga todos os refresh tokens da conta (logout de todos os dispositivos).</summary>
    [HttpPost("logout-todos")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> LogoutTodos(
        [FromServices] LogoutTodosHandler handler,
        CancellationToken ct)
    {
        var contaId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")!);
        await handler.HandleAsync(new LogoutTodosCommand(contaId), ct);
        return Ok();
    }

    // ── Recuperação de senha ──────────────────────────────────────────────────

    /// <summary>Solicita link de redefinição de senha (expira em 10 min). Sempre retorna 200.</summary>
    [HttpPost("recuperar-senha")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RecuperarSenha(
        [FromBody] EmailRequest request,
        [FromServices] RecuperarSenhaHandler handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new RecuperarSenhaCommand(
            request.Email,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString()), ct);
        return Ok();
    }

    /// <summary>Redefine a senha usando o token recebido por email.</summary>
    [HttpPost("redefinir-senha")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RedefinirSenha(
        [FromBody] RedefinirSenhaRequest request,
        [FromServices] RedefinirSenhaHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(
            new RedefinirSenhaCommand(request.Token, request.NovaSenha), ct);
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    // ── Social login ──────────────────────────────────────────────────────────

    /// <summary>Autentica ou cria conta via provedor social (google | apple).</summary>
    [HttpPost("social/{provedor}")]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SocialLogin(
        string provedor,
        [FromBody] SocialLoginRequest request,
        [FromServices] LoginSocialHandler handler,
        CancellationToken ct)
    {
        var command = new LoginSocialCommand(
            provedor, request.Token, request.Dispositivo,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var result = await handler.HandleAsync(command, ct);
        if (result.IsFailure)
            return provedor is "google" or "apple"
                ? Unauthorized(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    // ── MFA ───────────────────────────────────────────────────────────────────

    /// <summary>Inicia a ativação do MFA. Retorna QR code e secret.</summary>
    [HttpGet("mfa/configurar")]
    [Authorize]
    [ProducesResponseType(typeof(AtivarMfaResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> MfaConfigurar(
        [FromServices] AtivarMfaHandler handler,
        CancellationToken ct)
    {
        var contaId = ObterContaId();
        var result = await handler.HandleAsync(new AtivarMfaCommand(contaId), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>Confirma a ativação do MFA com código TOTP.</summary>
    [HttpPost("mfa/ativar")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MfaAtivar(
        [FromBody] MfaConfirmarRequest request,
        [FromServices] AtivarMfaHandler handler,
        CancellationToken ct)
    {
        var contaId = ObterContaId();
        var result = await handler.ConfirmarAsync(
            new AtivarMfaConfirmarCommand(contaId, request.Secret, request.Codigo), ct);
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    /// <summary>Desativa o MFA verificando o código TOTP atual.</summary>
    [HttpPost("mfa/desativar")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MfaDesativar(
        [FromBody] CodigoTotpRequest request,
        [FromServices] DesativarMfaHandler handler,
        CancellationToken ct)
    {
        var contaId = ObterContaId();
        var result = await handler.HandleAsync(
            new DesativarMfaCommand(contaId, request.Codigo), ct);
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    /// <summary>Solicita reset do MFA por email (expira em 10 min). Sempre retorna 200.</summary>
    [HttpPost("mfa/reset-solicitar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MfaResetSolicitar(
        [FromBody] EmailRequest request,
        [FromServices] ResetMfaSolicitarHandler handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new ResetMfaSolicitarCommand(request.Email), ct);
        return Ok();
    }

    /// <summary>Confirma o reset do MFA com o token recebido por email.</summary>
    [HttpPost("mfa/reset-confirmar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MfaResetConfirmar(
        [FromBody] TokenRequest request,
        [FromServices] ResetMfaConfirmarHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(
            new ResetMfaConfirmarCommand(request.Token), ct);
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid ObterContaId() =>
        Guid.Parse(User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public sealed record CadastrarContaRequest(
    string Email,
    string Senha,
    bool AceitouTermos,
    bool AceitouPoliticaPrivacidade,
    string VersaoTermos = "1.0");

public sealed record LoginRequest(
    string Email,
    string Senha,
    string? CodigoMfa = null,
    string? Dispositivo = null);

public sealed record TokenRequest(string Token);
public sealed record EmailRequest(string Email);
public sealed record SocialLoginRequest(string Token, string? Dispositivo = null);
public sealed record RedefinirSenhaRequest(string Token, string NovaSenha);
public sealed record MfaConfirmarRequest(string Secret, string Codigo);
public sealed record CodigoTotpRequest(string Codigo);
