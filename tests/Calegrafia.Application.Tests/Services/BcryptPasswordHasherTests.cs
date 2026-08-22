using Calegrafia.Infrastructure.Services;
using FluentAssertions;

namespace Calegrafia.Application.Tests.Services;

public sealed class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _sut = new();

    [Fact]
    public void Hash_RetornaStringDiferenteDaSenhaOriginal()
    {
        var hash = _sut.Hash("Senha123");
        hash.Should().NotBe("Senha123");
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Hash_MesmaSenha_ProduzeHashesDiferentes()
    {
        // BCrypt inclui salt aleatório — dois hashes da mesma senha são diferentes
        var h1 = _sut.Hash("Senha123");
        var h2 = _sut.Hash("Senha123");
        h1.Should().NotBe(h2);
    }

    [Fact]
    public void Verify_SenhaCorreta_RetornaTrue()
    {
        var hash = _sut.Hash("Senha123");
        _sut.Verify("Senha123", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_SenhaErrada_RetornaFalse()
    {
        var hash = _sut.Hash("Senha123");
        _sut.Verify("SenhaErrada", hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_HashVazio_RetornaFalse()
    {
        _sut.Verify("Senha123", "$2a$12$invalido").Should().BeFalse();
    }
}
