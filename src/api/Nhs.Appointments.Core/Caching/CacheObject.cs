namespace Nhs.Appointments.Core.Caching;

internal record LazySlideCacheObject
{
    public LazySlideCacheObject(object value, DateTimeOffset dateTimeUpdated)
    {
        Status = SlidingStatus.Fresh;
        DateTimeUpdated = dateTimeUpdated;
        Value = value;
    }
    
    public LazySlideCacheObject(object value, DateTimeOffset dateTimeUpdated, bool sliding)
    {
        Status = sliding ? SlidingStatus.Sliding : SlidingStatus.Initialising;
        DateTimeUpdated = dateTimeUpdated;
        Value = value;
    }
    
    public string Status { get; }
    public DateTimeOffset? DateTimeUpdated { get; }
    public object Value { get; set; }
}

internal record CacheObject<T>(T Value);

public static class SlidingStatus
{
    public const string Sliding = "Sliding";
    public const string Initialising = "Initialising";
    public const string Fresh = "Fresh";
}
