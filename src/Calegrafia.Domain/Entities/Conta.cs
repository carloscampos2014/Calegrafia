using Calegrafia.Domain.Common;
using Calegrafia.Domain.ValueObjects;

namespace Calegrafia.Domain.Entities;

public sealed class Conta
{
    public Guid Id { get; private set; }
    public Email Email { get; private set; }
    public string? SenhaHash { get; private set; }
    public StatusConta Status { get; private set; }
    public bool MfaAtivo { get; private set; }
    public string? MfaSecret { get; private set; }
    public int TentativasLogin { get; private set; }
    public DateTime? BloqueadoAte { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }

    private Conta() { Email = null!; } // EF/Dapper

    private Conta(Guid id, Email email, string? senhaHash)
    {
        Id = id;
        Email = email;
        SenhaHash = senhaHash;
        Status = StatusConta.Pendente;
        MfaAtivo = false;
        TentativasLogin = 0;
        CriadoEm = DateTime.UtcNow;
        AtualizadoEm = DateTime.UtcNow;
    }

    public static Result<Conta> Criar(Email email, string? senhaHash = null)
    {
        return Result<Conta>.Success(new Conta(Guid.NewGuid(), email, senhaHash));
    }

    // --- Confirmação de email ---

    public Result Ativar()
    {
        if (Status == StatusConta.Ativo)
            return Result.Failure("Conta já está ativa.");

        Status = StatusConta.Ativo;
        AtualizadoEm = DateTime.UtcNow;
        return Result.Success();
    }

    // --- Controle de tentativas e bloqueio ---

    public bool EstaBloqueada() =>
        Status == StatusConta.Bloqueado && BloqueadoAte.HasValue && BloqueadoAte > DateTime.UtcNow;

    public Result RegistrarTentativaFalha(int maxTentativas = 5, int minutosBloqueio = 15)
    {
        if (EstaBloqueada())
            return Result.Failure($"Conta bloqueada até {BloqueadoAte:HH:mm}.");

        TentativasLogin++;
        AtualizadoEm = DateTime.UtcNow;

        if (TentativasLogin >= maxTentativas)
        {
            Status = StatusConta.Bloqueado;
            BloqueadoAte = DateTime.UtcNow.AddMinutes(minutosBloqueio);
            return Result.Failure($"Conta bloqueada por {minutosBloqueio} minutos após {maxTentativas} tentativas falhas.");
        }

        return Result.Success();
    }

    public void ResetarTentativas()
    {
        TentativasLogin = 0;
        BloqueadoAte = null;

        if (Status == StatusConta.Bloqueado)
            Status = StatusConta.Ativo;

        AtualizadoEm = DateTime.UtcNow;
    }

    // --- MFA ---

    public Result AtivarMfa(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            return Result.Failure("Secret TOTP inválido.");

        MfaAtivo = true;
        MfaSecret = secret;
        AtualizadoEm = DateTime.UtcNow;
        return Result.Success();
    }

    public Result DesativarMfa()
    {
        if (!MfaAtivo)
            return Result.Failure("MFA não está ativo.");

        MfaAtivo = false;
        MfaSecret = null;
        AtualizadoEm = DateTime.UtcNow;
        return Result.Success();
    }

    // --- Senha ---

    public void AtualizarSenha(string senhaHash)
    {
        SenhaHash = senhaHash;
        AtualizadoEm = DateTime.UtcNow;
    }
}

public enum StatusConta
{
    Pendente,
    Ativo,
    Bloqueado
}
