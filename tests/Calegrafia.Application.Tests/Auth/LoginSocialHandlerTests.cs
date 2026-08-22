using Calegrafia.Application.Auth.Commands;
using Calegrafia.Application.Auth.Handlers;
using Calegrafia.Domain.Entities;
using Calegrafia.Domain.Interfaces;
using Calegrafia.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Calegrafia.Application.Tests.Auth;

public sealed class LoginSocialHandlerTests
{
    private readonly IContaRepository _contaRepo = Substitute.For<IContaRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepo = Substitute.For<IRefreshTokenRepository>();
    private readonly IProvedorSocialRepository _provedorRepo = Substitute.For<IProvedorSocialRepository>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly ISocialLoginProvider _googleProvider = Substitute.For<ISocialLoginProvider>();
    private readonly ISocialLoginProvider _appleProvider = Substitute.For<ISocialLoginProvider>();

    private LoginSocialHandler CriarHandler()
    {
        _googleProvider.Provedor.Returns("google");
        _appleProvider.Provedor.Returns("apple");
        return new(_contaRepo, _refreshTokenRepo, _provedorRepo, _jwtService,
            new[] { _googleProvider, _appleProvider });
    }

    private static LoginSocialCommand ComandoGoogle(string token = "token-google") =>
        new("google", token, "dispositivo-teste");

    private static LoginSocialCommand ComandoApple(string token = "token-apple") =>
        new("apple", token);

    private static SocialUserInfo UserInfoValido(string email = "social@test.com") =>
        new("sub-123", email, "Usuário Social");

    // ── Provedor não suportado ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProvedorNaoSuportado_RetornaFalha()
    {
        var result = await CriarHandler().HandleAsync(new LoginSocialCommand("facebook", "token"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("não suportado");
    }

    // ── Token inválido ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_TokenGoogleInvalido_RetornaFalha()
    {
        _googleProvider.ValidarTokenAsync(Arg.Any<string>()).Returns((SocialUserInfo?)null);

        var result = await CriarHandler().HandleAsync(ComandoGoogle("token-invalido"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("inválido");
    }

    [Fact]
    public async Task Handle_TokenAppleInvalido_RetornaFalha()
    {
        _appleProvider.ValidarTokenAsync(Arg.Any<string>()).Returns((SocialUserInfo?)null);

        var result = await CriarHandler().HandleAsync(ComandoApple("token-invalido"));

        result.IsFailure.Should().BeTrue();
    }

    // ── Novo usuário ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NovoUsuarioGoogle_CriarContaAtivada()
    {
        _googleProvider.ValidarTokenAsync(Arg.Any<string>()).Returns(UserInfoValido());
        _contaRepo.ObterPorEmailAsync(Arg.Any<Email>()).Returns((Conta?)null);
        var novaContaId = Guid.NewGuid();
        _contaRepo.CriarAsync(Arg.Any<Conta>()).Returns(novaContaId);
        var email = Email.Create("social@test.com").Value!;
        var conta = Conta.Criar(email).Value!;
        conta.Ativar();
        _contaRepo.ObterPorIdAsync(novaContaId).Returns(conta);
        _jwtService.GerarAccessToken(Arg.Any<Guid>(), Arg.Any<string>()).Returns("access");
        _jwtService.GerarRefreshToken().Returns(("refresh", DateTime.UtcNow.AddDays(30)));

        var result = await CriarHandler().HandleAsync(ComandoGoogle());

        result.IsSuccess.Should().BeTrue();
        await _contaRepo.Received(1).CriarAsync(Arg.Any<Conta>());
    }

    [Fact]
    public async Task Handle_NovoUsuario_VincularProvedor()
    {
        _googleProvider.ValidarTokenAsync(Arg.Any<string>()).Returns(UserInfoValido());
        _contaRepo.ObterPorEmailAsync(Arg.Any<Email>()).Returns((Conta?)null);
        var novaContaId = Guid.NewGuid();
        _contaRepo.CriarAsync(Arg.Any<Conta>()).Returns(novaContaId);
        var email = Email.Create("social@test.com").Value!;
        var conta = Conta.Criar(email).Value!;
        conta.Ativar();
        _contaRepo.ObterPorIdAsync(novaContaId).Returns(conta);
        _jwtService.GerarAccessToken(Arg.Any<Guid>(), Arg.Any<string>()).Returns("access");
        _jwtService.GerarRefreshToken().Returns(("refresh", DateTime.UtcNow.AddDays(30)));

        await CriarHandler().HandleAsync(ComandoGoogle());

        await _provedorRepo.Received(1).VincularSeNaoExistirAsync(
            novaContaId, "google", "sub-123");
    }

    // ── Usuário existente ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_UsuarioExistente_NaoCriaNovaConta()
    {
        var email = Email.Create("existente@test.com").Value!;
        var contaExistente = Conta.Criar(email).Value!;
        contaExistente.Ativar();

        _googleProvider.ValidarTokenAsync(Arg.Any<string>()).Returns(UserInfoValido("existente@test.com"));
        _contaRepo.ObterPorEmailAsync(Arg.Any<Email>()).Returns(contaExistente);
        _jwtService.GerarAccessToken(Arg.Any<Guid>(), Arg.Any<string>()).Returns("access");
        _jwtService.GerarRefreshToken().Returns(("refresh", DateTime.UtcNow.AddDays(30)));

        var result = await CriarHandler().HandleAsync(ComandoGoogle());

        result.IsSuccess.Should().BeTrue();
        await _contaRepo.DidNotReceive().CriarAsync(Arg.Any<Conta>());
    }

    [Fact]
    public async Task Handle_UsuarioExistente_VinculaProvedorSeNaoVinculado()
    {
        var email = Email.Create("existente@test.com").Value!;
        var contaExistente = Conta.Criar(email).Value!;
        contaExistente.Ativar();

        _googleProvider.ValidarTokenAsync(Arg.Any<string>()).Returns(UserInfoValido("existente@test.com"));
        _contaRepo.ObterPorEmailAsync(Arg.Any<Email>()).Returns(contaExistente);
        _jwtService.GerarAccessToken(Arg.Any<Guid>(), Arg.Any<string>()).Returns("access");
        _jwtService.GerarRefreshToken().Returns(("refresh", DateTime.UtcNow.AddDays(30)));

        await CriarHandler().HandleAsync(ComandoGoogle());

        await _provedorRepo.Received(1).VincularSeNaoExistirAsync(
            contaExistente.Id, "google", "sub-123");
    }

    // ── Login bem-sucedido ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_LoginSocialValido_RetornaTokens()
    {
        var email = Email.Create("social@test.com").Value!;
        var conta = Conta.Criar(email).Value!;
        conta.Ativar();

        _googleProvider.ValidarTokenAsync(Arg.Any<string>()).Returns(UserInfoValido());
        _contaRepo.ObterPorEmailAsync(Arg.Any<Email>()).Returns(conta);
        _jwtService.GerarAccessToken(Arg.Any<Guid>(), Arg.Any<string>()).Returns("access-social");
        _jwtService.GerarRefreshToken().Returns(("refresh-social", DateTime.UtcNow.AddDays(30)));

        var result = await CriarHandler().HandleAsync(ComandoGoogle());

        result.IsSuccess.Should().BeTrue();
        result.Value!.MfaRequerido.Should().BeFalse();
        result.Value.AccessToken.Should().Be("access-social");
    }
}
