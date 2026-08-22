namespace Calegrafia.Application.Auth.Commands;

public sealed record AtivarMfaCommand(Guid ContaId);

/// <summary>Passo 2 da ativação — confirma com código TOTP e o secret gerado no passo 1.</summary>
public sealed record AtivarMfaConfirmarCommand(Guid ContaId, string SecretPlain, string CodigoTotp);

public sealed record DesativarMfaCommand(Guid ContaId, string CodigoTotp);
public sealed record ResetMfaSolicitarCommand(string Email);
public sealed record ResetMfaConfirmarCommand(string Token);

public sealed record AtivarMfaResult(string QrCodeUri, string SecretPlain);
