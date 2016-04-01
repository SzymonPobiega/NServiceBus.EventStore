using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace NServiceBus.EventStore.Tests
{
    class ChaosMonkeyConnectionAdapter : IEventStoreConnection
    {
        IEventStoreConnection connection;
        int successfulOperations;
        int counter;

        public ChaosMonkeyConnectionAdapter(IEventStoreConnection connection, int successfulOperations)
        {
            this.connection = connection;
            this.successfulOperations = successfulOperations;
        }

        public Task ConnectAsync()
        {
            return connection.ConnectAsync();
        }

        void IncrementAndCheckCounter()
        {
            counter++;
            if (counter > successfulOperations)
            {
                throw new ChaosException();
            }
        }

        public void Close()
        {
            connection.Close();
        }

        public Task<DeleteResult> DeleteStreamAsync(string stream, int expectedVersion, UserCredentials userCredentials = null)
        {
            IncrementAndCheckCounter();
            return connection.DeleteStreamAsync(stream, expectedVersion, userCredentials);
        }

        public Task<DeleteResult> DeleteStreamAsync(string stream, int expectedVersion, bool hardDelete, UserCredentials userCredentials = null)
        {
            IncrementAndCheckCounter();
            return connection.DeleteStreamAsync(stream, expectedVersion, hardDelete, userCredentials);
        }

        public Task<WriteResult> AppendToStreamAsync(string stream, int expectedVersion, params EventData[] events)
        {
            IncrementAndCheckCounter();
            return connection.AppendToStreamAsync(stream, expectedVersion, events);
        }

        public Task<WriteResult> AppendToStreamAsync(string stream, int expectedVersion, UserCredentials userCredentials, params EventData[] events)
        {
            IncrementAndCheckCounter();
            return connection.AppendToStreamAsync(stream, expectedVersion, userCredentials, events);
        }

        public Task<WriteResult> AppendToStreamAsync(string stream, int expectedVersion, IEnumerable<EventData> events, UserCredentials userCredentials = null)
        {
            IncrementAndCheckCounter();
            return connection.AppendToStreamAsync(stream, expectedVersion, events, userCredentials);
        }

        public Task<EventStoreTransaction> StartTransactionAsync(string stream, int expectedVersion, UserCredentials userCredentials = null)
        {
            IncrementAndCheckCounter();
            return connection.StartTransactionAsync(stream, expectedVersion, userCredentials);
        }

        public EventStoreTransaction ContinueTransaction(long transactionId, UserCredentials userCredentials = null)
        {
            IncrementAndCheckCounter();
            return connection.ContinueTransaction(transactionId, userCredentials);
        }

        public Task<EventReadResult> ReadEventAsync(string stream, int eventNumber, bool resolveLinkTos, UserCredentials userCredentials = null)
        {
            return connection.ReadEventAsync(stream, eventNumber, resolveLinkTos, userCredentials);
        }

        public Task<StreamEventsSlice> ReadStreamEventsForwardAsync(string stream, int start, int count, bool resolveLinkTos,
            UserCredentials userCredentials = null)
        {
            return connection.ReadStreamEventsForwardAsync(stream, start, count, resolveLinkTos, userCredentials);
        }

        public Task<StreamEventsSlice> ReadStreamEventsBackwardAsync(string stream, int start, int count, bool resolveLinkTos,
            UserCredentials userCredentials = null)
        {
            return connection.ReadStreamEventsBackwardAsync(stream, start, count, resolveLinkTos, userCredentials);
        }

        public Task<AllEventsSlice> ReadAllEventsForwardAsync(Position position, int maxCount, bool resolveLinkTos,
            UserCredentials userCredentials = null)
        {
            return connection.ReadAllEventsForwardAsync(position, maxCount, resolveLinkTos, userCredentials);
        }

        public Task<AllEventsSlice> ReadAllEventsBackwardAsync(Position position, int maxCount, bool resolveLinkTos,
            UserCredentials userCredentials = null)
        {
            return connection.ReadAllEventsBackwardAsync(position, maxCount, resolveLinkTos, userCredentials);
        }

        public Task<EventStoreSubscription> SubscribeToStreamAsync(string stream, bool resolveLinkTos, Action<EventStoreSubscription, ResolvedEvent> eventAppeared, Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            IncrementAndCheckCounter();
            return connection.SubscribeToStreamAsync(stream, resolveLinkTos, eventAppeared, subscriptionDropped, userCredentials);
        }

        public EventStoreStreamCatchUpSubscription SubscribeToStreamFrom(string stream, int? lastCheckpoint, bool resolveLinkTos,
            Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared, Action<EventStoreCatchUpSubscription> liveProcessingStarted = null, Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int readBatchSize = 500)
        {
            IncrementAndCheckCounter();
            return connection.SubscribeToStreamFrom(stream, lastCheckpoint, resolveLinkTos, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials, readBatchSize);
        }

        public Task<EventStoreSubscription> SubscribeToAllAsync(bool resolveLinkTos, Action<EventStoreSubscription, ResolvedEvent> eventAppeared, Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            IncrementAndCheckCounter();
            return connection.SubscribeToAllAsync(resolveLinkTos, eventAppeared, subscriptionDropped, userCredentials);
        }

        public EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(string stream, string groupName,
            Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared, Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null, int bufferSize = 10,
            bool autoAck = true)
        {
            IncrementAndCheckCounter();
            return connection.ConnectToPersistentSubscription(stream, groupName, eventAppeared, subscriptionDropped, userCredentials, bufferSize, autoAck);
        }

        public EventStoreAllCatchUpSubscription SubscribeToAllFrom(Position? lastCheckpoint, bool resolveLinkTos, Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared,
            Action<EventStoreCatchUpSubscription> liveProcessingStarted = null, Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null,
            int readBatchSize = 500)
        {
            IncrementAndCheckCounter();
            return connection.SubscribeToAllFrom(lastCheckpoint, resolveLinkTos, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials, readBatchSize);
        }

        public Task UpdatePersistentSubscriptionAsync(string stream, string groupName, PersistentSubscriptionSettings settings,
            UserCredentials credentials)
        {
            IncrementAndCheckCounter();
            return connection.UpdatePersistentSubscriptionAsync(stream, groupName, settings, credentials);
        }

        public Task CreatePersistentSubscriptionAsync(string stream, string groupName, PersistentSubscriptionSettings settings,
            UserCredentials credentials)
        {
            IncrementAndCheckCounter();
            return connection.CreatePersistentSubscriptionAsync(stream, groupName, settings, credentials);
        }

        public Task DeletePersistentSubscriptionAsync(string stream, string groupName, UserCredentials userCredentials = null)
        {
            IncrementAndCheckCounter();
            return connection.DeletePersistentSubscriptionAsync(stream, groupName, userCredentials);
        }

        public Task<WriteResult> SetStreamMetadataAsync(string stream, int expectedMetastreamVersion, StreamMetadata metadata,
            UserCredentials userCredentials = null)
        {
            return connection.SetStreamMetadataAsync(stream, expectedMetastreamVersion, metadata, userCredentials);
        }

        public Task<WriteResult> SetStreamMetadataAsync(string stream, int expectedMetastreamVersion, byte[] metadata,
            UserCredentials userCredentials = null)
        {
            IncrementAndCheckCounter();
            return connection.SetStreamMetadataAsync(stream, expectedMetastreamVersion, metadata, userCredentials);
        }

        public Task<StreamMetadataResult> GetStreamMetadataAsync(string stream, UserCredentials userCredentials = null)
        {
            return connection.GetStreamMetadataAsync(stream, userCredentials);
        }

        public Task<RawStreamMetadataResult> GetStreamMetadataAsRawBytesAsync(string stream, UserCredentials userCredentials = null)
        {
            return connection.GetStreamMetadataAsRawBytesAsync(stream, userCredentials);
        }

        public Task SetSystemSettingsAsync(SystemSettings settings, UserCredentials userCredentials = null)
        {
            IncrementAndCheckCounter();
            return connection.SetSystemSettingsAsync(settings, userCredentials);
        }

        public string ConnectionName
        {
            get { return connection.ConnectionName; }
        }

        public event EventHandler<ClientConnectionEventArgs> Connected
        {
            add { connection.Connected += value; }
            remove { connection.Connected -= value; }
        }

        public event EventHandler<ClientConnectionEventArgs> Disconnected
        {
            add { connection.Disconnected += value; }
            remove { connection.Disconnected -= value; }
        }

        public event EventHandler<ClientReconnectingEventArgs> Reconnecting
        {
            add { connection.Reconnecting += value; }
            remove { connection.Reconnecting -= value; }
        }

        public event EventHandler<ClientClosedEventArgs> Closed
        {
            add { connection.Closed += value; }
            remove { connection.Closed -= value; }
        }

        public event EventHandler<ClientErrorEventArgs> ErrorOccurred
        {
            add { connection.ErrorOccurred += value; }
            remove { connection.ErrorOccurred -= value; }
        }

        public event EventHandler<ClientAuthenticationFailedEventArgs> AuthenticationFailed
        {
            add { connection.AuthenticationFailed += value; }
            remove { connection.AuthenticationFailed -= value; }
        }

        public void Dispose()
        {
            connection.Dispose();
        }
    }
}