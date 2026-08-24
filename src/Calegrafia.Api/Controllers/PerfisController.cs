using Calegrafia.Application.Perfis.Commands;
using Calegrafia.Application.Perfis.Handlers;
using Calegrafia.Application.Perfis.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Calegrafia.Api.Controllers;

/// <summary>Gerenciamento de perfis da conta.</summary>
[ApiController]
[Route("api/perfis")]
[Authorize]
[Produces("application/json")]
public sealed class PerfisController : ControllerBase
{
    private Guid ContaId =>
        Guid.Parse(User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Lista todos os perfis da conta autenticada.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PerfilResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromServices] ListarPerfisHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ListarPerfisQuery(ContaId), ct);
        return Ok(result.Value);
    }

    /// <summary>Cria um novo perfil. Máximo de 5 por conta.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PerfilResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar(
        [FromBody] CriarPerfilRequest request,
        [FromServices] CriarPerfilHandler handler,
        CancellationToken ct)
    {
        var command = new CriarPerfilCommand(
            ContaId, request.Nome, request.IsInfantil, request.UsaLibras,
            request.ConsentimentoParentalAceito, request.VersaoTermos,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var result = await handler.HandleAsync(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Listar), result.Value)
            : BadRequest(new { error = result.Error });
    }

    /// <summary>Edita um perfil existente da conta.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PerfilResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Editar(
        Guid id,
        [FromBody] EditarPerfilRequest request,
        [FromServices] EditarPerfilHandler handler,
        CancellationToken ct)
    {
        var command = new EditarPerfilCommand(
            id, ContaId, request.Nome, request.IsInfantil, request.UsaLibras, request.AvatarUrl);

        var result = await handler.HandleAsync(command, ct);
        if (result.IsFailure)
            return result.Error.Contains("permissão") ? Forbid() : BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>Exclui um perfil da conta (operação irreversível).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(
        Guid id,
        [FromServices] ExcluirPerfilHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ExcluirPerfilCommand(id, ContaId), ct);
        if (result.IsFailure)
            return result.Error.Contains("permissão") ? Forbid() : NotFound(new { error = result.Error });

        return NoContent();
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public sealed record CriarPerfilRequest(
    string Nome,
    bool IsInfantil = false,
    bool UsaLibras = false,
    bool ConsentimentoParentalAceito = false,
    string VersaoTermos = "1.0");

public sealed record EditarPerfilRequest(
    string Nome,
    bool IsInfantil,
    bool UsaLibras,
    string? AvatarUrl = null);
