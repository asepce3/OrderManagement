namespace OrderManagement.Common;

public class Result<T>
{
    public bool IsSuccess { get; set; }
    public bool IsFailure => !IsSuccess;
    public T? Data { get; set; }
    public string? Error { get; set; }
    public int Code { get; set; }

    public static Result<T> Success(T value, int code = 200) => new() { IsSuccess = true, Data = value, Code = code };
    public static Result<T> Failure(string error, int code) => new() { IsSuccess = false, Error = error, Code = code };
}
