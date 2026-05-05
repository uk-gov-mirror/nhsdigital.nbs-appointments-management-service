namespace Nhs.Appointments.Core.Metrics;

public interface IMetricsRecorder
{
    void RecordMetric(IMetric metric);
}
