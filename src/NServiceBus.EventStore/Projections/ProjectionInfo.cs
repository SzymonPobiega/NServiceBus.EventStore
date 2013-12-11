using System;

namespace NServiceBus.Transports.EventStore.Projections
{
    public class ProjectionInfo
    {
        public string Status { get; set; }
        public string Name { get; set; }

        public ManagedProjectionState StatusEnum
        {
            get
            {
                ManagedProjectionState result;
                return Enum.TryParse(Status, out result) 
                    ? result 
                    : ManagedProjectionState.Undefined;
            }
        }
    }
}