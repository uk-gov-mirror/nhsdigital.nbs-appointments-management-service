namespace Nhs.Appointments.Core.Caching;

public record CacheStoreResponse<T>(bool Success, T Response);

public static class CacheStoreResponses
{
    public static CacheStoreResponse<T> Success<T>(T response) => new (true, response);
    public static CacheStoreResponse<T> Fail<T>() => new (false, default);
}
