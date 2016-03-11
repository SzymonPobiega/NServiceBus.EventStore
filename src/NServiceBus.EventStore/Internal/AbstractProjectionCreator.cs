using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Internal.Projections;

namespace NServiceBus.Internal
{
    abstract class AbstractProjectionCreator : IRegisterProjections
    {
        private const string ByCategoryProjection = "$by_category";
        protected abstract string GetQuery();
        protected abstract string GetName();

        public async Task RegisterProjectionsFor(IProjectionsManager projectionManager)
        {
            var byCategory = await projectionManager.GetStatus(ByCategoryProjection);
            if (byCategory.StatusEnum == ManagedProjectionState.Stopped)
            {
                await projectionManager.Enable(ByCategoryProjection);
            }
            var projectionName = GetName();

            var projectionList = await projectionManager.List();
            if (projectionList.All(x => x.Name != projectionName))
            {
                var query = GetQuery();
                try
                {
                    await projectionManager.CreateContinuous(projectionName, query);
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
                var status = await projectionManager.GetStatus(projectionName);
                if (status.StatusEnum == ManagedProjectionState.Stopped)
                {
                    await projectionManager.Enable(projectionName);
                }
            }
        }
    }
}