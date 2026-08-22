using Calegrafia.Application.Auth.Commands;
using Calegrafia.Application.Auth.Handlers;
using Calegrafia.Domain.Entities;
using Calegrafia.Domain.Interfaces;
using Calegrafia.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Calegrafia.Application.Tests.Auth;

public sealed class ConfirmarEmailHandlerTests
{
    private readonly IContaRepository _contaRepo = Substitute.For<IContaRepository>();
    private readonly ITokenConfirmacaoRepository _tokenRepo = Substitute.For<ITokenConfirmacaoRepository>();

    private ConfirmarEmailHandler CriarHandler() => new(_contaRepo, _tokenRepo);

    private static TokenConfirmacaoData TokenValido(Guid? contaId = null) =>
        new(Guid.NewGuid(), contaId ?? Guid.NewGuid(),
            "confirmacao_email", "token-valido",
            DateTime.UtcNow.AddHours(23), Usado: false, DateTime.UtcNow);

    private static TokenConfirmacaoData TokenExpirado() =>
        new(Guid.NewGuid(), Guid.NewGuid(),
            "confirmacao_email", "token-expirado",
            DateTime.UtcNow.AddHours(-1), Usado: false, DateTime.UtcNow);

    private static TokenConfirmacaoData TokenUsado() =>
        new(Guid.NewGuid(), Guid.NewGuid(),
            "confirmacao_email", "token-usado",
            DateTime.UtcNow.AddHours(23), Usado: true, DateTime.UtcNow);

    // ── Token inválido ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_TokenNaoEncontrado_RetornaFalha()
    {
        _tokenRepo.ObterPorTokenAsync(Arg.Any<string>()).Returns((TokenConfirmacaoData?)null);

        var result = await CriarHandler().HandleAsync(new ConfirmarEmailCommand("token-inexistente"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("inválido");
    }

    [Fact]
    public async Task Handle_TokenExpirado_RetornaFalha()
    {
        _tokenRepo.ObterPorTokenAsync(Arg.Any<string>()).Returns(TokenExpirado());

        var result = await CriarHandler().HandleAsync(new ConfirmarEmailCommand("token-expirado"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("expirou");
    }

    [Fact]
    public async Task Handle_TokenJaUsado_RetornaFalha()
    {
        _tokenRepo.ObterPorTokenAsync(Arg.Any<string>()).Returns(TokenUsado());

        var result = await CriarHandler().HandleAsync(new ConfirmarEmailCommand("token-usado"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("já foi utilizado");
    }

    [Fact]
    public async Task Handle_TokenTipoErrado_RetornaFalha()
    {
        var tokenTipoErrado = new TokenConfirmacaoData(
            Guid.NewGuid(), Guid.NewGuid(),
            "redefinicao_senha", "token-tipo-errado",
            DateTime.UtcNow.AddHours(1), false, DateTime.UtcNow);

        _tokenRepo.ObterPorTokenAsync(Arg.Any<string>()).Returns(tokenTipoErrado);

        var result = await CriarHandler().HandleAsync(new ConfirmarEmailCommand("token-tipo-errado"));

        result.IsFailure.Should().BeTrue();
    }

    // ── Token válido ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_TokenValido_RetornaSucesso()
    {
        var contaId = Guid.NewGuid();
        var token = TokenValido(contaId);
        _tokenRepo.ObterPorTokenAsync(Arg.Any<string>()).Returns(token);

        var email = Email.Create("user@test.com").Value!;
        var conta = Conta.Criar(email).Value!;
        _contaRepo.ObterPorIdAsync(contaId).Returns(conta);

        var result = await CriarHandler().HandleAsync(new ConfirmarEmailCommand("token-valido"));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_TokenValido_MarcaTokenComoUsado()
    {
        var contaId = Guid.NewGuid();
        var token = TokenValido(contaId);
        _tokenRepo.ObterPorTokenAsync(Arg.Any<string>()).Returns(token);

        var email = Email.Create("user@test.com").Value!;
        var conta = Conta.Criar(email).Value!;
        _contaRepo.ObterPorIdAsync(contaId).Returns(conta);

        await CriarHandler().HandleAsync(new ConfirmarEmailCommand("token-valido"));

        await _tokenRepo.Received(1).MarcarComoUsadoAsync(token.Id);
    }

    [Fact]
    public async Task Handle_TokenValido_AtualizaConta()
    {
        var contaId = Guid.NewGuid();
        var token = TokenValido(contaId);
        _tokenRepo.ObterPorTokenAsync(Arg.Any<string>()).Returns(token);

        var email = Email.Create("user@test.com").Value!;
        var conta = Conta.Criar(email).Value!;
        _contaRepo.ObterPorIdAsync(contaId).Returns(conta);

        await CriarHandler().HandleAsync(new ConfirmarEmailCommand("token-valido"));

        await _contaRepo.Received(1).AtualizarAsync(Arg.Any<Domain.Entities.Conta>());
    }
}
