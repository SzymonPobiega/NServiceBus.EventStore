using System;
using EventStore.ClientAPI;
using NServiceBus.Persistence;

namespace NServiceBus
{
    /// <summary>
    /// Provides extensions for using EventStore persistence.
    /// </summary>
    public static class EventStoreSynchronizedStorageSessionExtensions
    {
        /// <summary>
        /// Gets the connection to the event store.
        /// </summary>
        public static IEventStoreConnection Connection(this SynchronizedStorageSession session)
        {
            var typedSession = session as EventStoreSynchronizedStorageSession;
            if (typedSession == null)
            {
                throw new Exception("Connection can obly be obtained if EventStore persistence is used.");
            }
            return typedSession.Connection;
        }
    }
}