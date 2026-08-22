using Calegrafia.Application.Auth.Commands;
using Calegrafia.Application.Auth.Handlers;
using Calegrafia.Domain.Entities;
using Calegrafia.Domain.Interfaces;
using Calegrafia.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Calegrafia.Application.Tests.Auth;

public sealed class LoginHandlerTests
{
    private readonly IContaRepository _contaRepo = Substitute.For<IContaRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepo = Substitute.For<IRefreshTokenRepository>();
    private readonly ILogAutenticacaoRepository _logRepo = Substitute.For<ILogAutenticacaoRepository>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITotpService _totpService = Substitute.For<ITotpService>();

    private LoginHandler CriarHandler() => new(
        _contaRepo, _refreshTokenRepo, _logRepo,
        _jwtService, _hasher, _totpService);

    private static LoginCommand ComandoValido(
        string email = "user@test.com",
        string senha = "Senha123",
        string? codigoMfa = null) =>
        new(email, senha, codigoMfa, "127.0.0.1", "TestAgent");

    private Conta CriarContaAtiva(string email = "user@test.com")
    {
        var emailVo = Email.Create(email).Value!;
        var conta = Conta.Criar(emailVo, "hash").Value!;
        conta.Ativar();
        return conta;
    }

    // ── Email inválido ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_EmailInvalido_RetornaFalha()
    {
        var result = await CriarHandler().HandleAsync(ComandoValido(email: "nao-e-email"));
        result.IsFailure.Should().BeTrue();
    }

    // ── Conta não encontrada ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ContaNaoEncontrada_RetornaMensagemGenerica()
    {
        _contaRepo.ObterPorEmailAsync(Arg.Any<Email>()).Returns((Conta?)null);

        var result = await CriarHandler().HandleAsync(ComandoValido());

        result.IsFailure.Should().BeTrue();
        // Não revela se o email existe ou não
        result.Error.Should().Be("Email ou senha incorretos.");
    }

    // ── Conta não confirmada ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ContaNaoConfirmada_RetornaFalha()
    {
        var emailVo = Email.Create("user@test.com").Value!;
        var contaPendente = Conta.Criar(emailVo, "hash").Value!; // status Pendente
        _contaRepo.ObterPorEmailAsync(Arg.Any<Email>()).Returns(contaPendente);

        var result = await CriarHandler().HandleAsync(ComandoValido());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Conta não confirmada");
    }

    // ── Senha incorreta + bloqueio ────────────────────────────────────────────

    [Fact]
    public async Task Handle_SenhaErrada_RetornaFalha()
    {
        var conta = CriarContaAtiva();
        _contaRepo.ObterPorEmailAsync(Arg.Any<Email>()).Returns(conta);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var result = await CriarHandler().HandleAsync(ComandoValido());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SenhaErrada_RegistraLogDeFalha()
    {
        var conta = CriarContaAtiva();
        _contaRepo.ObterPorEmailAsync(Arg.Any<Email>()).Returns(conta);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        await CriarHandler().HandleAsync(ComandoValido());

        await _logRepo.Received(1).RegistrarAsync(
            conta.Id, Arg.Any<string>(), "login_falha",
            Arg.Any<string?>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task Handle_ContaBloqueada_RetornaFalha()
    {
        var conta = CriarContaAtiva();
        // Simular 5 tentativas para bloquear
        for (var i = 0; i < 5; i++)
            conta.RegistrarTentativaFalha();

        _contaRepo.ObterPorEmailAsync(Arg.Any<Email>()).Returns(conta);

        var result = await CriarHandler().HandleAsync(ComandoValido());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("bloqueada");
    }

    // ── MFA ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_MfaAtivo_SemCodigo_RetornaMfaRequerido()
    {
        var conta = CriarContaAtiva();
        conta.AtivarMfa("secret-criptografado");
        _contaRepo.ObterPorEmailAsync(Arg.Any<Email>()).Returns(conta);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var result = await CriarHandler().HandleAsync(ComandoValido());

        result.IsSuccess.Should().BeTrue();
        result.Value!.MfaRequerido.Should().BeTrue();
        result.Value.AccessToken.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MfaAtivo_CodigoInvalido_RetornaFalha()
    {
        var conta = CriarContaAtiva();
        conta.AtivarMfa("secret-criptografado");
        _contaRepo.ObterPorEmailAsync(Arg.Any<Email>()).Returns(conta);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _totpService.DescriptografarSecret(Arg.Any<string>()).Returns("secret");
        _totpService.ValidarCodigo(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var result = await CriarHandler().HandleAsync(ComandoValido(codigoMfa: "000000"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("inválido");
    }

    [Fact]
    public async Task Handle_MfaAtivo_CodigoValido_RetornaTokens()
    {
        var conta = CriarContaAtiva();
        conta.AtivarMfa("secret-criptografado");
        _contaRepo.ObterPorEmailAsync(Arg.Any<Email>()).Returns(conta);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _totpService.DescriptografarSecret(Arg.Any<string>()).Returns("secret");
        _totpService.ValidarCodigo(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _jwtService.GerarAccessToken(Arg.Any<Guid>(), Arg.Any<string>()).Returns("access-token");
        _jwtService.GerarRefreshToken().Returns(("refresh-token", DateTime.UtcNow.AddDays(30)));

        var result = await CriarHandler().HandleAsync(ComandoValido(codigoMfa: "123456"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.MfaRequerido.Should().BeFalse();
        result.Value.AccessToken.Should().Be("access-token");
    }

    // ── Login bem-sucedido ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CredenciaisValidas_RetornaTokens()
    {
        var conta = CriarContaAtiva();
        _contaRepo.ObterPorEmailAsync(Arg.Any<Email>()).Returns(conta);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _jwtService.GerarAccessToken(Arg.Any<Guid>(), Arg.Any<string>()).Returns("access-token");
        _jwtService.GerarRefreshToken().Returns(("refresh-token", DateTime.UtcNow.AddDays(30)));

        var result = await CriarHandler().HandleAsync(ComandoValido());

        result.IsSuccess.Should().BeTrue();
        result.Value!.MfaRequerido.Should().BeFalse();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
    }

    [Fact]
    public async Task Handle_LoginBemSucedido_RegistraLogOk()
    {
        var conta = CriarContaAtiva();
        _contaRepo.ObterPorEmailAsync(Arg.Any<Email>()).Returns(conta);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _jwtService.GerarAccessToken(Arg.Any<Guid>(), Arg.Any<string>()).Returns("access-token");
        _jwtService.GerarRefreshToken().Returns(("refresh-token", DateTime.UtcNow.AddDays(30)));

        await CriarHandler().HandleAsync(ComandoValido());

        await _logRepo.Received(1).RegistrarAsync(
            conta.Id, Arg.Any<string>(), "login_ok",
            Arg.Any<string?>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task Handle_LoginBemSucedido_CriaRefreshToken()
    {
        var conta = CriarContaAtiva();
        _contaRepo.ObterPorEmailAsync(Arg.Any<Email>()).Returns(conta);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _jwtService.GerarAccessToken(Arg.Any<Guid>(), Arg.Any<string>()).Returns("access-token");
        _jwtService.GerarRefreshToken().Returns(("refresh-token", DateTime.UtcNow.AddDays(30)));

        await CriarHandler().HandleAsync(ComandoValido());

        await _refreshTokenRepo.Received(1).CriarAsync(
            conta.Id, "refresh-token", Arg.Any<DateTime>(), Arg.Any<string?>());
    }
}
