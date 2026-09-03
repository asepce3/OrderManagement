using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OrderManagement.Common;
using OrderManagement.DTOs;
using OrderManagement.Redis;

namespace OrderManagement.Services;

public class IdempotencyService : IIdempotencyService
{
    private readonly IRedisRepository _redisRepository;
    private const string Prefix = "idempotency:";
    private const int TtlSeconds = 3600;

    public IdempotencyService(IRedisRepository redisRepository)
    {
        _redisRepository = redisRepository ?? throw new ArgumentNullException(nameof(redisRepository));
    }

    public async Task<Result<T>> ProcessAsync<T>(string idempotencyKey, object payload, Func<Task<Result<T>>> action)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Result<T>.Failure("Idempotency-Key header is required.", StatusCodes.Status400BadRequest);

        var payloadHash = ComputePayloadHash(payload);
        var cacheKey = $"{Prefix}{idempotencyKey}";

        bool acquired = await _redisRepository.TrySetIfNotExistsAsync(cacheKey, "", TimeSpan.FromSeconds(TtlSeconds));
        if (!acquired)
        {
            var cachedValue = await _redisRepository.GetAsync(cacheKey);
            if (cachedValue == null)
            {
                return Result<T>.Failure("Duplicate request occured.", StatusCodes.Status400BadRequest);
            }

            var cached = JsonSerializer.Deserialize<IdempotencyCacheDto<T>>(cachedValue, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                IncludeFields = false
            });
            if (cached == null)
                return Result<T>.Failure("Duplicate request occured.", StatusCodes.Status400BadRequest);

            if (cached.Hash != payloadHash)
                return Result<T>.Failure("Idempotency-Key has been used with different payload.", StatusCodes.Status409Conflict);

            return cached.Response;
        }

        var result = await action();
        var cacheData = new IdempotencyCacheDto<T>
        {
            Hash = payloadHash,
            Response = result
        };

        if (result.IsFailure)
        {
            await _redisRepository.DeleteAsync(cacheKey);
            return result;
        }

        await _redisRepository.SetAsync(
            cacheKey,
            JsonSerializer.Serialize(cacheData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            TimeSpan.FromSeconds(TtlSeconds));

        return result;
    }

    private static string ComputePayloadHash(object payload)
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(payload, options);
        var sortedJson = SortJsonKeys(json);

        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sortedJson));
        return Convert.ToHexString(bytes);
    }

    private static string SortJsonKeys(string json)
    {
        using var document = JsonDocument.Parse(json);
        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });

        WriteSortedElement(writer, document.RootElement);
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteSortedElement(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteSortedElement(writer, property.Value);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteSortedElement(writer, item);
                }
                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;

            case JsonValueKind.Number:
                writer.WriteRawValue(element.GetRawText());
                break;

            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;

            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;

            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;

            default:
                writer.WriteRawValue(element.GetRawText());
                break;
        }
    }

}
