using OrderManagement.Common;

namespace OrderManagement.DTOs;

public class IdempotencyCacheDto<T>
{
    public string Hash { get; set; } = string.Empty;
    public Result<T> Response { get; set; } = null!;
}
