namespace Chambers.Core;

public class ChamberConnectionException : Exception
{
    public ChamberConnectionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}