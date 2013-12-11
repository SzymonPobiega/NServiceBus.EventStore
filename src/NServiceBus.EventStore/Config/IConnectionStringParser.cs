namespace NServiceBus.Transports.EventStore.Config
{
    public interface IConnectionStringParser
    {
        ConnectionConfiguration Parse(string connectionString);
    }
}