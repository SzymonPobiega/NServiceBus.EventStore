using System.Linq;
using NServiceBus.Transports.EventStore.Projections;

namespace NServiceBus.Transports.EventStore
{
    public abstract class AbstractProjectionCreator : IRegisterProjections
    {
        private const string ByCategoryProjection = "$by_category";
        protected abstract string GetQuery(Address address);
        protected abstract string GetName(Address address);

        public IManageEventStoreConnections ConnectionManager { get; set; }

        public void RegisterProjectionsFor(Address address, string account)
        {
            var projectionManager = ConnectionManager.GetProjectionManager();
            var byCategory = projectionManager.GetStatus(ByCategoryProjection);
            if (byCategory.StatusEnum == ManagedProjectionState.Stopped)
            {
                projectionManager.Enable(ByCategoryProjection);
            }
            var projectionName = GetName(address);

            var projectionList = projectionManager.List();
            if (projectionList.All(x => x.Name != projectionName))
            {
                var query = GetQuery(address);
                projectionManager.CreateContinuous(projectionName,query);
            }
            else
            {
                var status = projectionManager.GetStatus(projectionName);
                if (status.StatusEnum == ManagedProjectionState.Stopped)
                {
                    projectionManager.Enable(projectionName);
                }
            }
        }
    }
}