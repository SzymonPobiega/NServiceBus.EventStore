using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using NServiceBus.Transports.EventStore.Projections;

namespace NServiceBus.Transports.EventStore
{
    public class SubscriptionManager : IManageSubscriptions
    {
        const string ProjectionTemplate = @"options({{
    reorderEvents: false
}});
fromStreams([{0}])
.when({{
{1}
}})";

        private const string EventTemplate = @"'{0}'/*{1}*/: function (s, e) {{
        linkTo('{1}',e);
    }}";

        private static readonly Regex streamsExpression = new Regex(@"fromStreams\(\[([^)]+)\]\)", RegexOptions.Compiled);
        private static readonly Regex typesExpression = new Regex(@"'([^']+)'/\*([^*]+)\*/: function \(s, e\)", RegexOptions.Compiled);

        public Address EndpointAddress { get; set; }

        private readonly IManageEventStoreConnections connectionManager;

        public SubscriptionManager(IManageEventStoreConnections connectionManager)
        {
            this.connectionManager = connectionManager;
        }

        public void Subscribe(Type eventType, Address publisherAddress)
        {
            var typeName = eventType.AssemblyQualifiedName;
            var publisherOutgoingQueue = publisherAddress.GetFinalOutgoingQueue();
            var projectionManager = connectionManager.GetProjectionManager();
            var projectionName = EndpointAddress.GetSubscriptionProjectionName();
            List<string> streams;
            List<Tuple<string, string>> types;
            LoadAndParseQuery(projectionManager, projectionName, out streams, out types);
            if (!streams.Contains(publisherOutgoingQueue))
            {
                streams.Add(publisherOutgoingQueue);
            }
            var typeAndStream = Tuple.Create(typeName, EndpointAddress.GetReceiveAddressFrom(publisherAddress));
            if (!types.Contains(typeAndStream))
            {
                types.Add(typeAndStream);
            }
            CreateOrUpdateQuery(projectionManager, projectionName, streams, types);
        }

        public void Unsubscribe(Type eventType, Address publisherAddress)
        {
            var typeName = eventType.FullName;
            var publisherOutgoingQueue = publisherAddress.GetFinalOutgoingQueue();
            var projectionManager = connectionManager.GetProjectionManager();
            var projectionName = EndpointAddress.GetSubscriptionProjectionName();
            List<string> streams;
            List<Tuple<string, string>> types;
            LoadAndParseQuery(projectionManager, projectionName, out streams, out types);
            var receiveAddress = EndpointAddress.GetReceiveAddressFrom(publisherAddress);
            var typeAndStream = Tuple.Create(typeName, receiveAddress);
            types.Remove(typeAndStream);
            if (types.All(x => x.Item2 != receiveAddress))
            {
                streams.Remove(publisherOutgoingQueue);
            }
            CreateOrUpdateQuery(projectionManager, projectionName, streams, types);
        }

        private void CreateOrUpdateQuery(IProjectionsManager projectionManager, string projectionName, IList<string> streams, IList<Tuple<string, string>> types)
        {
            var newQuery = RenderQuery(streams, types);
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
                    //projectionManager.Enable(projectionName);
                }
                else
                {
                    projectionManager.Delete(projectionName);
                }
            }
        }

        private string RenderQuery(IList<string> streams, IList<Tuple<string, string>> types)
        {
            if (streams.Count == 0 && types.Count == 0)
            {
                return null;
            }
            var streamsQuoted = streams.Select(x => "'" + x + "'");
            var newStreamsString = string.Join(",", streamsQuoted);
            var typesString = string.Join(","+Environment.NewLine,
                                          types.Select(
                                              x => string.Format(EventTemplate, x.Item1, x.Item2)));
            var newQuery = string.Format(ProjectionTemplate, newStreamsString, typesString);
            return newQuery;
        }

        private void LoadAndParseQuery(IProjectionsManager projectionManager, string projectionName, out List<string> streams, out List<Tuple<string, string>> types)
        {
            if (projectionManager.Exists(projectionName))
            {
                var query = projectionManager.GetQuery(projectionName);
                var streamsString = streamsExpression.Match(query).Groups[1].Value;
                streams = streamsString
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim(' ', '\''))
                    .ToList();
                var matches = typesExpression.Matches(query);
                types = matches.Cast<Match>().Select(m => Tuple.Create(m.Groups[1].Value, m.Groups[2].Value)).ToList();
            }
            else
            {
                streams = new List<string>();
                types = new List<Tuple<string, string>>();
            }
        }
    }
}