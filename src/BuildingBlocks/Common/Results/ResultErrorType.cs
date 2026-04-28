namespace Ambev.DeveloperEvaluation.Common.Results;

public enum ResultErrorType
{
    None = 0,
    Validation = 1,
    BusinessRule = 2,
    NotFound = 3,
    Conflict = 4,
    Unauthorized = 5,
    Forbidden = 6,
    Unexpected = 7
}