using OrderManagement.Common;

namespace OrderManagement.Services;

public interface IIdempotencyService
{
    Task<Result<T>> ProcessAsync<T>(string idempotencyKey, object payload, Func<Task<Result<T>>> action);
}
