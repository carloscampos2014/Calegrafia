using System.Text.RegularExpressions;
using Calegrafia.Domain.Common;

namespace Calegrafia.Domain.ValueObjects;

public sealed class Email : IEquatable<Email>
{
    private static readonly Regex _regex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private Email(string value) => Value = value;

    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Email>.Failure("Email não pode ser vazio.");

        var normalized = value.Trim().ToLowerInvariant();

        if (!_regex.IsMatch(normalized))
            return Result<Email>.Failure("Formato de email inválido.");

        if (normalized.Length > 255)
            return Result<Email>.Failure("Email não pode ter mais de 255 caracteres.");

        return Result<Email>.Success(new Email(normalized));
    }

    public bool Equals(Email? other) =>
        other is not null && Value == other.Value;

    public override bool Equals(object? obj) =>
        obj is Email other && Equals(other);

    public override int GetHashCode() =>
        Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}
