namespace Chambers.Core;

public interface IReadOnlyTemperaturePoint
{
    DateTime Time { get; }

    double Monitored { get; }

    double Target { get; }

    double Delta();
}
