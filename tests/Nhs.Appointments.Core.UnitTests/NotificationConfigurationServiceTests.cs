using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Nhs.Appointments.Core.Bookings;
using Nhs.Appointments.Core.Caching;
using Nhs.Appointments.Core.Caching.InMemory;
using Nhs.Appointments.Core.Concurrency;
using Nhs.Appointments.Core.Messaging;

namespace Nhs.Appointments.Core.UnitTests;
public class NotificationConfigurationServiceTests
{
    private readonly Mock<IMemoryCache> _memoryCacheMock = new();
    private readonly Mock<INotificationConfigurationStore> _storeMock = new();
    private readonly NotificationConfigurationService _sut;
    private static readonly string[] Covid5_11Services = { "COVID:5_11" };
    private readonly Mock<IOptions<LeaseManagerOptions>> _leaseOptions = new();

    public NotificationConfigurationServiceTests()
    {
        _leaseOptions.Setup(o => o.Value).Returns(new LeaseManagerOptions { Timeout = new TimeSpan(1, 0, 0) });
        _sut = new NotificationConfigurationService(new CacheService(new InMemoryCacheStore(_memoryCacheMock.Object), new InMemoryLeaseManager(_leaseOptions.Object), TimeProvider.System), _storeMock.Object);
    }

    [Fact]
    public async Task GetNotificationConfigurationsAsync_WithMatchingEventTypeAndService_ReturnsCombinedConfiguration()
    {
        // Arrange
        var eventType = "AppointmentBooked";
        var service = "COVID:5_11";

        var configs = new List<NotificationConfiguration>
        {
            new NotificationConfiguration
            {
                EventType = "AppointmentBooked",
                Services = Covid5_11Services,
                EmailTemplateId = "email-template-1",
                SmsTemplateId = null
            },
            new NotificationConfiguration
            {
                EventType = "AppointmentBooked",
                Services = Covid5_11Services,
                EmailTemplateId = null,
                SmsTemplateId = "sms-template-2"
            }
        };

        SetupMemoryCache(configs);

        // Act
        var result = await _sut.GetNotificationConfigurationsAsync(eventType, service);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(eventType, result.EventType);
        Assert.Single(result.Services);
        Assert.Equal(service, result.Services.First());
        Assert.Equal("email-template-1", result.EmailTemplateId);
        Assert.Equal("sms-template-2", result.SmsTemplateId);
    }

    [Fact]
    public async Task GetNotificationConfigurationsAsync_NoMatchingService_ReturnsNull()
    {
        // Arrange
        var eventType = "AppointmentBooked";
        var service = "UnknownService";

        var configs = new List<NotificationConfiguration>
        {
            new NotificationConfiguration
            {
                EventType = "AppointmentBooked",
                Services = Covid5_11Services,
                EmailTemplateId = "email-template-1",
                SmsTemplateId = "sms-template-1"
            }
        };

        SetupMemoryCache(configs);

        // Act
        var result = await _sut.GetNotificationConfigurationsAsync(eventType, service);

        // Assert
        Assert.Null(result);
    }

    private void SetupMemoryCache(IEnumerable<NotificationConfiguration> cacheEntry)
    {
        object outCache = new CacheObject<IEnumerable<NotificationConfiguration>>(cacheEntry);
        _memoryCacheMock
            .Setup(mc => mc.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
            .Callback(new TryGetValueCallback((object key, out object value) =>
            {
                value = outCache;
            }))
            .Returns(true);
    }

    private delegate void TryGetValueCallback(object key, out object value);
}
