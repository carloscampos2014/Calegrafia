namespace Calegrafia.Domain.Interfaces;

public sealed record RefreshTokenData(
    Guid Id,
    Guid ContaId,
    string Token,
    DateTime ExpiraEm,
    bool Revogado,
    string? Dispositivo,
    DateTime CriadoEm)
{
    public bool EstaExpirado() => DateTime.UtcNow > ExpiraEm;
    public bool EstaValido() => !Revogado && !EstaExpirado();
}
