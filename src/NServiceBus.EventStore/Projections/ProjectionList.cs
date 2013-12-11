using System.Collections.Generic;

namespace NServiceBus.Transports.EventStore.Projections
{
    public class ProjectionList
    {
        public List<ProjectionInfo> Projections { get; set; }
    }
}