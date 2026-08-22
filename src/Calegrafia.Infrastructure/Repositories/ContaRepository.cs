using Calegrafia.Domain.Common;
using Calegrafia.Domain.Entities;
using Calegrafia.Domain.Interfaces;
using Calegrafia.Domain.ValueObjects;
using Dapper;
using Npgsql;

namespace Calegrafia.Infrastructure.Repositories;

public sealed class ContaRepository : IContaRepository
{
    private readonly string _connectionString;

    public ContaRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<Conta?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, email, senha_hash, status, mfa_ativo, mfa_secret,
                   tentativas_login, bloqueado_ate, criado_em, atualizado_em
            FROM contas
            WHERE id = @Id
            """;

        await using var conn = CreateConnection();
        var row = await conn.QuerySingleOrDefaultAsync<ContaRow>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
        return row?.ToDomain();
    }

    public async Task<Conta?> ObterPorEmailAsync(Email email, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, email, senha_hash, status, mfa_ativo, mfa_secret,
                   tentativas_login, bloqueado_ate, criado_em, atualizado_em
            FROM contas
            WHERE email = @Email
            """;

        await using var conn = CreateConnection();
        var row = await conn.QuerySingleOrDefaultAsync<ContaRow>(new CommandDefinition(sql, new { Email = email.Value }, cancellationToken: ct));
        return row?.ToDomain();
    }

    public async Task<bool> ExisteEmailAsync(Email email, CancellationToken ct = default)
    {
        const string sql = "SELECT COUNT(1) FROM contas WHERE email = @Email";
        await using var conn = CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { Email = email.Value }, cancellationToken: ct));
        return count > 0;
    }

    public async Task<Guid> CriarAsync(Conta conta, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO contas (id, email, senha_hash, status, mfa_ativo, mfa_secret,
                                tentativas_login, bloqueado_ate, criado_em, atualizado_em)
            VALUES (@Id, @Email, @SenhaHash, @Status, @MfaAtivo, @MfaSecret,
                    @TentativasLogin, @BloqueadoAte, @CriadoEm, @AtualizadoEm)
            """;

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, ContaRow.FromDomain(conta), cancellationToken: ct));
        return conta.Id;
    }

    public async Task AtualizarAsync(Conta conta, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE contas
            SET senha_hash       = @SenhaHash,
                status           = @Status,
                mfa_ativo        = @MfaAtivo,
                mfa_secret       = @MfaSecret,
                tentativas_login = @TentativasLogin,
                bloqueado_ate    = @BloqueadoAte,
                atualizado_em    = @AtualizadoEm
            WHERE id = @Id
            """;

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, ContaRow.FromDomain(conta), cancellationToken: ct));
    }

    // ── Mapeamento ──────────────────────────────────────────────────────────

    private sealed record ContaRow(
        Guid Id,
        string Email,
        string? SenhaHash,
        string Status,
        bool MfaAtivo,
        string? MfaSecret,
        int TentativasLogin,
        DateTime? BloqueadoAte,
        DateTime CriadoEm,
        DateTime AtualizadoEm)
    {
        public Conta ToDomain()
        {
            var emailVo = Domain.ValueObjects.Email.Create(Email).Value!;
            return ContaMapper.Map(this, emailVo);
        }

        public static ContaRow FromDomain(Conta c) => new(
            c.Id, c.Email.Value, c.SenhaHash,
            c.Status.ToString().ToLowerInvariant(),
            c.MfaAtivo, c.MfaSecret, c.TentativasLogin,
            c.BloqueadoAte, c.CriadoEm, c.AtualizadoEm);
    }
}

/// <summary>Mapeamento de ContaRow → Conta usando reflection mínima para preservar encapsulamento.</summary>
internal static class ContaMapper
{
    public static Conta Map(dynamic row, Email email)
    {
        var conta = (Conta)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Conta));

        SetProp(conta, "Id", (Guid)row.Id);
        SetProp(conta, "Email", email);
        SetProp(conta, "SenhaHash", (string?)row.SenhaHash);
        SetProp(conta, "Status", Enum.Parse<StatusConta>(row.Status, ignoreCase: true));
        SetProp(conta, "MfaAtivo", (bool)row.MfaAtivo);
        SetProp(conta, "MfaSecret", (string?)row.MfaSecret);
        SetProp(conta, "TentativasLogin", (int)row.TentativasLogin);
        SetProp(conta, "BloqueadoAte", (DateTime?)row.BloqueadoAte);
        SetProp(conta, "CriadoEm", (DateTime)row.CriadoEm);
        SetProp(conta, "AtualizadoEm", (DateTime)row.AtualizadoEm);

        return conta;
    }

    private static void SetProp<T>(T obj, string name, object? value)
    {
        var prop = typeof(T).GetProperty(name,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        prop?.SetValue(obj, value);
    }
}
