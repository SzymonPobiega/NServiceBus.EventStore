using System.Collections.Generic;
using NServiceBus.Internal.Projections;

namespace NServiceBus.AddIn.Tests
{
    public class FakeProjectionsManager : IProjectionsManager
    {
        public string Projection { get; set; }

        public bool Exists(string projectionName)
        {
            return true;
        }

        public string GetQuery(string projectionName)
        {
            return Projection;
        }

        public void CreateContinuous(string projectionName, string query)
        {
            Projection = query;
        }

        public void UpdateQuery(string projectionName, string newQuery)
        {
            Projection = newQuery;
        }

        public void Delete(string projectionName)
        {
            Projection = null;
        }

        public ProjectionInfo GetStatus(string projectionName)
        {
            throw new System.NotImplementedException();
        }

        public IList<ProjectionInfo> List()
        {
            throw new System.NotImplementedException();
        }

        public void Stop(string projectionName)
        {
        }

        public void Enable(string projectionName)
        {
        }
    }
}