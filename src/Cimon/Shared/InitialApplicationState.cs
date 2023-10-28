namespace Cimon.Shared;

public enum AppClientType
{
    Web,
    Electron
}

public class InitialApplicationState
{
    public AppClientType ClientType { get; set; }
}