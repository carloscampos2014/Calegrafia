using Calegrafia.Application.GestaoContas.Commands;
using Calegrafia.Application.GestaoContas.Handlers;
using Calegrafia.Domain.Entities;
using Calegrafia.Domain.Interfaces;
using Calegrafia.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Calegrafia.Application.Tests.Conta;

// ── ExportarDadosHandler ──────────────────────────────────────────────────────

public sealed class ExportarDadosHandlerTests
{
    private readonly IContaRepository _contaRepo = Substitute.For<IContaRepository>();
    private readonly IPerfilRepository _perfilRepo = Substitute.For<IPerfilRepository>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();

    private ExportarDadosHandler CriarHandler() =>
        new(_contaRepo, _perfilRepo, _emailService);

    private Domain.Entities.Conta CriarContaAtiva()
    {
        var email = Email.Create("user@test.com").Value!;
        var conta = Domain.Entities.Conta.Criar(email, "hash").Value!;
        conta.Ativar();
        return conta;
    }

    [Fact]
    public async Task Handle_ContaNaoEncontrada_RetornaFalha()
    {
        _contaRepo.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Domain.Entities.Conta?)null);

        var result = await CriarHandler().HandleAsync(new ExportarDadosCommand(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("não encontrada");
    }

    [Fact]
    public async Task Handle_ContaValida_RetornaSucessoImediatamente()
    {
        var conta = CriarContaAtiva();
        _contaRepo.ObterPorIdAsync(Arg.Any<Guid>()).Returns(conta);
        _perfilRepo.ListarPorContaAsync(Arg.Any<Guid>())
            .Returns(new List<Perfil>().AsReadOnly() as IReadOnlyList<Perfil>);

        var result = await CriarHandler().HandleAsync(new ExportarDadosCommand(conta.Id));

        // Deve retornar sucesso imediatamente (202) sem esperar o envio de email
        result.IsSuccess.Should().BeTrue();
    }
}

// ── ExcluirContaHandler ───────────────────────────────────────────────────────

public sealed class ExcluirContaHandlerTests
{
    private readonly IContaRepository _contaRepo = Substitute.For<IContaRepository>();
    private readonly IPerfilRepository _perfilRepo = Substitute.For<IPerfilRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepo = Substitute.For<IRefreshTokenRepository>();
    private readonly ILogAutenticacaoRepository _logRepo = Substitute.For<ILogAutenticacaoRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();

    private ExcluirContaHandler CriarHandler() =>
        new(_contaRepo, _perfilRepo, _refreshTokenRepo, _logRepo, _hasher);

    private Domain.Entities.Conta CriarContaAtiva()
    {
        var email = Email.Create("user@test.com").Value!;
        var conta = Domain.Entities.Conta.Criar(email, "hash_bcrypt").Value!;
        conta.Ativar();
        return conta;
    }

    [Fact]
    public async Task Handle_ContaNaoEncontrada_RetornaFalha()
    {
        _contaRepo.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Domain.Entities.Conta?)null);

        var result = await CriarHandler().HandleAsync(
            new ExcluirContaCommand(Guid.NewGuid(), "Senha123"));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SenhaIncorreta_RetornaFalha()
    {
        var conta = CriarContaAtiva();
        _contaRepo.ObterPorIdAsync(Arg.Any<Guid>()).Returns(conta);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var result = await CriarHandler().HandleAsync(
            new ExcluirContaCommand(conta.Id, "SenhaErrada"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Senha incorreta");
    }

    [Fact]
    public async Task Handle_SenhaCorreta_RevogarTokensEAnonimizarLogs()
    {
        var conta = CriarContaAtiva();
        _contaRepo.ObterPorIdAsync(Arg.Any<Guid>()).Returns(conta);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _perfilRepo.ListarPorContaAsync(Arg.Any<Guid>())
            .Returns(new List<Perfil>().AsReadOnly() as IReadOnlyList<Perfil>);

        var result = await CriarHandler().HandleAsync(
            new ExcluirContaCommand(conta.Id, "Senha123"));

        result.IsSuccess.Should().BeTrue();
        await _refreshTokenRepo.Received(1).RevogarTodosPorContaAsync(
            conta.Id, Arg.Any<CancellationToken>());
        await _logRepo.Received(1).AnonymizarPorContaAsync(
            conta.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SenhaCorreta_ExcluirPerfisAssociados()
    {
        var conta = CriarContaAtiva();
        _contaRepo.ObterPorIdAsync(Arg.Any<Guid>()).Returns(conta);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var perfis = new List<Perfil>
        {
            Perfil.Criar(conta.Id, "Carlos").Value!,
            Perfil.Criar(conta.Id, "Sofia").Value!,
        }.AsReadOnly() as IReadOnlyList<Perfil>;
        _perfilRepo.ListarPorContaAsync(Arg.Any<Guid>()).Returns(perfis);

        await CriarHandler().HandleAsync(new ExcluirContaCommand(conta.Id, "Senha123"));

        await _perfilRepo.Received(2).ExcluirAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}


