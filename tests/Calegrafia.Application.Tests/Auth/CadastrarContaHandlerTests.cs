using Calegrafia.Application.Auth.Commands;
using Calegrafia.Application.Auth.Handlers;
using Calegrafia.Domain.Interfaces;
using Calegrafia.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Calegrafia.Application.Tests.Auth;

public sealed class CadastrarContaHandlerTests
{
    private readonly IContaRepository _contaRepo = Substitute.For<IContaRepository>();
    private readonly ITokenConfirmacaoRepository _tokenRepo = Substitute.For<ITokenConfirmacaoRepository>();
    private readonly IConsentimentoRepository _consentimentoRepo = Substitute.For<IConsentimentoRepository>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();

    private CadastrarContaHandler CriarHandler() => new(
        _contaRepo, _tokenRepo, _consentimentoRepo,
        _emailService, _hasher, "https://app.calegrafia.com");

    private static CadastrarContaCommand ComandoValido(
        string email = "user@test.com",
        string senha = "Senha123",
        bool aceitouTermos = true,
        bool aceitouPolitica = true) =>
        new(email, senha, aceitouTermos, aceitouPolitica, "1.0");

    // ── Aceite de termos (RF-12) ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_SemAceitarTermos_RetornaFalha()
    {
        var result = await CriarHandler().HandleAsync(ComandoValido(aceitouTermos: false));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Termos de Uso");
    }

    [Fact]
    public async Task Handle_SemAceitarPolitica_RetornaFalha()
    {
        var result = await CriarHandler().HandleAsync(ComandoValido(aceitouPolitica: false));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Política de Privacidade");
    }

    // ── Validação de email ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_EmailInvalido_RetornaFalha()
    {
        var result = await CriarHandler().HandleAsync(ComandoValido(email: "nao-e-email"));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_EmailDuplicado_RetornaFalha()
    {
        var emailValido = Email.Create("existente@test.com").Value!;
        _contaRepo.ExisteEmailAsync(Arg.Any<Email>()).Returns(true);

        var result = await CriarHandler().HandleAsync(ComandoValido(email: "existente@test.com"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("já está cadastrado");
    }

    // ── Validação de senha ────────────────────────────────────────────────────

    [Theory]
    [InlineData("curta")]           // menos de 8 chars
    [InlineData("semmaius123")]     // sem maiúscula
    [InlineData("SEMMENUS123")]     // sem minúscula
    [InlineData("SemNumeros")]      // sem número
    public async Task Handle_SenhaFraca_RetornaFalha(string senhaFraca)
    {
        _contaRepo.ExisteEmailAsync(Arg.Any<Email>()).Returns(false);

        var result = await CriarHandler().HandleAsync(ComandoValido(senha: senhaFraca));

        result.IsFailure.Should().BeTrue();
    }

    // ── Cadastro bem-sucedido ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_DadosValidos_CriaContaERetornaId()
    {
        var contaId = Guid.NewGuid();
        _contaRepo.ExisteEmailAsync(Arg.Any<Email>()).Returns(false);
        _contaRepo.CriarAsync(Arg.Any<Domain.Entities.Conta>()).Returns(contaId);
        _tokenRepo.CriarAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(Guid.NewGuid());
        _hasher.Hash(Arg.Any<string>()).Returns("hash_bcrypt");

        var result = await CriarHandler().HandleAsync(ComandoValido());

        result.IsSuccess.Should().BeTrue();
        result.Value!.ContaId.Should().Be(contaId);
        result.Value.Email.Should().Be("user@test.com");
    }

    [Fact]
    public async Task Handle_DadosValidos_RegistraConsentimentos()
    {
        _contaRepo.ExisteEmailAsync(Arg.Any<Email>()).Returns(false);
        _contaRepo.CriarAsync(Arg.Any<Domain.Entities.Conta>()).Returns(Guid.NewGuid());
        _tokenRepo.CriarAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(Guid.NewGuid());
        _hasher.Hash(Arg.Any<string>()).Returns("hash_bcrypt");

        await CriarHandler().HandleAsync(ComandoValido());

        // Dois consentimentos: termos_uso e politica_privacidade
        await _consentimentoRepo.Received(2).RegistrarAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), true,
            Arg.Any<string?>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task Handle_DadosValidos_EnviaEmailConfirmacao()
    {
        _contaRepo.ExisteEmailAsync(Arg.Any<Email>()).Returns(false);
        _contaRepo.CriarAsync(Arg.Any<Domain.Entities.Conta>()).Returns(Guid.NewGuid());
        _tokenRepo.CriarAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(Guid.NewGuid());
        _hasher.Hash(Arg.Any<string>()).Returns("hash_bcrypt");

        await CriarHandler().HandleAsync(ComandoValido());

        await _emailService.Received(1).EnviarConfirmacaoCadastroAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
