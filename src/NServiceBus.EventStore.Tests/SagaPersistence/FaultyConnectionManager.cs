using System;
using System.Linq;
using EventStore.ClientAPI;
using NServiceBus.Internal;
using NServiceBus.Internal.Projections;

namespace NServiceBus.EventStore.Tests.SagaPersistence
{
    public class FaultyConnectionManager : IManageEventStoreConnections
    {
        private int attempt;
        private readonly int[] _connectionAttemptsToFail;
        private readonly IManageEventStoreConnections realConnectionManager;

        public FaultyConnectionManager(IManageEventStoreConnections realConnectionManager, params int[] connectionAttemptsToFail)
        {
            this._connectionAttemptsToFail = connectionAttemptsToFail;
            this.realConnectionManager = realConnectionManager;
        }

        public IEventStoreConnection GetConnection()
        {
            attempt++;
            if (_connectionAttemptsToFail.Contains(attempt))
            {
                throw new Exception("Connection lost");
            }
            return realConnectionManager.GetConnection();
        }

        public IProjectionsManager GetProjectionManager()
        {
            throw new NotImplementedException();
        }
    }
}