using Dapper;
using Npgsql;

namespace Calegrafia.Infrastructure.Repositories;

public sealed class RefreshTokenRepository
{
    private readonly string _connectionString;

    public RefreshTokenRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<Guid> CriarAsync(Guid contaId, string token, DateTime expiraEm, string? dispositivo = null, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO refresh_tokens (id, conta_id, token, expira_em, revogado, dispositivo, criado_em)
            VALUES (@Id, @ContaId, @Token, @ExpiraEm, FALSE, @Dispositivo, NOW())
            RETURNING id
            """;

        await using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<Guid>(new CommandDefinition(sql,
            new { Id = Guid.NewGuid(), ContaId = contaId, Token = token, ExpiraEm = expiraEm, Dispositivo = dispositivo },
            cancellationToken: ct));
    }

    public async Task<RefreshTokenRow?> ObterPorTokenAsync(string token, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, conta_id, token, expira_em, revogado, dispositivo, criado_em
            FROM refresh_tokens
            WHERE token = @Token
            """;

        await using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<RefreshTokenRow>(new CommandDefinition(sql, new { Token = token }, cancellationToken: ct));
    }

    public async Task RevogarAsync(string token, CancellationToken ct = default)
    {
        const string sql = "UPDATE refresh_tokens SET revogado = TRUE WHERE token = @Token";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { Token = token }, cancellationToken: ct));
    }

    public async Task RevogarTodosPorContaAsync(Guid contaId, CancellationToken ct = default)
    {
        const string sql = "UPDATE refresh_tokens SET revogado = TRUE WHERE conta_id = @ContaId";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { ContaId = contaId }, cancellationToken: ct));
    }

    public sealed record RefreshTokenRow(
        Guid Id,
        Guid ContaId,
        string Token,
        DateTime ExpiraEm,
        bool Revogado,
        string? Dispositivo,
        DateTime CriadoEm);
}
