using Calegrafia.Application.Perfis.Commands;
using Calegrafia.Application.Perfis.Handlers;
using Calegrafia.Application.Perfis.Queries;
using Calegrafia.Domain.Entities;
using Calegrafia.Domain.Interfaces;
using Calegrafia.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Calegrafia.Application.Tests.Perfis;

// ── CriarPerfilHandler ────────────────────────────────────────────────────────

public sealed class CriarPerfilHandlerTests
{
    private readonly IPerfilRepository _perfilRepo = Substitute.For<IPerfilRepository>();
    private readonly IConsentimentoRepository _consentimentoRepo = Substitute.For<IConsentimentoRepository>();

    private CriarPerfilHandler CriarHandler() => new(_perfilRepo, _consentimentoRepo);

    private static CriarPerfilCommand ComandoValido(
        bool isInfantil = false, bool consentimentoParental = false) =>
        new(Guid.NewGuid(), "Carlos", isInfantil, false, consentimentoParental, "1.0");

    [Fact]
    public async Task Handle_LimiteDe5Perfis_RetornaFalha()
    {
        _perfilRepo.ContarPorContaAsync(Arg.Any<Guid>()).Returns(5);

        var result = await CriarHandler().HandleAsync(ComandoValido());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Limite");
    }

    [Fact]
    public async Task Handle_PerfilInfantilSemConsentimento_RetornaFalha()
    {
        _perfilRepo.ContarPorContaAsync(Arg.Any<Guid>()).Returns(2);

        var result = await CriarHandler().HandleAsync(
            ComandoValido(isInfantil: true, consentimentoParental: false));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("consentimento parental");
    }

    [Fact]
    public async Task Handle_PerfilValido_CriarERetornarId()
    {
        var contaId = Guid.NewGuid();
        var perfilId = Guid.NewGuid();
        _perfilRepo.ContarPorContaAsync(Arg.Any<Guid>()).Returns(2);
        _perfilRepo.CriarAsync(Arg.Any<Perfil>()).Returns(perfilId);

        var result = await CriarHandler().HandleAsync(
            new CriarPerfilCommand(contaId, "Carlos"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(perfilId);
        result.Value.Nome.Should().Be("Carlos");
    }

    [Fact]
    public async Task Handle_PerfilInfantilComConsentimento_RegistrarConsentimentoParental()
    {
        var contaId = Guid.NewGuid();
        _perfilRepo.ContarPorContaAsync(Arg.Any<Guid>()).Returns(1);
        _perfilRepo.CriarAsync(Arg.Any<Perfil>()).Returns(Guid.NewGuid());

        await CriarHandler().HandleAsync(
            new CriarPerfilCommand(contaId, "Sofia", IsInfantil: true,
                ConsentimentoParentalAceito: true));

        await _consentimentoRepo.Received(1).RegistrarAsync(
            contaId, "consentimento_parental", Arg.Any<string>(), true,
            Arg.Any<string?>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task Handle_PerfilNaoInfantil_NaoRegistrarConsentimentoParental()
    {
        _perfilRepo.ContarPorContaAsync(Arg.Any<Guid>()).Returns(1);
        _perfilRepo.CriarAsync(Arg.Any<Perfil>()).Returns(Guid.NewGuid());

        await CriarHandler().HandleAsync(ComandoValido(isInfantil: false));

        await _consentimentoRepo.DidNotReceive().RegistrarAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(),
            Arg.Any<string?>(), Arg.Any<string?>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_NomeVazio_RetornaFalha(string nomeVazio)
    {
        _perfilRepo.ContarPorContaAsync(Arg.Any<Guid>()).Returns(0);

        var result = await CriarHandler().HandleAsync(
            new CriarPerfilCommand(Guid.NewGuid(), nomeVazio));

        result.IsFailure.Should().BeTrue();
    }
}

// ── ListarPerfisHandler ───────────────────────────────────────────────────────

public sealed class ListarPerfisHandlerTests
{
    private readonly IPerfilRepository _perfilRepo = Substitute.For<IPerfilRepository>();

    private ListarPerfisHandler CriarHandler() => new(_perfilRepo);

    [Fact]
    public async Task Handle_ContaSemPerfis_RetornaListaVazia()
    {
        _perfilRepo.ListarPorContaAsync(Arg.Any<Guid>())
            .Returns(Array.Empty<Perfil>().ToList().AsReadOnly() as IReadOnlyList<Perfil>);

        var result = await CriarHandler().HandleAsync(new ListarPerfisQuery(Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ContaComPerfis_RetornaListaMapeada()
    {
        var contaId = Guid.NewGuid();
        var email = Email.Create("user@test.com").Value!;
        var perfis = new List<Perfil>
        {
            Perfil.Criar(contaId, "Carlos").Value!,
            Perfil.Criar(contaId, "Sofia", isInfantil: true).Value!,
        }.AsReadOnly();

        _perfilRepo.ListarPorContaAsync(contaId).Returns(perfis);

        var result = await CriarHandler().HandleAsync(new ListarPerfisQuery(contaId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Nome.Should().Be("Carlos");
        result.Value[1].IsInfantil.Should().BeTrue();
    }
}

// ── EditarPerfilHandler ───────────────────────────────────────────────────────

public sealed class EditarPerfilHandlerTests
{
    private readonly IPerfilRepository _perfilRepo = Substitute.For<IPerfilRepository>();

    private EditarPerfilHandler CriarHandler() => new(_perfilRepo);

    [Fact]
    public async Task Handle_PerfilNaoEncontrado_RetornaFalha()
    {
        _perfilRepo.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Perfil?)null);

        var result = await CriarHandler().HandleAsync(
            new EditarPerfilCommand(Guid.NewGuid(), Guid.NewGuid(), "Novo", false, false));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("não encontrado");
    }

    [Fact]
    public async Task Handle_PerfilDeOutraConta_RetornaFalha()
    {
        var contaId = Guid.NewGuid();
        var outraContaId = Guid.NewGuid();
        var perfil = Perfil.Criar(contaId, "Carlos").Value!;
        _perfilRepo.ObterPorIdAsync(Arg.Any<Guid>()).Returns(perfil);

        var result = await CriarHandler().HandleAsync(
            new EditarPerfilCommand(perfil.Id, outraContaId, "Novo", false, false));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("permissão");
    }

    [Fact]
    public async Task Handle_DadosValidos_EditarEPersistir()
    {
        var contaId = Guid.NewGuid();
        var perfil = Perfil.Criar(contaId, "Carlos").Value!;
        _perfilRepo.ObterPorIdAsync(Arg.Any<Guid>()).Returns(perfil);

        var result = await CriarHandler().HandleAsync(
            new EditarPerfilCommand(perfil.Id, contaId, "Carlos Editado", true, true));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Nome.Should().Be("Carlos Editado");
        result.Value.IsInfantil.Should().BeTrue();
        result.Value.UsaLibras.Should().BeTrue();
        await _perfilRepo.Received(1).AtualizarAsync(Arg.Any<Perfil>());
    }
}

// ── ExcluirPerfilHandler ──────────────────────────────────────────────────────

public sealed class ExcluirPerfilHandlerTests
{
    private readonly IPerfilRepository _perfilRepo = Substitute.For<IPerfilRepository>();

    private ExcluirPerfilHandler CriarHandler() => new(_perfilRepo);

    [Fact]
    public async Task Handle_PerfilNaoEncontrado_RetornaFalha()
    {
        _perfilRepo.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Perfil?)null);

        var result = await CriarHandler().HandleAsync(
            new ExcluirPerfilCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PerfilDeOutraConta_RetornaFalha()
    {
        var contaId = Guid.NewGuid();
        var outraContaId = Guid.NewGuid();
        var perfil = Perfil.Criar(contaId, "Carlos").Value!;
        _perfilRepo.ObterPorIdAsync(Arg.Any<Guid>()).Returns(perfil);

        var result = await CriarHandler().HandleAsync(
            new ExcluirPerfilCommand(perfil.Id, outraContaId));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("permissão");
    }

    [Fact]
    public async Task Handle_PerfilValido_ExcluirERetornarSucesso()
    {
        var contaId = Guid.NewGuid();
        var perfil = Perfil.Criar(contaId, "Carlos").Value!;
        _perfilRepo.ObterPorIdAsync(Arg.Any<Guid>()).Returns(perfil);

        var result = await CriarHandler().HandleAsync(
            new ExcluirPerfilCommand(perfil.Id, contaId));

        result.IsSuccess.Should().BeTrue();
        await _perfilRepo.Received(1).ExcluirAsync(perfil.Id, Arg.Any<CancellationToken>());
    }
}
