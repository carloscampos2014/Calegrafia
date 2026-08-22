namespace Calegrafia.Domain.Interfaces;

public sealed record TokenConfirmacaoData(
    Guid Id,
    Guid ContaId,
    string Tipo,
    string Token,
    DateTime ExpiraEm,
    bool Usado,
    DateTime CriadoEm)
{
    public bool EstaExpirado() => DateTime.UtcNow > ExpiraEm;
    public bool EstaValido() => !Usado && !EstaExpirado();
}
