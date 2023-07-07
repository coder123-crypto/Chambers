namespace Chambers.Core;

public sealed class McChamber
{
    public string ChamberInfo => _mc?.McInfo ?? string.Empty;

    public IReadOnlyTemperaturePoint Temperature => _mc!.Temperature;

    ~McChamber()
    {
        Disconnect();
    }

    public void Connect(string connectionString, int channel)
    {
        Disconnect();

        lock (_locker)
        {
            if (connectionString.StartsWith("COM"))
            {
                var serial = new Local.McChamber();
                serial.Open(connectionString, channel);
                _mc = serial;
            }
            else
            {
                string[] args = connectionString.Split(":");

                var proto = new Remote.McChamber();
                proto.Connect($"http://{args[0]}:{args[1]}");
                proto.Open(args[2], channel);
                _mc = proto;
            }
        }
    }

    public void Connect(string port)
    {
        Connect(port, -1);
    }

    public void Disconnect()
    {
        lock (_locker)
        {
            _mc?.Close();
        }
    }
    
    public void TurnOff()
    {
        lock (_locker)
        {
            _mc!.TurnOff();
        }
    }

    public void TurnOn()
    {
        lock (_locker)
        {
            _mc!.TurnOn();
        }
    }

    public void GoTemp(double temp, McRefMode refMode)
    {
        _mc!.GoTemp(temp, refMode);

        IReadOnlyTemperaturePoint t;
        do
        {
            t = Temperature;
        } while (Math.Abs(t.Target - temp) > 0.05);
    }

    public void GoTemp(double temp, TimeSpan time, McRefMode refMode)
    {
        _mc!.GoTemp(temp, time, refMode);

        for (int i = 0; i < 5; i++)
        {
            _ = Temperature;
        }
    }

    private readonly object _locker = new();
    private IMcChamber? _mc;
}