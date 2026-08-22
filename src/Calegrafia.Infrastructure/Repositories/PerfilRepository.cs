using Calegrafia.Domain.Entities;
using Calegrafia.Domain.Interfaces;
using Dapper;
using Npgsql;

namespace Calegrafia.Infrastructure.Repositories;

public sealed class PerfilRepository : IPerfilRepository
{
    private readonly string _connectionString;

    public PerfilRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<IReadOnlyList<Perfil>> ListarPorContaAsync(Guid contaId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, conta_id, nome, avatar_url, is_infantil, usa_libras, criado_em, atualizado_em
            FROM perfis
            WHERE conta_id = @ContaId
            ORDER BY criado_em
            """;

        await using var conn = CreateConnection();
        var rows = await conn.QueryAsync<PerfilRow>(new CommandDefinition(sql, new { ContaId = contaId }, cancellationToken: ct));
        return rows.Select(r => r.ToDomain()).ToList().AsReadOnly();
    }

    public async Task<Perfil?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, conta_id, nome, avatar_url, is_infantil, usa_libras, criado_em, atualizado_em
            FROM perfis
            WHERE id = @Id
            """;

        await using var conn = CreateConnection();
        var row = await conn.QuerySingleOrDefaultAsync<PerfilRow>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
        return row?.ToDomain();
    }

    public async Task<int> ContarPorContaAsync(Guid contaId, CancellationToken ct = default)
    {
        const string sql = "SELECT COUNT(1) FROM perfis WHERE conta_id = @ContaId";
        await using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { ContaId = contaId }, cancellationToken: ct));
    }

    public async Task<Guid> CriarAsync(Perfil perfil, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO perfis (id, conta_id, nome, avatar_url, is_infantil, usa_libras, criado_em, atualizado_em)
            VALUES (@Id, @ContaId, @Nome, @AvatarUrl, @IsInfantil, @UsaLibras, @CriadoEm, @AtualizadoEm)
            """;

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, PerfilRow.FromDomain(perfil), cancellationToken: ct));
        return perfil.Id;
    }

    public async Task AtualizarAsync(Perfil perfil, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE perfis
            SET nome          = @Nome,
                avatar_url    = @AvatarUrl,
                is_infantil   = @IsInfantil,
                usa_libras    = @UsaLibras,
                atualizado_em = @AtualizadoEm
            WHERE id = @Id
            """;

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, PerfilRow.FromDomain(perfil), cancellationToken: ct));
    }

    public async Task ExcluirAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM perfis WHERE id = @Id";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    // ── Mapeamento ──────────────────────────────────────────────────────────

    private sealed record PerfilRow(
        Guid Id,
        Guid ContaId,
        string Nome,
        string? AvatarUrl,
        bool IsInfantil,
        bool UsaLibras,
        DateTime CriadoEm,
        DateTime AtualizadoEm)
    {
        public Perfil ToDomain()
        {
            var perfil = (Perfil)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Perfil));
            SetProp(perfil, "Id", Id);
            SetProp(perfil, "ContaId", ContaId);
            SetProp(perfil, "Nome", Nome);
            SetProp(perfil, "AvatarUrl", AvatarUrl);
            SetProp(perfil, "IsInfantil", IsInfantil);
            SetProp(perfil, "UsaLibras", UsaLibras);
            SetProp(perfil, "CriadoEm", CriadoEm);
            SetProp(perfil, "AtualizadoEm", AtualizadoEm);
            return perfil;
        }

        public static PerfilRow FromDomain(Perfil p) => new(
            p.Id, p.ContaId, p.Nome, p.AvatarUrl,
            p.IsInfantil, p.UsaLibras, p.CriadoEm, p.AtualizadoEm);

        private static void SetProp<T>(T obj, string name, object? value)
        {
            var prop = typeof(T).GetProperty(name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            prop?.SetValue(obj, value);
        }
    }
}
