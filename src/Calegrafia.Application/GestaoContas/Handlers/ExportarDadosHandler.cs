using Calegrafia.Application.GestaoContas.Commands;
using Calegrafia.Domain.Common;
using Calegrafia.Domain.Interfaces;

namespace Calegrafia.Application.GestaoContas.Handlers;

/// <summary>
/// RF-13 — Direito à portabilidade de dados (LGPD Art. 18).
/// Enfileira o job de exportação e retorna imediatamente (202 Accepted).
/// O job gera o JSON e envia por email assincronamente.
/// </summary>
public sealed class ExportarDadosHandler
{
    private readonly IContaRepository _contaRepo;
    private readonly IPerfilRepository _perfilRepo;
    private readonly IEmailService _emailService;

    public ExportarDadosHandler(
        IContaRepository contaRepo,
        IPerfilRepository perfilRepo,
        IEmailService emailService)
    {
        _contaRepo = contaRepo;
        _perfilRepo = perfilRepo;
        _emailService = emailService;
    }

    public async Task<Result> HandleAsync(ExportarDadosCommand command, CancellationToken ct = default)
    {
        var conta = await _contaRepo.ObterPorIdAsync(command.ContaId, ct);
        if (conta is null)
            return Result.Failure("Conta não encontrada.");

        // Disparar exportação em background (fire-and-forget seguro)
        _ = Task.Run(async () => await GerarEEnviarExportacaoAsync(conta, ct), ct);

        // Retornar imediatamente — controller deve mapear para 202 Accepted
        return Result.Success();
    }

    private async Task GerarEEnviarExportacaoAsync(Domain.Entities.Conta conta, CancellationToken ct)
    {
        try
        {
            var perfis = await _perfilRepo.ListarPorContaAsync(conta.Id, ct);

            // Montar JSON de exportação
            var dados = new
            {
                conta = new
                {
                    id = conta.Id,
                    email = conta.Email.Value,
                    status = conta.Status.ToString(),
                    mfaAtivo = conta.MfaAtivo,
                    criadoEm = conta.CriadoEm
                },
                perfis = perfis.Select(p => new
                {
                    id = p.Id,
                    nome = p.Nome,
                    isInfantil = p.IsInfantil,
                    usaLibras = p.UsaLibras,
                    criadoEm = p.CriadoEm
                }),
                exportadoEm = DateTime.UtcNow
            };

            var json = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(dados,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            var nomeArquivo = $"dados-calegrafia-{conta.Id:N}.json";

            await _emailService.EnviarExportacaoDadosAsync(
                conta.Email.Value, conta.Email.Value, json, nomeArquivo, ct);
        }
        catch
        {
            // Falha silenciosa — job deve ser retry em produção via fila (ex: Hangfire)
            // Por ora: log seria registrado aqui
        }
    }
}


