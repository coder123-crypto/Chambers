namespace Chambers.Core;

public interface IMcChamber
{
    string McInfo { get; }

    IReadOnlyTemperaturePoint Temperature { get; }

    void Open(string port, int channel);

    void Open(string port);

    void Close();

    void TurnOn();

    void TurnOff();

    void GoTemp(double temp, McRefMode refMode);

    void GoTemp(double temp, TimeSpan time, McRefMode refMode);
}