using Calegrafia.Domain.Interfaces;
using Dapper;
using Npgsql;

namespace Calegrafia.Infrastructure.Repositories;

public sealed class TokenConfirmacaoRepository : ITokenConfirmacaoRepository
{
    private readonly string _connectionString;

    public TokenConfirmacaoRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<Guid> CriarAsync(Guid contaId, string tipo, string token, DateTime expiraEm, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO tokens_confirmacao (id, conta_id, tipo, token, expira_em, usado, criado_em)
            VALUES (@Id, @ContaId, @Tipo, @Token, @ExpiraEm, FALSE, NOW())
            RETURNING id
            """;

        await using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<Guid>(new CommandDefinition(sql,
            new { Id = Guid.NewGuid(), ContaId = contaId, Tipo = tipo, Token = token, ExpiraEm = expiraEm },
            cancellationToken: ct));
    }

    public async Task<TokenConfirmacaoData?> ObterPorTokenAsync(string token, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, conta_id, token, tipo, expira_em, usado, criado_em
            FROM tokens_confirmacao
            WHERE token = @Token
            """;

        await using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<TokenConfirmacaoData>(new CommandDefinition(sql, new { Token = token }, cancellationToken: ct));
    }

    public async Task MarcarComoUsadoAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "UPDATE tokens_confirmacao SET usado = TRUE WHERE id = @Id";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }
}
