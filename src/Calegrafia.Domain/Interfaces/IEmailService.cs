namespace Calegrafia.Domain.Interfaces;

public interface IEmailService
{
    /// <summary>Envia email de confirmação de cadastro com link de ativação.</summary>
    Task EnviarConfirmacaoCadastroAsync(string destinatario, string nomeUsuario, string linkConfirmacao, CancellationToken ct = default);

    /// <summary>Envia email de redefinição de senha (link expira em 10 minutos).</summary>
    Task EnviarRedefinicaoSenhaAsync(string destinatario, string nomeUsuario, string linkRedefinicao, CancellationToken ct = default);

    /// <summary>Envia email de reset de TOTP/MFA (link expira em 10 minutos).</summary>
    Task EnviarResetMfaAsync(string destinatario, string nomeUsuario, string linkReset, CancellationToken ct = default);

    /// <summary>
    /// Envia email com arquivo JSON de exportação de dados do usuário (LGPD — portabilidade).
    /// </summary>
    Task EnviarExportacaoDadosAsync(string destinatario, string nomeUsuario, byte[] dadosJson, string nomeArquivo, CancellationToken ct = default);
}
