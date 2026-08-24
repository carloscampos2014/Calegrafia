using Calegrafia.Domain.Interfaces;
using Dapper;
using Npgsql;

namespace Calegrafia.Infrastructure.Repositories;

public sealed class ProvedorSocialRepository : IProvedorSocialRepository
{
    private readonly string _connectionString;

    public ProvedorSocialRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<bool> ExisteAsync(string provedor, string subjectId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(1) FROM provedores_sociais
            WHERE provedor = @Provedor AND subject_id = @SubjectId
            """;
        await using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { Provedor = provedor, SubjectId = subjectId }, cancellationToken: ct)) > 0;
    }

    public async Task VincularSeNaoExistirAsync(Guid contaId, string provedor, string subjectId, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO provedores_sociais (id, conta_id, provedor, subject_id, criado_em)
            VALUES (@Id, @ContaId, @Provedor, @SubjectId, NOW())
            ON CONFLICT (provedor, subject_id) DO NOTHING
            """;
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql,
            new { Id = Guid.NewGuid(), ContaId = contaId, Provedor = provedor, SubjectId = subjectId },
            cancellationToken: ct));
    }
}
