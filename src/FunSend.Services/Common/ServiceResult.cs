namespace FunSend.Services.Common;

/// <summary>Why a service call did not succeed, so controllers can pick a status code.</summary>
public enum ServiceErrorKind
{
    None = 0,
    NotFound = 1,
    Forbidden = 2,
    Validation = 3,
    Conflict = 4
}

/// <summary>
/// Outcome of a service call. The service layer never throws for an expected
/// refusal (a missing video, a user without rights) and never returns an
/// ASP.NET type, which keeps it testable without a web server.
/// </summary>
public class ServiceResult
{
    protected ServiceResult(bool succeeded, ServiceErrorKind errorKind, IReadOnlyList<string> errors)
    {
        Succeeded = succeeded;
        ErrorKind = errorKind;
        Errors = errors;
    }

    public bool Succeeded { get; }

    public ServiceErrorKind ErrorKind { get; }

    public IReadOnlyList<string> Errors { get; }

    public string? FirstError => Errors.Count > 0 ? Errors[0] : null;

    public static ServiceResult Success()
        => new(true, ServiceErrorKind.None, []);

    public static ServiceResult Failure(ServiceErrorKind kind, params string[] errors)
        => new(false, kind, errors);

    public static ServiceResult NotFound(string message = "The item was not found.")
        => Failure(ServiceErrorKind.NotFound, message);

    public static ServiceResult Forbidden(string message = "You are not allowed to do this.")
        => Failure(ServiceErrorKind.Forbidden, message);

    public static ServiceResult Invalid(params string[] errors)
        => Failure(ServiceErrorKind.Validation, errors);

    public static ServiceResult Conflict(string message)
        => Failure(ServiceErrorKind.Conflict, message);
}

/// <summary>A <see cref="ServiceResult"/> that carries a value when it succeeds.</summary>
public sealed class ServiceResult<T> : ServiceResult
{
    private ServiceResult(bool succeeded, ServiceErrorKind errorKind, IReadOnlyList<string> errors, T? value)
        : base(succeeded, errorKind, errors)
    {
        Value = value;
    }

    public T? Value { get; }

    public static ServiceResult<T> Success(T value)
        => new(true, ServiceErrorKind.None, [], value);

    public static new ServiceResult<T> Failure(ServiceErrorKind kind, params string[] errors)
        => new(false, kind, errors, default);

    public static new ServiceResult<T> NotFound(string message = "The item was not found.")
        => Failure(ServiceErrorKind.NotFound, message);

    public static new ServiceResult<T> Forbidden(string message = "You are not allowed to do this.")
        => Failure(ServiceErrorKind.Forbidden, message);

    public static new ServiceResult<T> Invalid(params string[] errors)
        => Failure(ServiceErrorKind.Validation, errors);

    public static new ServiceResult<T> Conflict(string message)
        => Failure(ServiceErrorKind.Conflict, message);
}
