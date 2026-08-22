using Calegrafia.Domain.Interfaces;
using Dapper;
using Npgsql;

namespace Calegrafia.Infrastructure.Repositories;

public sealed class LogAutenticacaoRepository : ILogAutenticacaoRepository
{
    private readonly string _connectionString;

    public LogAutenticacaoRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task RegistrarAsync(
        Guid? contaId,
        string emailHash,
        string evento,
        string? ipOrigem = null,
        string? userAgent = null,
        CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO logs_autenticacao (id, conta_id, email_hash, evento, ip_origem, user_agent, criado_em)
            VALUES (@Id, @ContaId, @EmailHash, @Evento, @IpOrigem::inet, @UserAgent, NOW())
            """;

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql,
            new { Id = Guid.NewGuid(), ContaId = contaId, EmailHash = emailHash, Evento = evento, IpOrigem = ipOrigem, UserAgent = userAgent },
            cancellationToken: ct));
    }

    /// <summary>
    /// Anonimiza logs de uma conta — remove conta_id, preserva email_hash para auditoria.
    /// Obrigação LGPD: logs retidos por 2 anos, mas dados pessoais removidos.
    /// </summary>
    public async Task AnonymizarPorContaAsync(Guid contaId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE logs_autenticacao
            SET conta_id = NULL
            WHERE conta_id = @ContaId
            """;

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { ContaId = contaId }, cancellationToken: ct));
    }
}
