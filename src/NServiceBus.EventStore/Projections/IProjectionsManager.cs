using System.Collections.Generic;

namespace NServiceBus.Transports.EventStore.Projections
{
    public interface IProjectionsManager
    {
        bool Exists(string projectionName);
        string GetQuery(string projectionName);
        void CreateContinuous(string projectionName, string query);
        void UpdateQuery(string projectionName, string newQuery);
        void Delete(string projectionName);
        ProjectionInfo GetStatus(string projectionName);
        IList<ProjectionInfo> List();
        void Enable(string projectionName);
    }
}