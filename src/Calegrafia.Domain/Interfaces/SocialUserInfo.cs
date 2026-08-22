namespace Calegrafia.Domain.Interfaces;

public sealed record SocialUserInfo(
    string SubjectId,  // ID único do usuário no provedor
    string Email,
    string? Nome);
