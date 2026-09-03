using StackExchange.Redis;

namespace OrderManagement.Redis;

public class RedisRepository : IRedisRepository
{
    private readonly IDatabase _database;

    public RedisRepository(IConnectionMultiplexer connection)
    {
        _database = connection.GetDatabase();
    }

    public async Task<string?> GetAsync(string key)
    {
        var value = await _database.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task<bool> TrySetIfNotExistsAsync(string key, string value, TimeSpan expiry)
    {
        return await _database.StringSetAsync(key, value, expiry, when: When.NotExists);
    }

    public async Task SetAsync(string key, string value, TimeSpan expiry)
    {
        await _database.StringSetAsync(key, value, expiry);
    }

    public async Task DeleteAsync(string key)
    {
        await _database.KeyDeleteAsync(key);
    }
}

