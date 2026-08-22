using Calegrafia.Domain.Interfaces;
using BC = BCrypt.Net.BCrypt;

namespace Calegrafia.Infrastructure.Services;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string senha) =>
        BC.HashPassword(senha, WorkFactor);

    public bool Verify(string senha, string hash)
    {
        try
        {
            return BC.Verify(senha, hash);
        }
        catch
        {
            return false;
        }
    }
}
