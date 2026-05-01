using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Nhs.Appointments.Core.Metrics;

public class InMemoryMetricsRecorder(ILogger<IMetricsRecorder> logger) : IMetricsRecorder
{
    public void RecordMetric(IMetric metric)
    {
        var json = JsonConvert.SerializeObject(metric);
        logger.LogInformation("{Metric}", json);
    }
}
