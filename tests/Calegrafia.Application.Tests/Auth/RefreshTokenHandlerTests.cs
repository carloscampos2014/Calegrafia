using Calegrafia.Application.Auth.Commands;
using Calegrafia.Application.Auth.Handlers;
using Calegrafia.Domain.Entities;
using Calegrafia.Domain.Interfaces;
using Calegrafia.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Calegrafia.Application.Tests.Auth;

public sealed class RefreshTokenHandlerTests
{
    private readonly IContaRepository _contaRepo = Substitute.For<IContaRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepo = Substitute.For<IRefreshTokenRepository>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();

    private RefreshTokenHandler CriarHandler() =>
        new(_contaRepo, _refreshTokenRepo, _jwtService);

    private static RefreshTokenData TokenValido(Guid? contaId = null) =>
        new(Guid.NewGuid(), contaId ?? Guid.NewGuid(),
            "token-valido", DateTime.UtcNow.AddDays(29),
            Revogado: false, null, DateTime.UtcNow);

    // ── Token inválido ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_TokenNaoEncontrado_RetornaFalha()
    {
        _refreshTokenRepo.ObterPorTokenAsync(Arg.Any<string>()).Returns((RefreshTokenData?)null);

        var result = await CriarHandler().HandleAsync(new RefreshTokenCommand("token-invalido"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("inválido");
    }

    [Fact]
    public async Task Handle_TokenRevogado_RetornaFalha()
    {
        var tokenRevogado = new RefreshTokenData(
            Guid.NewGuid(), Guid.NewGuid(), "token-revogado",
            DateTime.UtcNow.AddDays(29), Revogado: true, null, DateTime.UtcNow);

        _refreshTokenRepo.ObterPorTokenAsync(Arg.Any<string>()).Returns(tokenRevogado);

        var result = await CriarHandler().HandleAsync(new RefreshTokenCommand("token-revogado"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("revogado");
    }

    [Fact]
    public async Task Handle_TokenExpirado_RetornaFalha()
    {
        var tokenExpirado = new RefreshTokenData(
            Guid.NewGuid(), Guid.NewGuid(), "token-expirado",
            DateTime.UtcNow.AddDays(-1), Revogado: false, null, DateTime.UtcNow);

        _refreshTokenRepo.ObterPorTokenAsync(Arg.Any<string>()).Returns(tokenExpirado);

        var result = await CriarHandler().HandleAsync(new RefreshTokenCommand("token-expirado"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("expirado");
    }

    // ── Rotação de token ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_TokenValido_RevogarTokenAntigo()
    {
        var contaId = Guid.NewGuid();
        var token = TokenValido(contaId);
        _refreshTokenRepo.ObterPorTokenAsync(Arg.Any<string>()).Returns(token);

        var email = Email.Create("user@test.com").Value!;
        var conta = Conta.Criar(email).Value!;
        conta.Ativar();
        _contaRepo.ObterPorIdAsync(contaId).Returns(conta);
        _jwtService.GerarAccessToken(Arg.Any<Guid>(), Arg.Any<string>()).Returns("novo-access");
        _jwtService.GerarRefreshToken().Returns(("novo-refresh", DateTime.UtcNow.AddDays(30)));

        await CriarHandler().HandleAsync(new RefreshTokenCommand("token-valido"));

        await _refreshTokenRepo.Received(1).RevogarAsync("token-valido");
    }

    [Fact]
    public async Task Handle_TokenValido_CriarNovoRefreshToken()
    {
        var contaId = Guid.NewGuid();
        var token = TokenValido(contaId);
        _refreshTokenRepo.ObterPorTokenAsync(Arg.Any<string>()).Returns(token);

        var email = Email.Create("user@test.com").Value!;
        var conta = Conta.Criar(email).Value!;
        conta.Ativar();
        _contaRepo.ObterPorIdAsync(contaId).Returns(conta);
        _jwtService.GerarAccessToken(Arg.Any<Guid>(), Arg.Any<string>()).Returns("novo-access");
        _jwtService.GerarRefreshToken().Returns(("novo-refresh", DateTime.UtcNow.AddDays(30)));

        await CriarHandler().HandleAsync(new RefreshTokenCommand("token-valido"));

        await _refreshTokenRepo.Received(1).CriarAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TokenValido_RetornaNovoAccessToken()
    {
        var contaId = Guid.NewGuid();
        var token = TokenValido(contaId);
        _refreshTokenRepo.ObterPorTokenAsync(Arg.Any<string>()).Returns(token);

        var email = Email.Create("user@test.com").Value!;
        var conta = Conta.Criar(email).Value!;
        conta.Ativar();
        _contaRepo.ObterPorIdAsync(contaId).Returns(conta);
        _jwtService.GerarAccessToken(Arg.Any<Guid>(), Arg.Any<string>()).Returns("novo-access");
        _jwtService.GerarRefreshToken().Returns(("novo-refresh", DateTime.UtcNow.AddDays(30)));

        var result = await CriarHandler().HandleAsync(new RefreshTokenCommand("token-valido"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("novo-access");
        result.Value.RefreshToken.Should().Be("novo-refresh");
    }
}

public sealed class LogoutHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepo = Substitute.For<IRefreshTokenRepository>();

    private LogoutHandler CriarLogoutHandler() => new(_refreshTokenRepo);
    private LogoutTodosHandler CriarLogoutTodosHandler() => new(_refreshTokenRepo);

    // ── Logout simples ────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_TokenValido_Revogar()
    {
        var token = new RefreshTokenData(
            Guid.NewGuid(), Guid.NewGuid(), "meu-token",
            DateTime.UtcNow.AddDays(29), Revogado: false, null, DateTime.UtcNow);

        _refreshTokenRepo.ObterPorTokenAsync("meu-token").Returns(token);

        var result = await CriarLogoutHandler().HandleAsync(new LogoutCommand("meu-token"));

        result.IsSuccess.Should().BeTrue();
        await _refreshTokenRepo.Received(1).RevogarAsync("meu-token");
    }

    [Fact]
    public async Task Logout_TokenJaRevogado_RetornaSucessoSemErro()
    {
        var tokenRevogado = new RefreshTokenData(
            Guid.NewGuid(), Guid.NewGuid(), "token-revogado",
            DateTime.UtcNow.AddDays(29), Revogado: true, null, DateTime.UtcNow);

        _refreshTokenRepo.ObterPorTokenAsync("token-revogado").Returns(tokenRevogado);

        var result = await CriarLogoutHandler().HandleAsync(new LogoutCommand("token-revogado"));

        // Idempotente — não falha
        result.IsSuccess.Should().BeTrue();
        await _refreshTokenRepo.DidNotReceive().RevogarAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task Logout_TokenInexistente_RetornaSucessoIdempotente()
    {
        _refreshTokenRepo.ObterPorTokenAsync(Arg.Any<string>()).Returns((RefreshTokenData?)null);

        var result = await CriarLogoutHandler().HandleAsync(new LogoutCommand("token-inexistente"));

        result.IsSuccess.Should().BeTrue();
    }

    // ── Logout de todos os dispositivos ──────────────────────────────────────

    [Fact]
    public async Task LogoutTodos_RevogarTodosTokensDaConta()
    {
        var contaId = Guid.NewGuid();

        var result = await CriarLogoutTodosHandler().HandleAsync(new LogoutTodosCommand(contaId));

        result.IsSuccess.Should().BeTrue();
        await _refreshTokenRepo.Received(1).RevogarTodosPorContaAsync(contaId);
    }
}
