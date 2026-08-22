using Calegrafia.Application.Auth.Commands;
using Calegrafia.Domain.Common;
using Calegrafia.Domain.Entities;
using Calegrafia.Domain.Interfaces;
using Calegrafia.Domain.ValueObjects;

namespace Calegrafia.Application.Auth.Handlers;

public sealed class LoginHandler
{
    private readonly IContaRepository _contaRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly ILogAutenticacaoRepository _logRepo;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITotpService _totpService;

    public LoginHandler(
        IContaRepository contaRepo,
        IRefreshTokenRepository refreshTokenRepo,
        ILogAutenticacaoRepository logRepo,
        IJwtService jwtService,
        IPasswordHasher passwordHasher,
        ITotpService totpService)
    {
        _contaRepo = contaRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _logRepo = logRepo;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _totpService = totpService;
    }

    public async Task<Result<LoginResult>> HandleAsync(LoginCommand command, CancellationToken ct = default)
    {
        var emailResult = Email.Create(command.Email);
        if (emailResult.IsFailure)
            return await FalharLoginAsync(null, command, "login_falha", ct,
                "Credenciais inválidas.");

        var email = emailResult.Value!;
        var emailHash = HashEmail(command.Email);

        var conta = await _contaRepo.ObterPorEmailAsync(email, ct);

        // Conta não encontrada — retornar mensagem genérica (não revelar se email existe)
        if (conta is null)
            return await FalharLoginAsync(null, command, "login_falha", ct,
                "Email ou senha incorretos.");

        // Conta não confirmada
        if (conta.Status == StatusConta.Pendente)
            return await FalharLoginAsync(conta.Id, command, "login_falha", ct,
                "Conta não confirmada. Verifique seu email.");

        // Conta bloqueada
        if (conta.EstaBloqueada())
            return await FalharLoginAsync(conta.Id, command, "bloqueio", ct,
                $"Conta bloqueada. Tente novamente mais tarde.");

        // Verificar senha
        if (conta.SenhaHash is null || !_passwordHasher.Verify(command.Senha, conta.SenhaHash))
        {
            var bloqueioResult = conta.RegistrarTentativaFalha();
            await _contaRepo.AtualizarAsync(conta, ct);
            await RegistrarLogAsync(conta.Id, emailHash, "login_falha", command, ct);

            return Result<LoginResult>.Failure(
                bloqueioResult.IsFailure
                    ? bloqueioResult.Error
                    : "Email ou senha incorretos.");
        }

        // Verificar MFA
        if (conta.MfaAtivo)
        {
            if (string.IsNullOrWhiteSpace(command.CodigoMfa))
            {
                await RegistrarLogAsync(conta.Id, emailHash, "login_falha", command, ct);
                return Result<LoginResult>.Success(new LoginResult(MfaRequerido: true));
            }

            var secretDecriptografado = _totpService.DescriptografarSecret(conta.MfaSecret!);
            if (!_totpService.ValidarCodigo(secretDecriptografado, command.CodigoMfa))
            {
                await RegistrarLogAsync(conta.Id, emailHash, "mfa_falha", command, ct);
                return Result<LoginResult>.Failure("Código de autenticação inválido.");
            }
        }

        // Login bem-sucedido
        conta.ResetarTentativas();
        await _contaRepo.AtualizarAsync(conta, ct);

        var accessToken = _jwtService.GerarAccessToken(conta.Id, conta.Email.Value);
        var (refreshToken, refreshExpiraEm) = _jwtService.GerarRefreshToken();

        await _refreshTokenRepo.CriarAsync(conta.Id, refreshToken, refreshExpiraEm, command.Dispositivo, ct);
        await RegistrarLogAsync(conta.Id, emailHash, "login_ok", command, ct);

        return Result<LoginResult>.Success(new LoginResult(
            MfaRequerido: false,
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            RefreshTokenExpiraEm: refreshExpiraEm));
    }

    private async Task<Result<LoginResult>> FalharLoginAsync(
        Guid? contaId, LoginCommand command, string evento, CancellationToken ct, string mensagem)
    {
        await RegistrarLogAsync(contaId, HashEmail(command.Email), evento, command, ct);
        return Result<LoginResult>.Failure(mensagem);
    }

    private async Task RegistrarLogAsync(
        Guid? contaId, string emailHash, string evento, LoginCommand command, CancellationToken ct)
    {
        try
        {
            await _logRepo.RegistrarAsync(contaId, emailHash, evento, command.IpOrigem, command.UserAgent, ct);
        }
        catch
        {
            // Log não deve quebrar o fluxo de autenticação
        }
    }

    private static string HashEmail(string email)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(email.ToLowerInvariant()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
