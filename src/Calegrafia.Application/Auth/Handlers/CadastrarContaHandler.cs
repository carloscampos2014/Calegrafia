using Calegrafia.Application.Auth.Commands;
using Calegrafia.Domain.Common;
using Calegrafia.Domain.Entities;
using Calegrafia.Domain.Interfaces;
using Calegrafia.Domain.ValueObjects;

namespace Calegrafia.Application.Auth.Handlers;

public sealed class CadastrarContaHandler
{
    private readonly IContaRepository _contaRepo;
    private readonly ITokenConfirmacaoRepository _tokenRepo;
    private readonly IConsentimentoRepository _consentimentoRepo;
    private readonly IEmailService _emailService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly string _baseUrl;

    public CadastrarContaHandler(
        IContaRepository contaRepo,
        ITokenConfirmacaoRepository tokenRepo,
        IConsentimentoRepository consentimentoRepo,
        IEmailService emailService,
        IPasswordHasher passwordHasher,
        string baseUrl)
    {
        _contaRepo = contaRepo;
        _tokenRepo = tokenRepo;
        _consentimentoRepo = consentimentoRepo;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
        _baseUrl = baseUrl;
    }

    public async Task<Result<CadastrarContaResult>> HandleAsync(
        CadastrarContaCommand command, CancellationToken ct = default)
    {
        // Validar aceite de termos (RF-12)
        if (!command.AceitouTermos)
            return Result<CadastrarContaResult>.Failure("É obrigatório aceitar os Termos de Uso.");

        if (!command.AceitouPoliticaPrivacidade)
            return Result<CadastrarContaResult>.Failure("É obrigatório aceitar a Política de Privacidade.");

        // Validar email
        var emailResult = Email.Create(command.Email);
        if (emailResult.IsFailure)
            return Result<CadastrarContaResult>.Failure(emailResult.Error);

        var email = emailResult.Value!;

        // Verificar email duplicado
        if (await _contaRepo.ExisteEmailAsync(email, ct))
            return Result<CadastrarContaResult>.Failure("Este email já está cadastrado.");

        // Validar senha
        var senhaValida = ValidarSenha(command.Senha);
        if (senhaValida.IsFailure)
            return Result<CadastrarContaResult>.Failure(senhaValida.Error);

        // Criar conta
        var senhaHash = _passwordHasher.Hash(command.Senha);
        var contaResult = Conta.Criar(email, senhaHash);
        if (contaResult.IsFailure)
            return Result<CadastrarContaResult>.Failure(contaResult.Error);

        var conta = contaResult.Value!;
        var contaId = await _contaRepo.CriarAsync(conta, ct);

        // Registrar consentimentos (LGPD — RF-12)
        await _consentimentoRepo.RegistrarAsync(
            contaId, "termos_uso", command.VersaoTermos, aceito: true,
            command.IpOrigem, command.UserAgent, ct);

        await _consentimentoRepo.RegistrarAsync(
            contaId, "politica_privacidade", command.VersaoTermos, aceito: true,
            command.IpOrigem, command.UserAgent, ct);

        // Gerar token de confirmação (24h)
        var token = GerarToken();
        await _tokenRepo.CriarAsync(contaId, "confirmacao_email", token,
            DateTime.UtcNow.AddHours(24), ct);

        // Enviar email de confirmação
        var linkConfirmacao = $"{_baseUrl}/confirmar-email?token={token}";
        await _emailService.EnviarConfirmacaoCadastroAsync(
            email.Value, email.Value, linkConfirmacao, ct);

        return Result<CadastrarContaResult>.Success(new CadastrarContaResult(contaId, email.Value));
    }

    private static Result ValidarSenha(string senha)
    {
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < 8)
            return Result.Failure("A senha deve ter no mínimo 8 caracteres.");

        if (!senha.Any(char.IsUpper))
            return Result.Failure("A senha deve conter pelo menos uma letra maiúscula.");

        if (!senha.Any(char.IsLower))
            return Result.Failure("A senha deve conter pelo menos uma letra minúscula.");

        if (!senha.Any(char.IsDigit))
            return Result.Failure("A senha deve conter pelo menos um número.");

        return Result.Success();
    }

    private static string GerarToken() =>
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
}
