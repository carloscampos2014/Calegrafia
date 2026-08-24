using Calegrafia.Application.GestaoContas.Commands;
using Calegrafia.Application.GestaoContas.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Calegrafia.Api.Controllers;

/// <summary>Gestão da conta — exportação e exclusão de dados (LGPD Art. 18).</summary>
[ApiController]
[Route("api/conta")]
[Authorize]
[Produces("application/json")]
public sealed class ContaController : ControllerBase
{
    private Guid ContaId =>
        Guid.Parse(User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Solicita exportação dos dados (portabilidade LGPD). Processamento assíncrono — envia JSON por email em até 72h.
    /// </summary>
    [HttpPost("exportar-dados")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ExportarDados(
        [FromServices] ExportarDadosHandler handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new ExportarDadosCommand(ContaId), ct);
        return Accepted(new { message = "Exportação enfileirada. Você receberá os dados por email." });
    }

    /// <summary>
    /// Exclui a conta e todos os dados associados (direito ao esquecimento LGPD). Operação irreversível.
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExcluirConta(
        [FromBody] ExcluirContaRequest request,
        [FromServices] ExcluirContaHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(
            new ExcluirContaCommand(ContaId, request.SenhaAtual), ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}

public sealed record ExcluirContaRequest(string SenhaAtual);
