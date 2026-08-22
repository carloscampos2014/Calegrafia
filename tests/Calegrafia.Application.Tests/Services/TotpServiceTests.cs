using Calegrafia.Infrastructure.Services;
using FluentAssertions;

namespace Calegrafia.Application.Tests.Services;

/// <summary>Testes unitários para TotpService — geração, validação TOTP e criptografia AES-256.</summary>
public sealed class TotpServiceTests
{
    // Chave AES-256 de 32 bytes para testes
    private static readonly string _testKey = Convert.ToBase64String(new byte[32] {
        1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,
        17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32
    });

    private readonly TotpService _sut = new(_testKey);

    // ── GerarSecret ──────────────────────────────────────────────────────────

    [Fact]
    public void GerarSecret_RetornaStringNaoVazia()
    {
        var secret = _sut.GerarSecret();
        secret.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GerarSecret_EhBase32Valido()
    {
        var secret = _sut.GerarSecret();
        // Base32 usa apenas A-Z e 2-7
        secret.Should().MatchRegex("^[A-Z2-7]+=*$");
    }

    [Fact]
    public void GerarSecret_DoisSecretsSaoDiferentes()
    {
        var s1 = _sut.GerarSecret();
        var s2 = _sut.GerarSecret();
        s1.Should().NotBe(s2);
    }

    // ── GerarQrCodeUri ────────────────────────────────────────────────────────

    [Fact]
    public void GerarQrCodeUri_RetornaUriOtpauth()
    {
        var secret = _sut.GerarSecret();
        var uri = _sut.GerarQrCodeUri(secret, "user@test.com");
        uri.Should().StartWith("otpauth://totp/");
    }

    [Fact]
    public void GerarQrCodeUri_ContemSecretEEmail()
    {
        var secret = _sut.GerarSecret();
        var uri = _sut.GerarQrCodeUri(secret, "user@test.com", "Calegrafia");
        uri.Should().Contain(secret);
        uri.Should().Contain("Calegrafia");
    }

    [Fact]
    public void GerarQrCodeUri_EncodaCaracteresEspeciais()
    {
        var secret = _sut.GerarSecret();
        var uri = _sut.GerarQrCodeUri(secret, "user+tag@test.com", "Minha App");
        uri.Should().Contain("Minha%20App");
    }

    // ── ValidarCodigo ─────────────────────────────────────────────────────────

    [Fact]
    public void ValidarCodigo_CodigoVazio_RetornaFalse()
    {
        var secret = _sut.GerarSecret();
        _sut.ValidarCodigo(secret, "").Should().BeFalse();
    }

    [Fact]
    public void ValidarCodigo_CodigoComLetras_RetornaFalse()
    {
        var secret = _sut.GerarSecret();
        _sut.ValidarCodigo(secret, "abc123").Should().BeFalse();
    }

    [Fact]
    public void ValidarCodigo_CodigoComMenosDe6Digitos_RetornaFalse()
    {
        var secret = _sut.GerarSecret();
        _sut.ValidarCodigo(secret, "12345").Should().BeFalse();
    }

    [Fact]
    public void ValidarCodigo_CodigoErrado_RetornaFalse()
    {
        var secret = _sut.GerarSecret();
        _sut.ValidarCodigo(secret, "000000").Should().BeFalse();
    }

    [Fact]
    public void ValidarCodigo_SecretInvalido_RetornaFalse()
    {
        _sut.ValidarCodigo("SECRETINVALIDO!!!", "123456").Should().BeFalse();
    }

    // ── CriptografarSecret / DescriptografarSecret ───────────────────────────

    [Fact]
    public void CriptografarSecret_RetornaStringDiferenteDoOriginal()
    {
        var secret = _sut.GerarSecret();
        var criptografado = _sut.CriptografarSecret(secret);
        criptografado.Should().NotBe(secret);
    }

    [Fact]
    public void DescriptografarSecret_RecuperaSecretOriginal()
    {
        var secret = _sut.GerarSecret();
        var criptografado = _sut.CriptografarSecret(secret);
        var recuperado = _sut.DescriptografarSecret(criptografado);
        recuperado.Should().Be(secret);
    }

    [Fact]
    public void CriptografarSecret_DoisCifrasDoMesmoSecretSaoDiferentes()
    {
        // IV aleatório garante que mesmo input produz outputs diferentes
        var secret = _sut.GerarSecret();
        var c1 = _sut.CriptografarSecret(secret);
        var c2 = _sut.CriptografarSecret(secret);
        c1.Should().NotBe(c2);
    }

    [Fact]
    public void CriptografarDescriptografar_ComSecretLongo_FuncionaCorretamente()
    {
        const string secretLongo = "JBSWY3DPEHPK3PXP"; // 16 chars Base32
        var criptografado = _sut.CriptografarSecret(secretLongo);
        var recuperado = _sut.DescriptografarSecret(criptografado);
        recuperado.Should().Be(secretLongo);
    }

    // ── Construtor ────────────────────────────────────────────────────────────

    [Fact]
    public void Construtor_ChaveComTamanhoErrado_LancaArgumentException()
    {
        var chaveInvalida = Convert.ToBase64String(new byte[16]); // 16 bytes em vez de 32
        var act = () => new TotpService(chaveInvalida);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*32 bytes*");
    }
}
