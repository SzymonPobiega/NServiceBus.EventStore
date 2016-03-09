namespace NServiceBus.Internal
{
    public interface IRegisterProjections
    {
        void RegisterProjectionsFor(string account);
    }
}