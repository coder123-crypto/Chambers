using Grpc.Net.Client;
using McService;

namespace Chambers.Core.Remote;

public class McChamber : IMcChamber
{
    public string McInfo { get; private set; } = string.Empty;

    public IReadOnlyTemperaturePoint Temperature
    {
        get
        {
            try
            {
                var t = _client!.GetTemperature(new Request());
                ThrowIfError(t);
                return new TemperaturePoint(DateTime.Now, t.Monitored, t.Target);
            }
            catch
            {
                var t = _client!.GetTemperature(new Request());
                ThrowIfError(t);
                return new TemperaturePoint(DateTime.Now, t.Monitored, t.Target);
            }
        }
    }

    public void Connect(string address)
    {
        _channel?.Dispose();
        _channel = GrpcChannel.ForAddress(address);

        _client = new McService.McService.McServiceClient(_channel);
    }

    public void Open(string port, int channel)
    {
        ThrowIfError(_client!.Open(new OpenRequest { PortName = port, Channel = channel }));

        var reply = _client!.GetInfo(new Request());
        ThrowIfError(reply);
        McInfo = reply.Info switch
        {
            "T,SCP220,110.0" => "MC711",
            "T,SCP220,190.0" => "MC811",
            "T,P-300,190.0" => "MC812",
            _ => "Unknown Chamber"
        };
    }

    public void Open(string port)
    {
        Open(port, -1);
    }

    public void Close()
    {
        ThrowIfError(_client!.Close(new Request()));
    }

    public void TurnOn()
    {
        ThrowIfError(_client!.TurnOn(new Request()));
    }

    public void TurnOff()
    {
        ThrowIfError(_client!.TurnOff(new Request()));
    }

    public void GoTemp(double temp, McRefMode refMode)
    {
        ThrowIfError(_client!.GoTemp(new GoTempRequest
        {
            Temperature = temp,
            RefMode = refMode switch
            {
                McRefMode.RefModeAuto => RefMode.Auto,
                McRefMode.RefModeOn => RefMode.On,
                McRefMode.RefModeOff => RefMode.Off,
                _ => RefMode.None
            }
        }));
    }

    public void GoTemp(double temp, TimeSpan time, McRefMode refMode)
    {
        ThrowIfError(_client!.GoTempLinerial(new GoTempLinerialRequest
        {
            Temperature = temp,
            Time = (int) time.TotalSeconds,
            RefMode = refMode switch
            {
                McRefMode.RefModeAuto => RefMode.Auto,
                McRefMode.RefModeOn => RefMode.On,
                McRefMode.RefModeOff => RefMode.Off,
                _ => RefMode.None
            }
        }));
    }

    private static void ThrowIfError(Reply reply)
    {
        if (reply.Status != 0)
        {
            throw new Exception(reply.Error);
        }
    }

    private static void ThrowIfError(InfoReply reply)
    {
        if (reply.Status != 0)
        {
            throw new Exception(reply.Error);
        }
    }

    private static void ThrowIfError(TemperatureReply reply)
    {
        if (reply.Status != 0)
        {
            throw new Exception(reply.Error);
        }
    }

    private GrpcChannel? _channel;
    private McService.McService.McServiceClient? _client;
}