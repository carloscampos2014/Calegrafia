using Calegrafia.Application.Auth.Commands;
using Calegrafia.Application.Auth.Handlers;
using Calegrafia.Domain.Entities;
using Calegrafia.Domain.Interfaces;
using Calegrafia.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Calegrafia.Application.Tests.Auth;

// ── AtivarMfaHandler ──────────────────────────────────────────────────────────

public sealed class AtivarMfaHandlerTests
{
    private readonly IContaRepository _contaRepo = Substitute.For<IContaRepository>();
    private readonly ITotpService _totpService = Substitute.For<ITotpService>();

    private AtivarMfaHandler CriarHandler() => new(_contaRepo, _totpService);

    private Conta CriarContaAtiva()
    {
        var email = Email.Create("user@test.com").Value!;
        var conta = Conta.Criar(email, "hash").Value!;
        conta.Ativar();
        return conta;
    }

    [Fact]
    public async Task Handle_ContaNaoEncontrada_RetornaFalha()
    {
        _contaRepo.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Conta?)null);

        var result = await CriarHandler().HandleAsync(new AtivarMfaCommand(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("não encontrada");
    }

    [Fact]
    public async Task Handle_MfaJaAtivo_RetornaFalha()
    {
        var conta = CriarContaAtiva();
        conta.AtivarMfa("secret");
        _contaRepo.ObterPorIdAsync(Arg.Any<Guid>()).Returns(conta);

        var result = await CriarHandler().HandleAsync(new AtivarMfaCommand(conta.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("já está ativo");
    }

    [Fact]
    public async Task Handle_ContaValida_RetornaQrCodeESecret()
    {
        var conta = CriarContaAtiva();
        _contaRepo.ObterPorIdAsync(Arg.Any<Guid>()).Returns(conta);
        _totpService.GerarSecret().Returns("SECRETBASE32");
        _totpService.GerarQrCodeUri(Arg.Any<string>(), Arg.Any<string>()).Returns("otpauth://totp/...");

        var result = await CriarHandler().HandleAsync(new AtivarMfaCommand(conta.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value!.QrCodeUri.Should().Contain("otpauth://");
        result.Value.SecretPlain.Should().Be("SECRETBASE32");
    }

    [Fact]
    public async Task Confirmar_CodigoInvalido_RetornaFalha()
    {
        var conta = CriarContaAtiva();
        _contaRepo.ObterPorIdAsync(Arg.Any<Guid>()).Returns(conta);
        _totpService.ValidarCodigo(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var result = await CriarHandler().ConfirmarAsync(
            new AtivarMfaConfirmarCommand(conta.Id, "SECRETBASE32", "000000"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("inválido");
    }

    [Fact]
    public async Task Confirmar_CodigoValido_AtivarMfaEPersistir()
    {
        var conta = CriarContaAtiva();
        _contaRepo.ObterPorIdAsync(Arg.Any<Guid>()).Returns(conta);
        _totpService.ValidarCodigo(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _totpService.CriptografarSecret(Arg.Any<string>()).Returns("SECRET_CRIPTOGRAFADO");

        var result = await CriarHandler().ConfirmarAsync(
            new AtivarMfaConfirmarCommand(conta.Id, "SECRETBASE32", "123456"));

        result.IsSuccess.Should().BeTrue();
        await _contaRepo.Received(1).AtualizarAsync(Arg.Any<Conta>());
    }
}

// ── DesativarMfaHandler ───────────────────────────────────────────────────────

public sealed class DesativarMfaHandlerTests
{
    private readonly IContaRepository _contaRepo = Substitute.For<IContaRepository>();
    private readonly ITotpService _totpService = Substitute.For<ITotpService>();

    private DesativarMfaHandler CriarHandler() => new(_contaRepo, _totpService);

    private Conta CriarContaComMfa()
    {
        var email = Email.Create("user@test.com").Value!;
        var conta = Conta.Criar(email, "hash").Value!;
        conta.Ativar();
        conta.AtivarMfa("SECRET_CRIPTOGRAFADO");
        return conta;
    }

    [Fact]
    public async Task Handle_ContaNaoEncontrada_RetornaFalha()
    {
        _contaRepo.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Conta?)null);

        var result = await CriarHandler().HandleAsync(
            new DesativarMfaCommand(Guid.NewGuid(), "123456"));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MfaNaoAtivo_RetornaFalha()
    {
        var email = Email.Create("user@test.com").Value!;
        var conta = Conta.Criar(email, "hash").Value!;
        conta.Ativar();
        _contaRepo.ObterPorIdAsync(Arg.Any<Guid>()).Returns(conta);

        var result = await CriarHandler().HandleAsync(
            new DesativarMfaCommand(conta.Id, "123456"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("não está ativo");
    }

    [Fact]
    public async Task Handle_CodigoInvalido_RetornaFalha()
    {
        var conta = CriarContaComMfa();
        _contaRepo.ObterPorIdAsync(Arg.Any<Guid>()).Returns(conta);
        _totpService.DescriptografarSecret(Arg.Any<string>()).Returns("SECRET_PLAIN");
        _totpService.ValidarCodigo(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var result = await CriarHandler().HandleAsync(
            new DesativarMfaCommand(conta.Id, "000000"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("inválido");
    }

    [Fact]
    public async Task Handle_CodigoValido_DesativarMfaEPersistir()
    {
        var conta = CriarContaComMfa();
        _contaRepo.ObterPorIdAsync(Arg.Any<Guid>()).Returns(conta);
        _totpService.DescriptografarSecret(Arg.Any<string>()).Returns("SECRET_PLAIN");
        _totpService.ValidarCodigo(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var result = await CriarHandler().HandleAsync(
            new DesativarMfaCommand(conta.Id, "123456"));

        result.IsSuccess.Should().BeTrue();
        await _contaRepo.Received(1).AtualizarAsync(Arg.Any<Conta>());
    }
}

// ── ResetMfaHandler ───────────────────────────────────────────────────────────

public sealed class ResetMfaHandlerTests
{
    private readonly IContaRepository _contaRepo = Substitute.For<IContaRepository>();
    private readonly ITokenConfirmacaoRepository _tokenRepo = Substitute.For<ITokenConfirmacaoRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepo = Substitute.For<IRefreshTokenRepository>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();

    private ResetMfaSolicitarHandler CriarSolicitarHandler() =>
        new(_contaRepo, _tokenRepo, _emailService, "https://app.calegrafia.com");

    private ResetMfaConfirmarHandler CriarConfirmarHandler() =>
        new(_contaRepo, _tokenRepo, _refreshTokenRepo);

    private static TokenConfirmacaoData TokenResetMfaValido(Guid? contaId = null) =>
        new(Guid.NewGuid(), contaId ?? Guid.NewGuid(),
            "reset_mfa", "token-reset",
            DateTime.UtcNow.AddMinutes(9), false, DateTime.UtcNow);

    // ── Solicitar ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Solicitar_EmailInvalido_RetornaSucessoSilencioso()
    {
        var result = await CriarSolicitarHandler().HandleAsync(
            new ResetMfaSolicitarCommand("nao-e-email"));

        result.IsSuccess.Should().BeTrue();
        await _emailService.DidNotReceive().EnviarResetMfaAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Solicitar_ContaSemMfaAtivo_RetornaSucessoSilencioso()
    {
        var email = Email.Create("user@test.com").Value!;
        var conta = Conta.Criar(email, "hash").Value!;
        conta.Ativar(); // MFA não ativo
        _contaRepo.ObterPorEmailAsync(Arg.Any<Email>()).Returns(conta);

        var result = await CriarSolicitarHandler().HandleAsync(
            new ResetMfaSolicitarCommand("user@test.com"));

        result.IsSuccess.Should().BeTrue();
        await _emailService.DidNotReceive().EnviarResetMfaAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Solicitar_ContaComMfaAtivo_EnviaEmail()
    {
        var email = Email.Create("user@test.com").Value!;
        var conta = Conta.Criar(email, "hash").Value!;
        conta.Ativar();
        conta.AtivarMfa("secret");
        _contaRepo.ObterPorEmailAsync(Arg.Any<Email>()).Returns(conta);
        _tokenRepo.CriarAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(Guid.NewGuid());

        var result = await CriarSolicitarHandler().HandleAsync(
            new ResetMfaSolicitarCommand("user@test.com"));

        result.IsSuccess.Should().BeTrue();
        await _emailService.Received(1).EnviarResetMfaAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // ── Confirmar ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Confirmar_TokenInvalido_RetornaFalha()
    {
        _tokenRepo.ObterPorTokenAsync(Arg.Any<string>()).Returns((TokenConfirmacaoData?)null);

        var result = await CriarConfirmarHandler().HandleAsync(
            new ResetMfaConfirmarCommand("token-invalido"));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Confirmar_TokenExpirado_RetornaFalha()
    {
        var tokenExpirado = new TokenConfirmacaoData(
            Guid.NewGuid(), Guid.NewGuid(), "reset_mfa", "token",
            DateTime.UtcNow.AddMinutes(-1), false, DateTime.UtcNow);
        _tokenRepo.ObterPorTokenAsync(Arg.Any<string>()).Returns(tokenExpirado);

        var result = await CriarConfirmarHandler().HandleAsync(
            new ResetMfaConfirmarCommand("token"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("expirou");
    }

    [Fact]
    public async Task Confirmar_TokenValido_DesativarMfaERevogarTokens()
    {
        var contaId = Guid.NewGuid();
        var token = TokenResetMfaValido(contaId);
        _tokenRepo.ObterPorTokenAsync(Arg.Any<string>()).Returns(token);

        var email = Email.Create("user@test.com").Value!;
        var conta = Conta.Criar(email, "hash").Value!;
        conta.Ativar();
        conta.AtivarMfa("secret");
        _contaRepo.ObterPorIdAsync(contaId).Returns(conta);

        var result = await CriarConfirmarHandler().HandleAsync(
            new ResetMfaConfirmarCommand("token-reset"));

        result.IsSuccess.Should().BeTrue();
        await _contaRepo.Received(1).AtualizarAsync(Arg.Any<Conta>());
        await _tokenRepo.Received(1).MarcarComoUsadoAsync(token.Id, Arg.Any<CancellationToken>());
        await _refreshTokenRepo.Received(1).RevogarTodosPorContaAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
