using Dapper;
using Npgsql;

namespace Calegrafia.Infrastructure.Repositories;

/// <summary>
/// Repositório imutável (LGPD) — apenas INSERT.
/// Nunca atualizar ou deletar registros de consentimento.
/// </summary>
public sealed class ConsentimentoRepository
{
    private readonly string _connectionString;

    public ConsentimentoRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<Guid> RegistrarAsync(
        Guid contaId,
        string tipo,
        string versao,
        bool aceito,
        string? ipOrigem = null,
        string? userAgent = null,
        CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO consentimentos (id, conta_id, tipo, versao, aceito, ip_origem, user_agent, criado_em)
            VALUES (@Id, @ContaId, @Tipo, @Versao, @Aceito, @IpOrigem::inet, @UserAgent, NOW())
            RETURNING id
            """;

        await using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<Guid>(new CommandDefinition(sql,
            new { Id = Guid.NewGuid(), ContaId = contaId, Tipo = tipo, Versao = versao, Aceito = aceito, IpOrigem = ipOrigem, UserAgent = userAgent },
            cancellationToken: ct));
    }
}
