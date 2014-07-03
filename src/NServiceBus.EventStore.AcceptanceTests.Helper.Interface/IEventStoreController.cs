namespace NServiceBus.EventStore.AcceptanceTests.Helper.Interface
{
    public interface IEventStoreController
    {
        void Start(int tcpPort, int httpPort);
        void Shutdown();
    }
}
