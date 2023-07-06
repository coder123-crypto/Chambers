using System.IO.Ports;
using System.Text.RegularExpressions;
using Serial2Network.Core;
using static System.Globalization.CultureInfo;

namespace Chambers.Core;

public sealed class McChamber
{
    public string ChamberInfo
    {
        get
        {
            return _info switch
            {
                "T,SCP220,110.0" => "MC711",
                "T,SCP220,190.0" => "MC811",
                "T,P-300,190.0" => "MC812",
                _ => "Unknown Chamber"
            };
        }
    }
    private string _info = string.Empty;
    
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

    ~McChamber()
    {
        ClosePort();
    }

    public void OpenPort(string port, int channel)
    {
        ClosePort();

        lock (_locker)
        {
            _channel = channel;
            _client.Connect(port);
        }
    }

    public void OpenPort(string port)
    {
        OpenPort(port, -1);
    }

    public void ClosePort()
    {
        lock (_locker)
        {
            _client.Disconnect();
        }
    }

    public bool ConnectToChamber()
    {
        try
        {
            Request("ROM?");
        }
        catch (TimeoutException)
        {
            try
            {
                string line = Request("ROM?");
                if (!line.StartsWith("JMIC", StringComparison.Ordinal))
                {
                    return false;
                }
            }
            catch (TimeoutException)
            {
                return false;
            }
        }

        _info = Request("TYPE?");

        return true;
    }

    public void TurnOff()
    {
        Request("MODE, STANDBY");
    }

    public void TurnOn()
    {
        Request("MODE, CONSTANT");
    }

    public void GoTemp(double temp, McRefMode refMode)
    {
        Request($"SET, REF{(int) refMode}");
        Request("MODE, CONSTANT");
        Request($"TEMP, S{temp.ToString(InvariantCulture)}");

        IReadOnlyTemperaturePoint t;
        do
        {
            t = TemperatureRequest();
        } while (Math.Abs(t.Target - temp) > 0.05);
    }

    public void GoTemp(double temp, TimeSpan time, McRefMode refMode)
    {
        Request($"RUN PRGM, TEMP{Temperature.Monitored.ToString(InvariantCulture)} GOTEMP{temp.ToString(InvariantCulture)} TIME{time:hh\\:mm} REF{(int) refMode}");

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
                _client.WriteLine(output);
                return _client.ReadLine().Trim();
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
    private readonly Client _client = new()
    {
        SerialPortOptions =
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
        }
    };
    private int _channel = -1;
}