namespace NServiceBus.Internal
{
    public interface IConnectionStringParser
    {
        ConnectionConfiguration Parse(string connectionString);
    }
}