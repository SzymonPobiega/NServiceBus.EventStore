using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using NServiceBus.Transports.EventStore.Projections;
using NServiceBus.Unicast.Messages;

namespace NServiceBus.Transports.EventStore
{
    public class SubscriptionManager : IManageSubscriptions
    {
        const string ProjectionTemplate = @"options({{
    reorderEvents: false
}});
fromCategory('events')
.when({{
{1}
}})";

        private const string EventTemplate = @"'{0}': function (s, e) {{
        linkTo('{1}',e);
    }}";

        private static readonly Regex typesExpression = new Regex(@"'([^']+)': function \(s, e\)", RegexOptions.Compiled);

        public Address EndpointAddress { get; set; }

        private readonly IManageEventStoreConnections connectionManager;
        private readonly MessageMetadataRegistry metadataRegistry;

        public SubscriptionManager(IManageEventStoreConnections connectionManager, MessageMetadataRegistry metadataRegistry)
        {
            this.connectionManager = connectionManager;
            this.metadataRegistry = metadataRegistry;
        }

        public void Subscribe(Type eventType, Address publisherAddress)
        {
            try
            {
                DoSubscribe(eventType);
            }
            catch (Exception)
            {
                //Try once again
                DoSubscribe(eventType);
            }
        }

        private void DoSubscribe(Type eventType)
        {
            var projectionManager = connectionManager.GetProjectionManager();
            var projectionName = EndpointAddress.GetSubscriptionProjectionName();
            var types = LoadAndParseQuery(projectionManager, projectionName);

            var typeName = FormatTypeName(eventType);
            types.Add(typeName);
            CreateOrUpdateQuery(projectionManager, projectionName, types);
        }

        private static string FormatTypeName(Type eventType)
        {
            return eventType.AssemblyQualifiedName;
        }

        public void Unsubscribe(Type eventType, Address publisherAddress)
        {
            var projectionManager = connectionManager.GetProjectionManager();
            var projectionName = EndpointAddress.GetSubscriptionProjectionName();
            var types = LoadAndParseQuery(projectionManager, projectionName);
            var metadata = metadataRegistry.GetMessageDefinition(eventType);
            foreach (var type in metadata.MessageHierarchy)
            {
                var typeName = FormatTypeName(type);
                types.Remove(typeName);
            }
            CreateOrUpdateQuery(projectionManager, projectionName, types);
        }

        private void CreateOrUpdateQuery(IProjectionsManager projectionManager, string projectionName, IList<string> types)
        {
            var newQuery = RenderQuery(types);
            if (!projectionManager.Exists(projectionName))
            {
                if (newQuery != null)
                {
                    projectionManager.CreateContinuous(projectionName, newQuery);                    
                }
            }
            else
            {
                if (newQuery != null)
                {
                    projectionManager.UpdateQuery(projectionName, newQuery);
                }
                else
                {
                    projectionManager.Delete(projectionName);
                }
            }
        }

        private string RenderQuery(IList<string> types)
        {
            if (types.Count == 0 && types.Count == 0)
            {
                return null;
            }
            var streamsQuoted = types.Select(type => "'" + "events-" + type + "'");
            var newStreamsString = string.Join(",", streamsQuoted);
            var typesString = string.Join("," + Environment.NewLine, 
                types.Select(type => string.Format(EventTemplate, type, EndpointAddress.GetReceiveAddressFor(type))));
            var newQuery = string.Format(ProjectionTemplate, newStreamsString, typesString);
            return newQuery;
        }

        private IList<string> LoadAndParseQuery(IProjectionsManager projectionManager, string projectionName)
        {
            if (projectionManager.Exists(projectionName))
            {
                var query = projectionManager.GetQuery(projectionName);
                var matches = typesExpression.Matches(query);
                var result = matches.Cast<Match>().Select(m => m.Groups[1].Value).ToList();
                return result;
            }
            return new List<string>();
        }
    }
}