using Calegrafia.Application.Auth.Commands;
using Calegrafia.Domain.Common;
using Calegrafia.Domain.Interfaces;
using Calegrafia.Domain.ValueObjects;

namespace Calegrafia.Application.Auth.Handlers;

public sealed class RecuperarSenhaHandler
{
    private readonly IContaRepository _contaRepo;
    private readonly ITokenConfirmacaoRepository _tokenRepo;
    private readonly IEmailService _emailService;
    private readonly string _baseUrl;

    public RecuperarSenhaHandler(
        IContaRepository contaRepo,
        ITokenConfirmacaoRepository tokenRepo,
        IEmailService emailService,
        string baseUrl)
    {
        _contaRepo = contaRepo;
        _tokenRepo = tokenRepo;
        _emailService = emailService;
        _baseUrl = baseUrl;
    }

    public async Task<Result> HandleAsync(RecuperarSenhaCommand command, CancellationToken ct = default)
    {
        var emailResult = Email.Create(command.Email);

        // Retornar 200 mesmo se email inválido ou não encontrado — não revela existência (RF-10)
        if (emailResult.IsFailure)
            return Result.Success();

        var email = emailResult.Value!;
        var conta = await _contaRepo.ObterPorEmailAsync(email, ct);

        if (conta is null)
            return Result.Success(); // Não revelar que email não existe

        var token = GerarToken();
        await _tokenRepo.CriarAsync(
            conta.Id, "redefinicao_senha", token,
            DateTime.UtcNow.AddMinutes(10), ct);

        var link = $"{_baseUrl}/redefinir-senha?token={token}";

        try
        {
            await _emailService.EnviarRedefinicaoSenhaAsync(
                email.Value, email.Value, link, ct);
        }
        catch
        {
            // Falha no envio não deve revelar informação ao usuário
        }

        return Result.Success();
    }

    private static string GerarToken() =>
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
}
