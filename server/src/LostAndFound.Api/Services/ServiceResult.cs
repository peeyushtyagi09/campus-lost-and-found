namespace LostAndFound.Api.Services;

public sealed class ServiceResult<T>
{
    private ServiceResult(bool isSuccess, T? value, int statusCode, string? errorTitle)
    {
        IsSuccess = isSuccess;
        Value = value;
        StatusCode = statusCode;
        ErrorTitle = errorTitle;
    }

    public bool IsSuccess { get; }

    public T? Value { get; }

    public int StatusCode { get; }

    public string? ErrorTitle { get; }

    public static ServiceResult<T> Success(T value, int statusCode = StatusCodes.Status200OK)
        => new(true, value, statusCode, null);

    public static ServiceResult<T> Failure(int statusCode, string errorTitle)
        => new(false, default, statusCode, errorTitle);
}
