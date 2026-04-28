namespace Ambev.DeveloperEvaluation.Common.Results;

public sealed class Result<T> : Result
{
    private Result(T? value, bool isSuccess, IReadOnlyList<ResultError> errors, ResultErrorType errorType, int? statusCode)
        : base(isSuccess, errors, errorType, statusCode)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value)
        => new(value, true, SemErrosGenerico, ResultErrorType.None, null);

    public new static Result<T> Failure(ResultErrorType errorType, IEnumerable<ResultError> errors, int? statusCode = null)
    {
        var errosMaterializados = MaterializarErros(errors);
        return new Result<T>(default, false, errosMaterializados, errorType, statusCode);
    }

    public new static Result<T> Validation(IEnumerable<ResultError> errors, int? statusCode = 400)
        => Failure(ResultErrorType.Validation, errors, statusCode);

    public new static Result<T> BusinessRule(IEnumerable<ResultError> errors, int? statusCode = 422)
        => Failure(ResultErrorType.BusinessRule, errors, statusCode);

    public new static Result<T> NotFound(IEnumerable<ResultError> errors, int? statusCode = 404)
        => Failure(ResultErrorType.NotFound, errors, statusCode);

    public new static Result<T> Conflict(IEnumerable<ResultError> errors, int? statusCode = 409)
        => Failure(ResultErrorType.Conflict, errors, statusCode);

    public new static Result<T> Unauthorized(IEnumerable<ResultError> errors, int? statusCode = 401)
        => Failure(ResultErrorType.Unauthorized, errors, statusCode);

    public new static Result<T> Forbidden(IEnumerable<ResultError> errors, int? statusCode = 403)
        => Failure(ResultErrorType.Forbidden, errors, statusCode);

    public new static Result<T> Unexpected(IEnumerable<ResultError> errors, int? statusCode = 500)
        => Failure(ResultErrorType.Unexpected, errors, statusCode);

    private static readonly IReadOnlyList<ResultError> SemErrosGenerico = Array.Empty<ResultError>();
}