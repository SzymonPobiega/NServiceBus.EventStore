using System;
using System.Linq;
using NServiceBus.Transports.EventStore.Projections;

namespace NServiceBus.Transports.EventStore
{
    public abstract class AbstractProjectionCreator : IRegisterProjections
    {
        private const string ByCategoryProjection = "$by_category";
        protected abstract string GetQuery();
        protected abstract string GetName();

        public IManageEventStoreConnections ConnectionManager { get; set; }

        public void RegisterProjectionsFor(string account)
        {
            var projectionManager = ConnectionManager.GetProjectionManager();
            var byCategory = projectionManager.GetStatus(ByCategoryProjection);
            if (byCategory.StatusEnum == ManagedProjectionState.Stopped)
            {
                projectionManager.Enable(ByCategoryProjection);
            }
            var projectionName = GetName();

            var projectionList = projectionManager.List();
            if (projectionList.All(x => x.Name != projectionName))
            {
                var query = GetQuery();
                try
                {
                    projectionManager.CreateContinuous(projectionName, query);
                }
                catch (Exception)
                {
                    if (projectionList.All(x => x.Name != projectionName)) //Still not created
                    {
                        throw;
                    }
                }
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