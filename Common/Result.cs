namespace OrderManagement.Common;

public class Result<T>
{
    public bool IsSuccess { get; init; }
    public bool IsFailure => !IsSuccess;
    public T? Data { get; init; }
    public string? Error { get; init; }
    public int Code { get; init; }

    public static Result<T> Success(T value) => new() { IsSuccess = true, Data = value };
    public static Result<T> Failure(string error, int code) => new() { IsSuccess = false, Error = error, Code = code };
}
