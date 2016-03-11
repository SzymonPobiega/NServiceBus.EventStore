using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;

namespace NServiceBus.Internal.Projections
{
    public class DefaultProjectionsManager : IProjectionsManager
    {
        private readonly ProjectionsManager projectionsManager;
        private readonly UserCredentials userCredentials;

        public DefaultProjectionsManager(ProjectionsManager projectionsManager, UserCredentials userCredentials)
        {
            this.projectionsManager = projectionsManager;
            this.userCredentials = userCredentials;
        }

        public async Task<bool> Exists(string projectionName)
        {
            return (await List().ConfigureAwait(false)).Any(x => x.Name == projectionName);
        }

        public Task<string> GetQuery(string projectionName)
        {            
            return projectionsManager.GetQueryAsync(projectionName, userCredentials);
        }

        public Task CreateContinuous(string projectionName, string query)
        {
            return projectionsManager.CreateContinuousAsync(projectionName, query, userCredentials);
        }

        public Task UpdateQuery(string projectionName, string newQuery)
        {
            return projectionsManager.UpdateQueryAsync(projectionName, newQuery, userCredentials);
        }

        public Task Delete(string projectionName)
        {
            return projectionsManager.DeleteAsync(projectionName, userCredentials);
        }

        public async Task<ProjectionInfo> GetStatus(string projectionName)
        {
            var rawStatus = await projectionsManager.GetStatusAsync(projectionName, userCredentials);
            return rawStatus.ParseJson<ProjectionInfo>();
        }

        public async Task<IList<ProjectionInfo>> List()
        {
            var rawList = await projectionsManager.ListAllAsync(userCredentials).ConfigureAwait(false);
            return rawList.Select(x => new ProjectionInfo {Name = x.Name, Status = x.Status}).ToList();
        }

        public Task Stop(string projectionName)
        {
            return projectionsManager.DisableAsync(projectionName, userCredentials);
        }

        public async Task Enable(string projectionName)
        {
            try
            {
                await projectionsManager.EnableAsync(projectionName, userCredentials);
            }
            catch (Exception)
            {
                var projections = await List();
                if (projections.Any(x => x.Name == projectionName 
                    && (x.StatusEnum == ManagedProjectionState.Running || x.StatusEnum == ManagedProjectionState.Starting)))
                {
                    return;
                }
                throw;
            }
        }
    }
}