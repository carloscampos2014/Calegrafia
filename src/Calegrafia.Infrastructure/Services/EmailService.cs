using Calegrafia.Domain.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace Calegrafia.Infrastructure.Services;

public sealed class EmailService : IEmailService
{
    private readonly EmailOptions _options;

    public EmailService(EmailOptions options)
    {
        _options = options;
    }

    public async Task EnviarConfirmacaoCadastroAsync(
        string destinatario, string nomeUsuario, string linkConfirmacao, CancellationToken ct = default)
    {
        var assunto = "Confirme seu cadastro no Calegrafia";
        var corpo = $"""
            <h2>Bem-vindo ao Calegrafia, {nomeUsuario}!</h2>
            <p>Para ativar sua conta, clique no link abaixo:</p>
            <p><a href="{linkConfirmacao}">Confirmar meu email</a></p>
            <p>Este link é válido por 24 horas.</p>
            <hr/>
            <p><small>Se você não criou esta conta, ignore este email.</small></p>
            """;

        await EnviarAsync(destinatario, nomeUsuario, assunto, corpo, ct);
    }

    public async Task EnviarRedefinicaoSenhaAsync(
        string destinatario, string nomeUsuario, string linkRedefinicao, CancellationToken ct = default)
    {
        var assunto = "Redefinição de senha — Calegrafia";
        var corpo = $"""
            <h2>Olá, {nomeUsuario}</h2>
            <p>Recebemos uma solicitação para redefinir sua senha.</p>
            <p><a href="{linkRedefinicao}">Redefinir minha senha</a></p>
            <p><strong>Este link expira em 10 minutos.</strong></p>
            <hr/>
            <p><small>Se você não solicitou isso, sua conta continua segura. Ignore este email.</small></p>
            """;

        await EnviarAsync(destinatario, nomeUsuario, assunto, corpo, ct);
    }

    public async Task EnviarResetMfaAsync(
        string destinatario, string nomeUsuario, string linkReset, CancellationToken ct = default)
    {
        var assunto = "Reset de autenticador — Calegrafia";
        var corpo = $"""
            <h2>Olá, {nomeUsuario}</h2>
            <p>Recebemos uma solicitação para desativar o autenticador de dois fatores da sua conta.</p>
            <p><a href="{linkReset}">Desativar meu autenticador</a></p>
            <p><strong>Este link expira em 10 minutos.</strong></p>
            <hr/>
            <p><small>Se você não solicitou isso, sua conta continua protegida. Ignore este email.</small></p>
            """;

        await EnviarAsync(destinatario, nomeUsuario, assunto, corpo, ct);
    }

    public async Task EnviarExportacaoDadosAsync(
        string destinatario, string nomeUsuario, byte[] dadosJson, string nomeArquivo, CancellationToken ct = default)
    {
        var mensagem = new MimeMessage();
        mensagem.From.Add(new MailboxAddress(_options.NomeRemetente, _options.EmailRemetente));
        mensagem.To.Add(new MailboxAddress(nomeUsuario, destinatario));
        mensagem.Subject = "Exportação dos seus dados — Calegrafia";

        var builder = new BodyBuilder
        {
            HtmlBody = $"""
                <h2>Olá, {nomeUsuario}</h2>
                <p>Conforme solicitado, seus dados estão em anexo no arquivo <strong>{nomeArquivo}</strong>.</p>
                <p>Este arquivo contém todas as informações da sua conta no Calegrafia.</p>
                <hr/>
                <p><small>Solicitação processada conforme LGPD Art. 18 — Direito à portabilidade.</small></p>
                """
        };

        builder.Attachments.Add(nomeArquivo, dadosJson, ContentType.Parse("application/json"));
        mensagem.Body = builder.ToMessageBody();

        await EnviarMensagemAsync(mensagem, ct);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task EnviarAsync(string destinatario, string nomeDestinatario, string assunto, string corpoHtml, CancellationToken ct)
    {
        var mensagem = new MimeMessage();
        mensagem.From.Add(new MailboxAddress(_options.NomeRemetente, _options.EmailRemetente));
        mensagem.To.Add(new MailboxAddress(nomeDestinatario, destinatario));
        mensagem.Subject = assunto;
        mensagem.Body = new TextPart(TextFormat.Html) { Text = corpoHtml };

        await EnviarMensagemAsync(mensagem, ct);
    }

    private async Task EnviarMensagemAsync(MimeMessage mensagem, CancellationToken ct)
    {
        using var client = new SmtpClient();

        var secureSocket = _options.UsarSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;

        await client.ConnectAsync(_options.Host, _options.Porta, secureSocket, ct);

        if (!string.IsNullOrEmpty(_options.Usuario))
            await client.AuthenticateAsync(_options.Usuario, _options.Senha, ct);

        await client.SendAsync(mensagem, ct);
        await client.DisconnectAsync(quit: true, ct);
    }
}
