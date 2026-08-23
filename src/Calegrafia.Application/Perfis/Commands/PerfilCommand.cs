namespace Calegrafia.Application.Perfis.Commands;

public sealed record CriarPerfilCommand(
    Guid ContaId,
    string Nome,
    bool IsInfantil = false,
    bool UsaLibras = false,
    bool ConsentimentoParentalAceito = false,
    string VersaoTermos = "1.0",
    string? IpOrigem = null,
    string? UserAgent = null);

public sealed record EditarPerfilCommand(
    Guid PerfilId,
    Guid ContaId,
    string Nome,
    bool IsInfantil,
    bool UsaLibras,
    string? AvatarUrl = null);

public sealed record ExcluirPerfilCommand(Guid PerfilId, Guid ContaId);

public sealed record PerfilResult(
    Guid Id,
    Guid ContaId,
    string Nome,
    string? AvatarUrl,
    bool IsInfantil,
    bool UsaLibras);
