using System.Security.Cryptography;
using System.Text;
using Calegrafia.Domain.Interfaces;
using OtpNet;

namespace Calegrafia.Infrastructure.Services;

public sealed class TotpService : ITotpService
{
    private readonly byte[] _encryptionKey;

    /// <param name="encryptionKeyBase64">Chave AES-256 em Base64 (32 bytes). Deve vir de configuração segura.</param>
    public TotpService(string encryptionKeyBase64)
    {
        var key = Convert.FromBase64String(encryptionKeyBase64);
        if (key.Length != 32)
            throw new ArgumentException("A chave de criptografia deve ter exatamente 32 bytes (AES-256).", nameof(encryptionKeyBase64));

        _encryptionKey = key;
    }

    public string GerarSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(20); // 160 bits — padrão TOTP
        return Base32Encoding.ToString(bytes);
    }

    public string GerarQrCodeUri(string secret, string email, string issuer = "Calegrafia")
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedEmail = Uri.EscapeDataString(email);
        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secret}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period=30";
    }

    public bool ValidarCodigo(string secret, string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo) || codigo.Length != 6)
            return false;

        if (!long.TryParse(codigo, out _))
            return false;

        try
        {
            var keyBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(keyBytes);

            // VerifyTotp com janela de 1 — aceita código do intervalo anterior e seguinte (±30s)
            return totp.VerifyTotp(
                totp: codigo,
                timeStepMatched: out _,
                window: new VerificationWindow(previous: 1, future: 1));
        }
        catch
        {
            return false;
        }
    }

    public string CriptografarSecret(string secret)
    {
        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(secret);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Formato: IV (16 bytes) + ciphertext, codificado em Base64
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        aes.IV.CopyTo(result, 0);
        cipherBytes.CopyTo(result, aes.IV.Length);

        return Convert.ToBase64String(result);
    }

    public string DescriptografarSecret(string secretCriptografado)
    {
        var data = Convert.FromBase64String(secretCriptografado);

        using var aes = Aes.Create();
        aes.Key = _encryptionKey;

        var iv = data[..16];
        var cipherBytes = data[16..];
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
