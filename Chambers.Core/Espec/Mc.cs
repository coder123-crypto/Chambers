using System.Globalization;
using System.IO.Ports;
using System.Text.RegularExpressions;
using static System.Globalization.CultureInfo;

namespace Chambers.Core.Espec;

public sealed class Mc
{
    public string McInfo { get; set; } = string.Empty;

    public IReadOnlyTemperaturePoint Temperature
    {
        get
        {
            try
            {
                return TemperatureRequest();
            }
            catch
            {
                return TemperatureRequest();
            }
        }
    }

    ~Mc()
    {
        Close();
    }

    public void Open(string port, int channel)
    {
        Close();

        lock (_locker)
        {
            _port.PortName = port;
            _channel = channel;
            _port.Open();

            McInfo = Request("TYPE?") switch
            {
                "T,SCP220,110.0" => "MC711",
                "T,SCP220,190.0" => "MC811",
                "T,P-300,190.0" => "MC812",
                _ => "Unknown Chamber"
            };
        }
    }

    public void Open(string port)
    {
        Open(port, -1);
    }

    public void Close()
    {
        lock (_locker)
        {
            _port.Close();
        }
    }

    public void TurnOff()
    {
        Request("MODE, STANDBY");
    }

    public void TurnOn()
    {
        Request("MODE, CONSTANT");
    }

    public void GoTemp(double temp, RefMode refMode)
    {
        Request($"SET, REF{(int)refMode}");
        Request("MODE, CONSTANT");
        Request($"TEMP, S{temp.ToString(InvariantCulture)}");

        IReadOnlyTemperaturePoint t;
        do
        {
            t = TemperatureRequest();
        } while (Math.Abs(t.Target - temp) > 0.05);
    }

    public void GoTemp(double temp, TimeSpan time, RefMode refMode)
    {
        Request($"RUN PRGM, TEMP{Temperature.Monitored.ToString(InvariantCulture)} GOTEMP{temp.ToString(InvariantCulture)} TIME{time:hh\\:mm} REF{(int)refMode}");

        for (int i = 0; i < 5; i++)
        {
            TemperatureRequest();
        }
    }

    private IReadOnlyTemperaturePoint TemperatureRequest()
    {
        string line = Request("TEMP?");
        var g = Regex.Match(line, @"(-?\d+\.?\d*),(-?\d+\.?\d*),(-?\d+\.?\d*),(-?\d+\.?\d*)").Groups;
        return new TemperaturePoint(DateTime.Now, double.Parse(g[1].ToString(), InvariantCulture), double.Parse(g[2].ToString(), InvariantCulture));
    }

    private string Request(string request)
    {
        string output = PrepareCommand(request);

        lock (_locker)
        {
            try
            {
                _port.WriteLine(output);
                return _port.ReadLine().Trim();
            }
            catch (Exception e)
            {
                throw new ChamberConnectionException($"КТХ не ответила на {output}", e);
            }
        }
    }

    private string PrepareCommand(string command)
    {
        return _channel == -1 ? command : $"{_channel}, {command}";
    }

    private readonly object _locker = new();
    private readonly SerialPort _port = new()
    {
        BaudRate = 19200,
        DataBits = 8,
        Parity = Parity.None,
        StopBits = StopBits.One,
        Handshake = Handshake.RequestToSend,
        ReadTimeout = 1000,
        WriteTimeout = 1000,
        ReadBufferSize = 4096,
        WriteBufferSize = 4096
    };
    private int _channel = -1;
}