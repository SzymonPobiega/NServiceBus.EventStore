using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NServiceBus.Saga;
using NServiceBus.Transports.EventStore;

namespace NServiceBus.Persistence.EventStore.SagaPersister
{
    public class EventStoreSagaIndexer : IIndexSagas
    {
        private const string ProjectionTemplate = @"fromCategory('Saga_{0}')
.foreachStream()
.when({{
    SagaData : function(s,e) {{
        {1}
        s.saga = e.data;
        return s;
    }}
}})";
        private const string PropertyTemplate = @"if (typeof s.saga === 'undefined' || s.saga['{1}'] !== e.data['{1}']) {{
            linkTo('{0}_by_{2}-'+e.data['{1}'],e);
        }}";


        private readonly IManageEventStoreConnections connectionManager;

        public EventStoreSagaIndexer(IManageEventStoreConnections connectionManager)
        {
            this.connectionManager = connectionManager;
        }

        public void EnsureIndicesForProperties(IDictionary<Type, IDictionary<Type, KeyValuePair<PropertyInfo, PropertyInfo>>> sagaEntityToMessageToPropertyLookup)
        {
            foreach (var sagaEntityType in sagaEntityToMessageToPropertyLookup.Keys)
            {
                var propertiesToIndex = sagaEntityToMessageToPropertyLookup[sagaEntityType]
                    .Select(x => x.Value.Key)
                    .Distinct();
                CreateIndicesForSaga(sagaEntityType, propertiesToIndex);
            }
        }

        private void CreateIndicesForSaga(Type sagaEntityType, IEnumerable<PropertyInfo> propertiesToIndex)
        {
            var projectionManager = connectionManager.GetProjectionManager();
            var sagaName = sagaEntityType.FullName;
            var projectionName = string.Format("SagaIndex-" + sagaName);
            var propertyIndices = propertiesToIndex.Select(x => string.Format(PropertyTemplate, sagaName, x.Name.ToCamelCase(), x.Name));

            var query = string.Format(ProjectionTemplate, sagaName, string.Join(Environment.NewLine, propertyIndices));

            var projectionList = projectionManager.List();
            if (projectionList.All(x => x.Name != projectionName))
            {
                projectionManager.CreateContinuous(projectionName, query);
            }
            else
            {
                projectionManager.UpdateQuery(projectionName, query);                
            }
        }
    }
}