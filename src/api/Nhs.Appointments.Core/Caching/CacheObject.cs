namespace Nhs.Appointments.Core.Caching;

internal record LazySlideCacheObject
{
    public LazySlideCacheObject(object value, DateTimeOffset dateTimeUpdated)
    {
        DateTimeUpdated = dateTimeUpdated;
        Value = value;
    }
    
    internal bool DueToSlide(TimeSpan timeToSlide, DateTimeOffset dateTime) => DateTimeUpdated != null && DateTimeUpdated.Value.Add(timeToSlide) < dateTime;
    public DateTimeOffset? DateTimeUpdated { get; }
    public object Value { get; set; }
}

internal record CacheObject<T>(T Value);
