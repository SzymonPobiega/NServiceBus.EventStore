using System;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using NServiceBus.Persistence;
using NServiceBus.Persistence.EventStore.SagaPersister;

namespace NServiceBus
{
    /// <summary>
    ///     Provides extensions for using EventStore persistence.
    /// </summary>
    public static class SynchronizedStorageSessionExtensions
    {
        /// <summary>
        /// Provide information about saga version for saga data returned from a custom finder.
        /// </summary>
        /// <param name="session">Session.</param>
        /// <param name="saga">Saga data.</param>
        /// <param name="version">Version returned from the store.</param>
        public static void ProvideSagaVersion(this SynchronizedStorageSession session, IContainSagaData saga, int version)
        {
            DowncastSession(session).StoreSagaVersion(saga.Id, new SagaVersion(version, false));
        }

        /// <summary>
        ///     Checks if atomic appends (via outbox) are enabled in this session.
        /// </summary>
        /// <param name="session">Session.</param>
        /// <returns></returns>
        public static bool SupportsAtomicQueueForStore(this SynchronizedStorageSession session)
        {
            return DowncastSession(session).SupportsAtomicAppend;
        }

        /// <summary>
        ///     Appends an event to the collection of events to be persisted at the end of message processing.
        /// </summary>
        public static void AtomicQueueForStore(this SynchronizedStorageSession session, string destinationStream, EventData e)
        {
            var outboxSession = session as OutboxEventStoreSynchronizedStorageSession;
            if (outboxSession == null)
            {
                throw new Exception("Atomic appends are not supported without the EventStore outbox.");
            }
            outboxSession.AtomicAppend(destinationStream, e);
        }

        static EventStoreSynchronizedStorageSession DowncastSession(SynchronizedStorageSession session)
        {
            var typedSession = session as EventStoreSynchronizedStorageSession;
            if (typedSession == null)
            {
                throw new Exception("Connection can obly be obtained if EventStore persistence is used.");
            }
            return typedSession;
        }

        /// <summary>
        ///     Appends an event to specified stream.
        /// </summary>
        public static Task AppendToStreamAsync(this SynchronizedStorageSession session, string streamId,
            int expectedVersion, params EventData[] e)
        {
            return DowncastSession(session).Connection.AppendToStreamAsync(streamId, expectedVersion, e);
        }

        /// <summary>
        ///     Reads count Events from an Event Stream forwards (e.g. oldest to newest) starting from position start
        /// </summary>
        /// <param name="session">Session.</param>
        /// <param name="stream">The stream to read from</param>
        /// <param name="start">The starting point to read from</param>
        /// <param name="count">The count of items to read</param>
        /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
        /// <returns>
        ///     A <see cref="T:System.Threading.Tasks.Task`1" /> containing the results of the read operation
        /// </returns>
        public static Task<StreamEventsSlice> ReadStreamEventsForwardAsync(this SynchronizedStorageSession session,
            string stream, int start, int count, bool resolveLinkTos)
        {
            return DowncastSession(session)
                .Connection.ReadStreamEventsForwardAsync(stream, start, count, resolveLinkTos);
        }

        /// <summary>
        ///     Reads count events from an Event Stream backwards (e.g. newest to oldest) from position asynchronously
        /// </summary>
        /// <param name="session">Session.</param>
        /// <param name="stream">The Event Stream to read from</param>
        /// <param name="start">The position to start reading from</param>
        /// <param name="count">The count to read from the position</param>
        /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
        /// <returns>
        ///     An <see cref="T:System.Threading.Tasks.Task`1" /> containing the results of the read operation
        /// </returns>
        public static Task<StreamEventsSlice> ReadStreamEventsBackwardAsync(this SynchronizedStorageSession session,
            string stream, int start, int count, bool resolveLinkTos)
        {
            return DowncastSession(session)
                .Connection.ReadStreamEventsBackwardAsync(stream, start, count, resolveLinkTos);
        }

        /// <summary>
        ///     Deletes a stream from the Event Store asynchronously
        /// </summary>
        /// <param name="session">Session.</param>
        /// <param name="stream">The name of the stream to delete.</param>
        /// <param name="expectedVersion">
        ///     The expected version that the streams should have when being deleted.
        ///     <see cref="T:EventStore.ClientAPI.ExpectedVersion" />
        /// </param>
        /// <param name="hardDelete">
        ///     Indicator for tombstoning vs soft-deleting the stream. Tombstoned streams can never be recreated. Soft-deleted
        ///     streams
        ///     can be written to again, but the EventNumber sequence will not start from 0.
        /// </param>
        /// <returns>
        ///     A <see cref="T:System.Threading.Tasks.Task" /> that can be awaited upon by the caller.
        /// </returns>
        public static Task<DeleteResult> DeleteStreamAsync(this SynchronizedStorageSession session, string stream,
            int expectedVersion, bool hardDelete)
        {
            return DowncastSession(session).Connection.DeleteStreamAsync(stream, expectedVersion, hardDelete);
        }
    }
}