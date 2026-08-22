using System.Security.Cryptography;
using Calegrafia.Infrastructure.Services;
using FluentAssertions;

namespace Calegrafia.Application.Tests.Services;

/// <summary>Testes unitários para JwtService — geração e validação de tokens RS256.</summary>
public sealed class JwtServiceTests : IDisposable
{
    private readonly JwtService _sut;
    private readonly RSA _rsaPrivate;
    private readonly RSA _rsaPublic;

    public JwtServiceTests()
    {
        _rsaPrivate = RSA.Create(2048);
        _rsaPublic = RSA.Create();
        _rsaPublic.ImportRSAPublicKey(_rsaPrivate.ExportRSAPublicKey(), out _);

        var privateKeyPem = _rsaPrivate.ExportRSAPrivateKeyPem();
        var publicKeyPem = _rsaPrivate.ExportSubjectPublicKeyInfoPem();

        _sut = new JwtService(privateKeyPem, publicKeyPem, issuer: "calegrafia-test", audience: "calegrafia-app");
    }

    // ── GerarAccessToken ─────────────────────────────────────────────────────

    [Fact]
    public void GerarAccessToken_RetornaStringNaoVazia()
    {
        var token = _sut.GerarAccessToken(Guid.NewGuid(), "user@test.com");
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GerarAccessToken_TokenEhJwtValido()
    {
        var token = _sut.GerarAccessToken(Guid.NewGuid(), "user@test.com");
        token.Split('.').Should().HaveCount(3, "JWT deve ter header.payload.signature");
    }

    [Fact]
    public void GerarAccessToken_ComPerfilId_IncluiClaimPerfilId()
    {
        var contaId = Guid.NewGuid();
        var perfilId = Guid.NewGuid();

        var token = _sut.GerarAccessToken(contaId, "user@test.com", perfilId);
        var payload = _sut.ValidarAccessToken(token);

        payload.Should().NotBeNull();
        payload!.PerfilId.Should().Be(perfilId);
    }

    [Fact]
    public void GerarAccessToken_SemPerfilId_PerfilIdEhNull()
    {
        var token = _sut.GerarAccessToken(Guid.NewGuid(), "user@test.com");
        var payload = _sut.ValidarAccessToken(token);

        payload!.PerfilId.Should().BeNull();
    }

    // ── GerarRefreshToken ────────────────────────────────────────────────────

    [Fact]
    public void GerarRefreshToken_RetornaTokenNaoVazio()
    {
        var (token, _) = _sut.GerarRefreshToken();
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GerarRefreshToken_ExpiraEmAproximadamente30Dias()
    {
        var (_, expiraEm) = _sut.GerarRefreshToken();
        expiraEm.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GerarRefreshToken_DoisTokensSaoDiferentes()
    {
        var (token1, _) = _sut.GerarRefreshToken();
        var (token2, _) = _sut.GerarRefreshToken();
        token1.Should().NotBe(token2);
    }

    // ── ValidarAccessToken ───────────────────────────────────────────────────

    [Fact]
    public void ValidarAccessToken_TokenValido_RetornaPayload()
    {
        var contaId = Guid.NewGuid();
        var email = "user@test.com";

        var token = _sut.GerarAccessToken(contaId, email);
        var payload = _sut.ValidarAccessToken(token);

        payload.Should().NotBeNull();
        payload!.ContaId.Should().Be(contaId);
        payload.Email.Should().Be(email);
    }

    [Fact]
    public void ValidarAccessToken_TokenInvalido_RetornaNull()
    {
        var payload = _sut.ValidarAccessToken("token.invalido.assinatura");
        payload.Should().BeNull();
    }

    [Fact]
    public void ValidarAccessToken_TokenVazio_RetornaNull()
    {
        var payload = _sut.ValidarAccessToken(string.Empty);
        payload.Should().BeNull();
    }

    [Fact]
    public void ValidarAccessToken_TokenTamperado_RetornaNull()
    {
        var token = _sut.GerarAccessToken(Guid.NewGuid(), "user@test.com");
        var parts = token.Split('.');
        var tampered = $"{parts[0]}.{parts[1]}TAMPERED.{parts[2]}";

        var payload = _sut.ValidarAccessToken(tampered);
        payload.Should().BeNull();
    }

    public void Dispose()
    {
        _rsaPrivate.Dispose();
        _rsaPublic.Dispose();
    }
}
