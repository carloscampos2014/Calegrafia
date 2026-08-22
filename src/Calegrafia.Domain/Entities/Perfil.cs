using Calegrafia.Domain.Common;

namespace Calegrafia.Domain.Entities;

public sealed class Perfil
{
    public Guid Id { get; private set; }
    public Guid ContaId { get; private set; }
    public string Nome { get; private set; }
    public string? AvatarUrl { get; private set; }
    public bool IsInfantil { get; private set; }
    public bool UsaLibras { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }

    private Perfil() { Nome = string.Empty; } // Dapper

    private Perfil(Guid id, Guid contaId, string nome, bool isInfantil, bool usaLibras)
    {
        Id = id;
        ContaId = contaId;
        Nome = nome;
        IsInfantil = isInfantil;
        UsaLibras = usaLibras;
        CriadoEm = DateTime.UtcNow;
        AtualizadoEm = DateTime.UtcNow;
    }

    public static Result<Perfil> Criar(Guid contaId, string nome, bool isInfantil = false, bool usaLibras = false)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return Result<Perfil>.Failure("Nome do perfil não pode ser vazio.");

        if (nome.Length > 100)
            return Result<Perfil>.Failure("Nome do perfil não pode ter mais de 100 caracteres.");

        return Result<Perfil>.Success(new Perfil(Guid.NewGuid(), contaId, nome.Trim(), isInfantil, usaLibras));
    }

    public Result Editar(string nome, bool isInfantil, bool usaLibras, string? avatarUrl = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure("Nome do perfil não pode ser vazio.");

        if (nome.Length > 100)
            return Result.Failure("Nome do perfil não pode ter mais de 100 caracteres.");

        Nome = nome.Trim();
        IsInfantil = isInfantil;
        UsaLibras = usaLibras;
        AvatarUrl = avatarUrl;
        AtualizadoEm = DateTime.UtcNow;
        return Result.Success();
    }
}
