using System;
using System.Collections.Generic;
using System.Linq;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.SystemData;
using NServiceBus.Transports.EventStore.Serializers.Json;

namespace NServiceBus.Transports.EventStore.Projections
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
            return projectionsManager.GetQuery(projectionName, userCredentials);
        }

        public void CreateContinuous(string projectionName, string query)
        {
            projectionsManager.CreateContinuous(projectionName, query, userCredentials);
        }

        public void UpdateQuery(string projectionName, string newQuery)
        {
            projectionsManager.UpdateQuery(projectionName, newQuery, userCredentials);
        }

        public void Delete(string projectionName)
        {
            projectionsManager.Delete(projectionName, userCredentials);
        }

        public ProjectionInfo GetStatus(string projectionName)
        {
            var rawStatus = projectionsManager.GetStatus(projectionName, userCredentials);
            return rawStatus.ParseJson<ProjectionInfo>();
        }

        public IList<ProjectionInfo> List()
        {
            var rawList = projectionsManager.ListAll(userCredentials);
            return rawList.ParseJson<ProjectionList>().Projections;
        }

        public void Enable(string projectionName)
        {
            try
            {
                projectionsManager.Enable(projectionName, userCredentials);
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