namespace OrderManagement.Redis;

public interface IRedisRepository
{
    Task<string?> GetAsync(string key);
    Task<bool> TrySetIfNotExistsAsync(string key, string value, TimeSpan expiry);
    Task SetAsync(string key, string value, TimeSpan expiry);
    Task DeleteAsync(string key);
}
