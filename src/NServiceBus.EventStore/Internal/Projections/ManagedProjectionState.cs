namespace NServiceBus.Internal.Projections
{
    public enum ManagedProjectionState
    {
        Creating,
        Loading,
        Loaded,
        Writing,
        Preparing,
        Prepared,
        Stopped,
        Completed,
        Faulted,
        Starting,
        LoadingState,
        Running,
        Stopping,
        Undefined,
    }
}