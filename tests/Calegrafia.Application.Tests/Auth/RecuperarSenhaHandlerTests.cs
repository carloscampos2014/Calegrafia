using Calegrafia.Application.Auth.Commands;
using Calegrafia.Application.Auth.Handlers;
using Calegrafia.Domain.Entities;
using Calegrafia.Domain.Interfaces;
using Calegrafia.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Calegrafia.Application.Tests.Auth;

public sealed class RecuperarSenhaHandlerTests
{
    private readonly IContaRepository _contaRepo = Substitute.For<IContaRepository>();
    private readonly ITokenConfirmacaoRepository _tokenRepo = Substitute.For<ITokenConfirmacaoRepository>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();

    private RecuperarSenhaHandler CriarHandler() =>
        new(_contaRepo, _tokenRepo, _emailService, "https://app.calegrafia.com");

    // ── Segurança — não revelar se email existe ───────────────────────────────

    [Fact]
    public async Task Handle_EmailInvalido_RetornaSucessoSemErro()
    {
        var result = await CriarHandler().HandleAsync(new RecuperarSenhaCommand("nao-e-email"));

        result.IsSuccess.Should().BeTrue(); // Não revela que email é inválido
        await _tokenRepo.DidNotReceive().CriarAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Handle_EmailNaoCadastrado_RetornaSucessoSemEmail()
    {
        _contaRepo.ObterPorEmailAsync(Arg.Any<Email>()).Returns((Conta?)null);

        var result = await CriarHandler().HandleAsync(new RecuperarSenhaCommand("naoexiste@test.com"));

        result.IsSuccess.Should().BeTrue(); // Não revela que email não existe
        await _emailService.DidNotReceive().EnviarRedefinicaoSenhaAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // ── Email válido e cadastrado ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_EmailValido_CriaTokenEEnviaEmail()
    {
        var email = Email.Create("user@test.com").Value!;
        var conta = Conta.Criar(email).Value!;
        conta.Ativar();
        _contaRepo.ObterPorEmailAsync(Arg.Any<Email>()).Returns(conta);
        _tokenRepo.CriarAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(Guid.NewGuid());

        var result = await CriarHandler().HandleAsync(new RecuperarSenhaCommand("user@test.com"));

        result.IsSuccess.Should().BeTrue();
        await _tokenRepo.Received(1).CriarAsync(
            conta.Id, "redefinicao_senha", Arg.Any<string>(),
            Arg.Is<DateTime>(d => d > DateTime.UtcNow.AddMinutes(9) && d < DateTime.UtcNow.AddMinutes(11)));
        await _emailService.Received(1).EnviarRedefinicaoSenhaAsync(
            "user@test.com", Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_FalhaNoEnvioEmail_AindaRetornaSucesso()
    {
        var email = Email.Create("user@test.com").Value!;
        var conta = Conta.Criar(email).Value!;
        conta.Ativar();
        _contaRepo.ObterPorEmailAsync(Arg.Any<Email>()).Returns(conta);
        _tokenRepo.CriarAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(Guid.NewGuid());
        _emailService.EnviarRedefinicaoSenhaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromException(new Exception("SMTP down")));

        var result = await CriarHandler().HandleAsync(new RecuperarSenhaCommand("user@test.com"));

        result.IsSuccess.Should().BeTrue(); // Falha silenciosa — não expõe infra
    }
}

public sealed class RedefinirSenhaHandlerTests
{
    private readonly IContaRepository _contaRepo = Substitute.For<IContaRepository>();
    private readonly ITokenConfirmacaoRepository _tokenRepo = Substitute.For<ITokenConfirmacaoRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepo = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();

    private RedefinirSenhaHandler CriarHandler() =>
        new(_contaRepo, _tokenRepo, _refreshTokenRepo, _hasher);

    private static TokenConfirmacaoData TokenValido(Guid? contaId = null) =>
        new(Guid.NewGuid(), contaId ?? Guid.NewGuid(),
            "redefinicao_senha", "token-valido",
            DateTime.UtcNow.AddMinutes(9), Usado: false, DateTime.UtcNow);

    // ── Token inválido ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_TokenNaoEncontrado_RetornaFalha()
    {
        _tokenRepo.ObterPorTokenAsync(Arg.Any<string>()).Returns((TokenConfirmacaoData?)null);

        var result = await CriarHandler().HandleAsync(new RedefinirSenhaCommand("invalido", "Senha123"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("inválido");
    }

    [Fact]
    public async Task Handle_TokenExpirado_RetornaFalha()
    {
        var tokenExpirado = new TokenConfirmacaoData(
            Guid.NewGuid(), Guid.NewGuid(), "redefinicao_senha", "token",
            DateTime.UtcNow.AddMinutes(-1), false, DateTime.UtcNow);
        _tokenRepo.ObterPorTokenAsync(Arg.Any<string>()).Returns(tokenExpirado);

        var result = await CriarHandler().HandleAsync(new RedefinirSenhaCommand("token", "Senha123"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("expirou");
    }

    [Fact]
    public async Task Handle_TokenJaUsado_RetornaFalha()
    {
        var tokenUsado = new TokenConfirmacaoData(
            Guid.NewGuid(), Guid.NewGuid(), "redefinicao_senha", "token",
            DateTime.UtcNow.AddMinutes(9), Usado: true, DateTime.UtcNow);
        _tokenRepo.ObterPorTokenAsync(Arg.Any<string>()).Returns(tokenUsado);

        var result = await CriarHandler().HandleAsync(new RedefinirSenhaCommand("token", "Senha123"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("já foi utilizado");
    }

    [Fact]
    public async Task Handle_TokenTipoErrado_RetornaFalha()
    {
        var tokenTipoErrado = new TokenConfirmacaoData(
            Guid.NewGuid(), Guid.NewGuid(), "confirmacao_email", "token",
            DateTime.UtcNow.AddMinutes(9), false, DateTime.UtcNow);
        _tokenRepo.ObterPorTokenAsync(Arg.Any<string>()).Returns(tokenTipoErrado);

        var result = await CriarHandler().HandleAsync(new RedefinirSenhaCommand("token", "Senha123"));

        result.IsFailure.Should().BeTrue();
    }

    // ── Senha fraca ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("curta")]
    [InlineData("semmaius123")]
    [InlineData("SEMMENUS123")]
    [InlineData("SemNumeros")]
    public async Task Handle_SenhaFraca_RetornaFalha(string senhaFraca)
    {
        _tokenRepo.ObterPorTokenAsync(Arg.Any<string>()).Returns(TokenValido());

        var result = await CriarHandler().HandleAsync(new RedefinirSenhaCommand("token-valido", senhaFraca));

        result.IsFailure.Should().BeTrue();
    }

    // ── Redefinição bem-sucedida ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_TokenValido_AtualizaSenhaERevogarTokens()
    {
        var contaId = Guid.NewGuid();
        var token = TokenValido(contaId);
        _tokenRepo.ObterPorTokenAsync(Arg.Any<string>()).Returns(token);

        var email = Email.Create("user@test.com").Value!;
        var conta = Conta.Criar(email, "hash_antigo").Value!;
        conta.Ativar();
        _contaRepo.ObterPorIdAsync(contaId).Returns(conta);
        _hasher.Hash(Arg.Any<string>()).Returns("hash_novo");

        var result = await CriarHandler().HandleAsync(new RedefinirSenhaCommand("token-valido", "NovaSenha123"));

        result.IsSuccess.Should().BeTrue();
        await _contaRepo.Received(1).AtualizarAsync(Arg.Any<Domain.Entities.Conta>());
        await _tokenRepo.Received(1).MarcarComoUsadoAsync(token.Id, Arg.Any<CancellationToken>());
        await _refreshTokenRepo.Received(1).RevogarTodosPorContaAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
