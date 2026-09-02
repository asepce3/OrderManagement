namespace OrderManagement.Common;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string Message { get; init; } = string.Empty;

    public static ApiResponse<T> SuccessResponse(T data, string message = "OK")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static ApiResponse<T> ErrorResponse(string message)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Data = default,
            Message = message
        };
    }
}
