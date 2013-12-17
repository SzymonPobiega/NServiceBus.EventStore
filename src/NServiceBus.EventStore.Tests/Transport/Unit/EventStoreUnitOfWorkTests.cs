using System;
using System.Collections.Generic;
using System.Transactions;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using NServiceBus.Transports;
using NServiceBus.Transports.EventStore;
using NUnit.Framework;
using Rhino.Mocks;

namespace NServiceBus.AddIn.Tests.Unit
{
    [TestFixture]
    public class EventStoreUnitOfWorkTests
    {
        [Test]
        public void It_does_not_send_events_to_store_before_transaction_is_committed()
        {
            var connectionMock = MockRepository.GenerateMock<IEventStoreConnection>();
            connectionMock.Expect(x => x.AppendToStream(Arg<string>.Is.Anything,
                Arg<int>.Is.Anything,
                Arg<EventData[]>.Is.Anything)).Repeat.Never();

            var connectionManager = new FakeConnectionManager()
                {
                    Connection = connectionMock
                };
            var uow = new TransactionalUnitOfWork(connectionManager)
                {
                    EndpointAddress = new Address("q","m")
                };

            using (new TransactionScope())
            {
                uow.Send(new EventData(Guid.NewGuid(), "", false, new byte[0], new byte[0]));
            }

            connectionMock.VerifyAllExpectations();
        }

        [Test]
        public void It_sends_all_messages_at_once_after_transaction_is_committed()
        {
            var connectionMock = MockRepository.GenerateMock<IEventStoreConnection>();
            connectionMock.Expect(x => x.AppendToStream(Arg<string>.Is.Anything,
                Arg<int>.Is.Anything,
                Arg<IEnumerable<EventData>>.Is.Anything, Arg<UserCredentials>.Is.Anything)).Repeat.Once().Return(new WriteResult());

            var connectionManager = new FakeConnectionManager()
            {
                Connection = connectionMock
            };
            var uow = new TransactionalUnitOfWork(connectionManager)
            {
                EndpointAddress = new Address("q", "m")
            };

            using (var tx = new TransactionScope())
            {
                uow.Send(new EventData(Guid.NewGuid(), "", false, new byte[0], new byte[0]));
                uow.Send(new EventData(Guid.NewGuid(), "", false, new byte[0], new byte[0]));
                tx.Complete();
            }

            connectionMock.VerifyAllExpectations();
        }
    }
}