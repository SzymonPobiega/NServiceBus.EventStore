namespace NServiceBus.Transports.EventStore
{
    public interface IRegisterProjections
    {
        void RegisterProjectionsFor(Address address, string account);
    }
}