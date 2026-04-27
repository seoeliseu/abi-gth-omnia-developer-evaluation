namespace Ambev.DeveloperEvaluation.Common.Results;

public class Result
{
    private static readonly IReadOnlyList<ResultError> SemErros = Array.Empty<ResultError>();

    protected Result(bool isSuccess, IReadOnlyList<ResultError> errors, ResultErrorType errorType, int? statusCode)
    {
        if (isSuccess && errors.Count > 0)
        {
            throw new ArgumentException("Um resultado de sucesso nao pode conter erros.", nameof(errors));
        }

        if (!isSuccess && errors.Count == 0)
        {
            throw new ArgumentException("Um resultado de falha deve conter pelo menos um erro.", nameof(errors));
        }

        IsSuccess = isSuccess;
        Errors = errors;
        ErrorType = errorType;
        StatusCode = statusCode;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public IReadOnlyList<ResultError> Errors { get; }

    public ResultErrorType ErrorType { get; }

    public int? StatusCode { get; }

    public static Result Success() => new(true, SemErros, ResultErrorType.None, null);

    public static Result Failure(ResultErrorType errorType, IEnumerable<ResultError> errors, int? statusCode = null)
    {
        var errosMaterializados = MaterializarErros(errors);
        return new Result(false, errosMaterializados, errorType, statusCode);
    }

    public static Result Validation(IEnumerable<ResultError> errors, int? statusCode = 400)
        => Failure(ResultErrorType.Validation, errors, statusCode);

    public static Result BusinessRule(IEnumerable<ResultError> errors, int? statusCode = 422)
        => Failure(ResultErrorType.BusinessRule, errors, statusCode);

    public static Result NotFound(IEnumerable<ResultError> errors, int? statusCode = 404)
        => Failure(ResultErrorType.NotFound, errors, statusCode);

    public static Result Conflict(IEnumerable<ResultError> errors, int? statusCode = 409)
        => Failure(ResultErrorType.Conflict, errors, statusCode);

    public static Result Unauthorized(IEnumerable<ResultError> errors, int? statusCode = 401)
        => Failure(ResultErrorType.Unauthorized, errors, statusCode);

    public static Result Forbidden(IEnumerable<ResultError> errors, int? statusCode = 403)
        => Failure(ResultErrorType.Forbidden, errors, statusCode);

    public static Result Unexpected(IEnumerable<ResultError> errors, int? statusCode = 500)
        => Failure(ResultErrorType.Unexpected, errors, statusCode);

    protected static IReadOnlyList<ResultError> MaterializarErros(IEnumerable<ResultError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var errosMaterializados = errors.ToArray();
        if (errosMaterializados.Length == 0)
        {
            throw new ArgumentException("A colecao de erros nao pode ser vazia.", nameof(errors));
        }

        return errosMaterializados;
    }
}