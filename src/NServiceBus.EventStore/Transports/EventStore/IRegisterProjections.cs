namespace NServiceBus.Transports.EventStore
{
    public interface IRegisterProjections
    {
        void RegisterProjectionsFor(string account);
    }
}