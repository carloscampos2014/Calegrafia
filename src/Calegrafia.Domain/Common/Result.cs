namespace Calegrafia.Domain.Common;

/// <summary>
/// Representa o resultado de uma operação que pode falhar com mensagem de erro.
/// Evita lançar exceções para fluxos de domínio esperados.
/// </summary>
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string Error { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        Error = string.Empty;
    }

    private Result(string error)
    {
        IsSuccess = false;
        Value = default;
        Error = error;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error) => new(error);

    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<string, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value!) : onFailure(Error);
}

/// <summary>
/// Resultado sem valor de retorno — apenas sucesso ou falha.
/// </summary>
public sealed class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }

    private Result(bool success, string error)
    {
        IsSuccess = success;
        Error = error;
    }

    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string error) => new(false, error);
}
