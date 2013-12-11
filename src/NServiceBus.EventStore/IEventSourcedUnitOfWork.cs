using EventStore.ClientAPI;

namespace NServiceBus.Transports.EventStore
{
    public interface IEventSourcedUnitOfWork
    {
        void Initialize(string aggregateId, int expectedVersion);
        bool IsInitialized { get; }
        void Publish(params EventData[] rawMessages);
    }
}