using Calegrafia.Domain.Common;

namespace Calegrafia.Application.Auth.Commands;

public sealed record CadastrarContaCommand(
    string Email,
    string Senha,
    bool AceitouTermos,
    bool AceitouPoliticaPrivacidade,
    string VersaoTermos,
    string? IpOrigem = null,
    string? UserAgent = null);

public sealed record CadastrarContaResult(Guid ContaId, string Email);
