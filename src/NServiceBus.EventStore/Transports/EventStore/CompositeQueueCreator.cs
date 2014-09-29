using System.Collections.Generic;

namespace NServiceBus.Transports.EventStore
{
    public class CompositeQueueCreator : ICreateQueues
    {
        private readonly IEnumerable<IRegisterProjections> queueCreators;

        public CompositeQueueCreator(IEnumerable<IRegisterProjections> queueCreators)
        {
            this.queueCreators = queueCreators;
        }

        public void CreateQueueIfNecessary(Address address, string account)
        {
            foreach (var creator in queueCreators)
            {
                creator.RegisterProjectionsFor(account);
            }
        }
    }
}