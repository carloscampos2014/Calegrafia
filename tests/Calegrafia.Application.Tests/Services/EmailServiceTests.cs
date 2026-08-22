using Calegrafia.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReceivedExtensions;

namespace Calegrafia.Application.Tests.Services;

/// <summary>
/// Testes do contrato IEmailService via fake (NSubstitute).
/// Testes de integração com SMTP real (Mailpit) ficam em testes de integração separados.
/// </summary>
public sealed class EmailServiceTests
{
    private readonly IEmailService _sut = Substitute.For<IEmailService>();

    // ── EnviarConfirmacaoCadastroAsync ────────────────────────────────────────

    [Fact]
    public async Task EnviarConfirmacaoCadastro_ChamadoComParametrosCorretos_ExecutaSemErro()
    {
        await _sut.EnviarConfirmacaoCadastroAsync(
            "user@test.com", "Carlos", "https://app.calegrafia.com/confirmar?token=abc");

        await _sut.Received(1).EnviarConfirmacaoCadastroAsync(
            "user@test.com", "Carlos", "https://app.calegrafia.com/confirmar?token=abc");
    }

    [Fact]
    public async Task EnviarConfirmacaoCadastro_CancellationToken_EPassadoParaServico()
    {
        var cts = new CancellationTokenSource();

        await _sut.EnviarConfirmacaoCadastroAsync(
            "user@test.com", "Carlos", "https://link", cts.Token);

        await _sut.Received(1).EnviarConfirmacaoCadastroAsync(
            "user@test.com", "Carlos", "https://link", cts.Token);
    }

    // ── EnviarRedefinicaoSenhaAsync ───────────────────────────────────────────

    [Fact]
    public async Task EnviarRedefinicaoSenha_ChamadoComParametrosCorretos_ExecutaSemErro()
    {
        await _sut.EnviarRedefinicaoSenhaAsync(
            "user@test.com", "Carlos", "https://app.calegrafia.com/reset?token=xyz");

        await _sut.Received(1).EnviarRedefinicaoSenhaAsync(
            "user@test.com", "Carlos", Arg.Any<string>());
    }

    // ── EnviarResetMfaAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task EnviarResetMfa_ChamadoUmaVez_RecebidoUmaVez()
    {
        await _sut.EnviarResetMfaAsync("user@test.com", "Carlos", "https://link/reset-mfa");

        await _sut.Received(1).EnviarResetMfaAsync(
            "user@test.com", "Carlos", Arg.Any<string>());
    }

    // ── EnviarExportacaoDadosAsync ────────────────────────────────────────────

    [Fact]
    public async Task EnviarExportacaoDados_ComArquivoJson_ChamadoCorretamente()
    {
        var dadosJson = "{\"id\":\"123\",\"email\":\"user@test.com\"}"u8.ToArray();

        await _sut.EnviarExportacaoDadosAsync(
            "user@test.com", "Carlos", dadosJson, "dados-carlos.json");

        await _sut.Received(1).EnviarExportacaoDadosAsync(
            "user@test.com", "Carlos", dadosJson, "dados-carlos.json");
    }

    [Fact]
    public async Task EnviarExportacaoDados_NaoChama_OutroMetodo()
    {
        var dadosJson = new byte[] { 1, 2, 3 };

        await _sut.EnviarExportacaoDadosAsync("a@b.com", "Teste", dadosJson, "dados.json");

        // Garante que outros métodos não foram chamados acidentalmente
        await _sut.DidNotReceive().EnviarConfirmacaoCadastroAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        await _sut.DidNotReceive().EnviarRedefinicaoSenhaAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // ── Contrato geral ────────────────────────────────────────────────────────

    [Fact]
    public void Interface_TemQuatroMetodos()
    {
        typeof(IEmailService).GetMethods().Should().HaveCount(4);
    }

    [Fact]
    public void TodosOsMetodos_RetornamTask()
    {
        var metodos = typeof(IEmailService).GetMethods();
        metodos.Should().AllSatisfy(m =>
            m.ReturnType.Should().Be(typeof(Task),
                because: $"{m.Name} deve retornar Task"));
    }
}
