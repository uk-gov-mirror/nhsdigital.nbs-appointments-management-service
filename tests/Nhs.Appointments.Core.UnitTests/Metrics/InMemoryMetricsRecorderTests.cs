using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Nhs.Appointments.Core.Metrics;

namespace Nhs.Appointments.Core.UnitTests.Metrics;

public class InMemoryMetricsRecorderTests
{
    private readonly Mock<ILogger<IMetricsRecorder>> logger = new();
    private readonly InMemoryMetricsRecorder _sut;

    public InMemoryMetricsRecorderTests() => _sut = new InMemoryMetricsRecorder(logger.Object);

    [Fact]
    public void RecordMetric_RecordsMetrics()
    {
        var random = new Random();
        var expectedValue1 = random.Next(1,1000);
        var generatedName1 = Guid.NewGuid().ToString();
        var testMetric = new TestMetric(generatedName1, expectedValue1);
        var expectedJson1 = JsonConvert.SerializeObject(testMetric);

        var generatedName2 = Guid.NewGuid().ToString();
        var expectedValue2 = Guid.NewGuid().ToString();
        var otherMetric = new OtherMetric(generatedName2, "myField", expectedValue2);
        var expectedJson2 = JsonConvert.SerializeObject(otherMetric);
        
        _sut.RecordMetric(testMetric);
        _sut.RecordMetric(otherMetric);

        logger.Verify(x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, t) =>
                        state.ToString().Contains($"{expectedJson1}")
                ),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Once
        );
        logger.Verify(x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, t) =>
                        state.ToString().Contains($"{expectedJson2}")
                ),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Once
        );

        logger.VerifyNoOtherCalls();
    }

    private class TestMetric(string name, int value) : IMetric
    {
        public string Name => name;

        public int Value => value;
    }

    private class OtherMetric(string name, string field, string value) : IMetric
    {
        public string Name => name;

        public string Field => field;

        public string Value => value;
    }
}
