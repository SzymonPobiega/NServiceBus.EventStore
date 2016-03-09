using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using EventStore.ClientAPI;
using NServiceBus.Internal;

namespace NServiceBus.Transports.EventStore
{
    public class TransactionalUnitOfWork
    {
        public Address EndpointAddress { get; set; }

        private readonly IManageEventStoreConnections connectionManager;        

        public TransactionalUnitOfWork(IManageEventStoreConnections connectionManager)
        {
            this.connectionManager = connectionManager;            
        }

        public void Send(EventData message)
        {
            var transaction = Transaction.Current;
            var transactionId = transaction.TransactionInformation.LocalIdentifier;

            IList<EventData> messages;

            if (!OutstandingOperations.TryGetValue(transactionId, out messages))
            {
                messages = new List<EventData>();
                transaction.TransactionCompleted += CommitTransaction;
                OutstandingOperations.Add(transactionId, messages);
            }

            messages.Add(message);
        }

        private void CommitTransaction(object sender, TransactionEventArgs e)
        {
            var transactionInfo = e.Transaction.TransactionInformation;

            if (transactionInfo.Status != TransactionStatus.Committed)
            {
                OutstandingOperations.Clear();
                return;
            }

            var transactionId = transactionInfo.LocalIdentifier;

            if (!OutstandingOperations.ContainsKey(transactionId))
                return;

            var messages = OutstandingOperations[transactionId];

            if (!messages.Any())
                return;

            connectionManager.GetConnection()
                .AppendToStreamAsync(EndpointAddress.GetOutgoingStream(), ExpectedVersion.Any, messages).Wait();

            OutstandingOperations.Clear();
        }

        IDictionary<string, IList<EventData>> OutstandingOperations
        {
            get
            {
                return outstandingOperations ?? (outstandingOperations = new Dictionary<string, IList<EventData>>());
            }
        }

        //we use a dictionary to make sure that actions from other tx doesn't spill over if threads are getting reused by the hosting infrastructure
        [ThreadStatic]
        static IDictionary<string, IList<EventData>> outstandingOperations;
    }
}