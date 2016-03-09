using System;
using System.Collections.Generic;
using System.Linq;
using EventStore.ClientAPI;
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

        public bool Exists(string projectionName)
        {
            return List().Any(x => x.Name == projectionName);
        }

        public string GetQuery(string projectionName)
        {            
            return projectionsManager.GetQueryAsync(projectionName, userCredentials).Result;
        }

        public void CreateContinuous(string projectionName, string query)
        {
            projectionsManager.CreateContinuousAsync(projectionName, query, userCredentials).Wait();
        }

        public void UpdateQuery(string projectionName, string newQuery)
        {
            projectionsManager.UpdateQueryAsync(projectionName, newQuery, userCredentials).Wait();
        }

        public void Delete(string projectionName)
        {
            projectionsManager.DeleteAsync(projectionName, userCredentials).Wait();
        }

        public ProjectionInfo GetStatus(string projectionName)
        {
            var rawStatus = projectionsManager.GetStatusAsync(projectionName, userCredentials).Result;
            return rawStatus.ParseJson<ProjectionInfo>();
        }

        public IList<ProjectionInfo> List()
        {
            var rawList = projectionsManager.ListAllAsync(userCredentials).Result;
            return rawList.ParseJson<ProjectionList>().Projections;
        }

        public void Stop(string projectionName)
        {
            projectionsManager.DisableAsync(projectionName, userCredentials).Wait();
        }

        public void Enable(string projectionName)
        {
            try
            {
                projectionsManager.EnableAsync(projectionName, userCredentials).Wait();
            }
            catch (Exception)
            {
                var projections = List();
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