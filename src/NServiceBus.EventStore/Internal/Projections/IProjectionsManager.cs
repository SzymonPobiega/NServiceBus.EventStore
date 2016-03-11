using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.ClientAPI.Projections;

namespace NServiceBus.Internal.Projections
{
    public interface IProjectionsManager
    {
        Task<bool> Exists(string projectionName);
        Task<string> GetQuery(string projectionName);
        Task CreateContinuous(string projectionName, string query);
        Task UpdateQuery(string projectionName, string newQuery);
        Task Delete(string projectionName);
        Task<ProjectionInfo> GetStatus(string projectionName);
        Task<IList<ProjectionInfo>> List();
        Task Stop(string projectionName);
        Task Enable(string projectionName);
    }
}