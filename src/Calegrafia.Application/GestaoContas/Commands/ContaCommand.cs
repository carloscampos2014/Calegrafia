namespace Calegrafia.Application.GestaoContas.Commands;

public sealed record ExportarDadosCommand(Guid ContaId);

public sealed record ExcluirContaCommand(
    Guid ContaId,
    string SenhaAtual);


