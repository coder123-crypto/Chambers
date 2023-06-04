namespace Chambers.Core;

public record TemperaturePoint(DateTime Time, double Monitored, double Target) : IReadOnlyTemperaturePoint
{
    public double Delta()
    {
        return Target - Monitored;
    }
}