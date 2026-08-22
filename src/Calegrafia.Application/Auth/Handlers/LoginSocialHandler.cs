using Calegrafia.Application.Auth.Commands;
using Calegrafia.Domain.Common;
using Calegrafia.Domain.Entities;
using Calegrafia.Domain.Interfaces;
using Calegrafia.Domain.ValueObjects;

namespace Calegrafia.Application.Auth.Handlers;

public sealed class LoginSocialHandler
{
    private readonly IContaRepository _contaRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IProvedorSocialRepository _provedorSocialRepo;
    private readonly IJwtService _jwtService;
    private readonly IEnumerable<ISocialLoginProvider> _providers;

    public LoginSocialHandler(
        IContaRepository contaRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IProvedorSocialRepository provedorSocialRepo,
        IJwtService jwtService,
        IEnumerable<ISocialLoginProvider> providers)
    {
        _contaRepo = contaRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _provedorSocialRepo = provedorSocialRepo;
        _jwtService = jwtService;
        _providers = providers;
    }

    public async Task<Result<LoginResult>> HandleAsync(LoginSocialCommand command, CancellationToken ct = default)
    {
        // Encontrar o provider correto
        var provider = _providers.FirstOrDefault(p =>
            p.Provedor.Equals(command.Provedor, StringComparison.OrdinalIgnoreCase));

        if (provider is null)
            return Result<LoginResult>.Failure($"Provedor '{command.Provedor}' não suportado.");

        // Validar token no provedor externo
        var userInfo = await provider.ValidarTokenAsync(command.Token, ct);
        if (userInfo is null)
            return Result<LoginResult>.Failure("Token do provedor inválido ou expirado.");

        // Validar email retornado pelo provedor
        var emailResult = Email.Create(userInfo.Email);
        if (emailResult.IsFailure)
            return Result<LoginResult>.Failure("Email retornado pelo provedor é inválido.");

        var email = emailResult.Value!;

        // Verificar se já existe conta com esse email
        var conta = await _contaRepo.ObterPorEmailAsync(email, ct);

        if (conta is not null)
        {
            // Vincular provedor à conta existente (se ainda não vinculado)
            await _provedorSocialRepo.VincularSeNaoExistirAsync(
                conta.Id, command.Provedor, userInfo.SubjectId, ct);
        }
        else
        {
            // Criar nova conta ativa (sem confirmação de email — social login já valida)
            var novaConta = Conta.Criar(email).Value!;
            novaConta.Ativar();
            var novaContaId = await _contaRepo.CriarAsync(novaConta, ct);

            await _provedorSocialRepo.VincularSeNaoExistirAsync(
                novaContaId, command.Provedor, userInfo.SubjectId, ct);

            conta = await _contaRepo.ObterPorIdAsync(novaContaId, ct);
        }

        if (conta is null)
            return Result<LoginResult>.Failure("Falha ao criar ou recuperar a conta.");

        if (conta.EstaBloqueada())
            return Result<LoginResult>.Failure("Conta bloqueada. Tente novamente mais tarde.");

        // Gerar tokens
        var accessToken = _jwtService.GerarAccessToken(conta.Id, conta.Email.Value);
        var (refreshToken, refreshExpiraEm) = _jwtService.GerarRefreshToken();

        await _refreshTokenRepo.CriarAsync(
            conta.Id, refreshToken, refreshExpiraEm, command.Dispositivo, ct);

        return Result<LoginResult>.Success(new LoginResult(
            MfaRequerido: false,
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            RefreshTokenExpiraEm: refreshExpiraEm));
    }
}
