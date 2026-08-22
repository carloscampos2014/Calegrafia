namespace Calegrafia.Domain.Interfaces;

public sealed record TokenPayload(
    Guid ContaId,
    string Email,
    Guid? PerfilId,
    DateTime ExpiraEm);
