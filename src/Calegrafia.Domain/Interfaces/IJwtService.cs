namespace Calegrafia.Domain.Interfaces;

public interface IJwtService
{
    /// <summary>Gera access token RS256 com expiração de 15 minutos.</summary>
    string GerarAccessToken(Guid contaId, string email, Guid? perfilId = null);

    /// <summary>Gera refresh token opaco (string aleatória segura) com expiração de 30 dias.</summary>
    (string Token, DateTime ExpiraEm) GerarRefreshToken();

    /// <summary>Valida e decodifica um access token. Retorna null se inválido ou expirado.</summary>
    TokenPayload? ValidarAccessToken(string token);
}
