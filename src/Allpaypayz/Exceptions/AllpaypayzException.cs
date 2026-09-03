namespace Allpaypayz.Exceptions;

using System.Collections.Generic;

public class AllpaypayzException : System.Exception
{
    public string ErrorType { get; }
    public string Code { get; }
    public int? Status { get; }
    public string? RequestId { get; }
    public IReadOnlyList<Dictionary<string, object?>>? Details { get; }
    public int? RetryAfterSeconds { get; }

    public AllpaypayzException(
        string errorType,
        string code,
        string message,
        int? status = null,
        string? requestId = null,
        IReadOnlyList<Dictionary<string, object?>>? details = null,
        int? retryAfterSeconds = null
    ) : base(message)
    {
        ErrorType = errorType;
        Code = code;
        Status = status;
        RequestId = requestId;
        Details = details;
        RetryAfterSeconds = retryAfterSeconds;
    }
}

public class AllpaypayzValidationError : AllpaypayzException
{
    public AllpaypayzValidationError(string errorType, string code, string message, int? status, string? requestId, IReadOnlyList<Dictionary<string, object?>>? details, int? retryAfterSeconds)
        : base(errorType, code, message, status, requestId, details, retryAfterSeconds) { }
}

public class AllpaypayzAuthenticationError : AllpaypayzException
{
    public AllpaypayzAuthenticationError(string errorType, string code, string message, int? status, string? requestId, IReadOnlyList<Dictionary<string, object?>>? details, int? retryAfterSeconds)
        : base(errorType, code, message, status, requestId, details, retryAfterSeconds) { }
}

public class AllpaypayzNotFoundError : AllpaypayzException
{
    public AllpaypayzNotFoundError(string errorType, string code, string message, int? status, string? requestId, IReadOnlyList<Dictionary<string, object?>>? details, int? retryAfterSeconds)
        : base(errorType, code, message, status, requestId, details, retryAfterSeconds) { }
}

public class AllpaypayzConflictError : AllpaypayzException
{
    public AllpaypayzConflictError(string errorType, string code, string message, int? status, string? requestId, IReadOnlyList<Dictionary<string, object?>>? details, int? retryAfterSeconds)
        : base(errorType, code, message, status, requestId, details, retryAfterSeconds) { }
}

public class AllpaypayzBusinessError : AllpaypayzException
{
    public AllpaypayzBusinessError(string errorType, string code, string message, int? status, string? requestId, IReadOnlyList<Dictionary<string, object?>>? details, int? retryAfterSeconds)
        : base(errorType, code, message, status, requestId, details, retryAfterSeconds) { }
}

public class AllpaypayzRateLimitError : AllpaypayzException
{
    public AllpaypayzRateLimitError(string errorType, string code, string message, int? status, string? requestId, IReadOnlyList<Dictionary<string, object?>>? details, int? retryAfterSeconds)
        : base(errorType, code, message, status, requestId, details, retryAfterSeconds) { }
}

public class AllpaypayzGatewayError : AllpaypayzException
{
    public AllpaypayzGatewayError(string errorType, string code, string message, int? status, string? requestId, IReadOnlyList<Dictionary<string, object?>>? details, int? retryAfterSeconds)
        : base(errorType, code, message, status, requestId, details, retryAfterSeconds) { }
}

public class AllpaypayzNetworkError : AllpaypayzException
{
    public AllpaypayzNetworkError(string errorType, string code, string message, int? status, string? requestId, IReadOnlyList<Dictionary<string, object?>>? details, int? retryAfterSeconds)
        : base(errorType, code, message, status, requestId, details, retryAfterSeconds) { }
}

public class AllpaypayzWebhookError : System.Exception
{
    public string Code { get; }
    public AllpaypayzWebhookError(string code, string message) : base(message) { Code = code; }
}
