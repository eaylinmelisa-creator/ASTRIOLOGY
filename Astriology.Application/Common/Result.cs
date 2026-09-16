namespace Astriology.Application.Common;

/// <summary>
/// Why an operation failed, in terms the presentation layer can map to a status
/// code without knowing anything about the service that produced it.
/// </summary>
public enum ResultError
{
    None = 0,

    /// <summary>Input the caller can correct. Maps to 400.</summary>
    Validation,

    /// <summary>Missing or bad credentials. Maps to 401.</summary>
    Unauthorized,

    /// <summary>Authenticated but not allowed. Maps to 403.</summary>
    Forbidden,

    /// <summary>Maps to 404.</summary>
    NotFound,

    /// <summary>State prevents the operation, such as deleting a category in use. Maps to 409.</summary>
    Conflict,
}

/// <summary>
/// Outcome of an operation that produces no value.
/// </summary>
/// <remarks>
/// Expected business failures travel back as a result, not as an exception.
/// Exceptions stay reserved for the genuinely unexpected, so the global handler
/// never has to distinguish "wrong password" from "the database is down".
/// </remarks>
public class Result
{
    protected Result(bool isSuccess, string? error, ResultError errorKind)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorKind = errorKind;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    /// <summary>Message safe to show the caller. Null on success.</summary>
    public string? Error { get; }

    public ResultError ErrorKind { get; }

    public static Result Success() => new(true, null, ResultError.None);

    public static Result Failure(ResultError kind, string error) => new(false, error, kind);

    public static Result Invalid(string error) => Failure(ResultError.Validation, error);

    public static Result Unauthorized(string error) => Failure(ResultError.Unauthorized, error);

    public static Result Forbidden(string error) => Failure(ResultError.Forbidden, error);

    public static Result NotFound(string error) => Failure(ResultError.NotFound, error);

    public static Result Conflict(string error) => Failure(ResultError.Conflict, error);
}

/// <summary>Outcome of an operation that produces a value on success.</summary>
public sealed class Result<T> : Result
{
    private Result(bool isSuccess, T? value, string? error, ResultError errorKind)
        : base(isSuccess, error, errorKind)
        => Value = value;

    /// <summary>The produced value. Null when <see cref="Result.IsFailure"/>.</summary>
    public T? Value { get; }

    public static Result<T> Success(T value) => new(true, value, null, ResultError.None);

    public static new Result<T> Failure(ResultError kind, string error) => new(false, default, error, kind);

    public static new Result<T> Invalid(string error) => Failure(ResultError.Validation, error);

    public static new Result<T> Unauthorized(string error) => Failure(ResultError.Unauthorized, error);

    public static new Result<T> Forbidden(string error) => Failure(ResultError.Forbidden, error);

    public static new Result<T> NotFound(string error) => Failure(ResultError.NotFound, error);

    public static new Result<T> Conflict(string error) => Failure(ResultError.Conflict, error);
}
