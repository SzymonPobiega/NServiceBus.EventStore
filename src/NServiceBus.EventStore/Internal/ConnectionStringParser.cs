using System;
using System.Collections.Generic;
using System.Linq;
using EventStore.ClientAPI.SystemData;

namespace NServiceBus.Internal
{
    public class ConnectionStringParser : IConnectionStringParser
    {
        private static readonly List<PropertyParser> propertyParsers
            = new List<PropertyParser>()
                {
                    new PropertyParser((p, b) => b.Name = p["name"], "name"),
                    new PropertyParser((p, b) => b.ConnectionSettings.SetDefaultUserCredentials(new UserCredentials(p["user"],p["password"])), "user","password"),

                    new PropertyParser((p, b) => b.ConnectionSettings.EnableVerboseLogging(), "verboseLogging"),
                    new PropertyParser((p, b) => b.ConnectionSettings.KeepRetrying(), "keepRetrying"),
                    new PropertyParser((p, b) => b.ConnectionSettings.KeepReconnecting(), "keepReconnecting"),
                    new PropertyParser((p, b) => b.ConnectionSettings.PerformOnMasterOnly(), "readFromMaster"),
                    new PropertyParser((p, b) => b.ConnectionSettings.PerformOnAnyNode(), "readFromAny"),

                    new PropertyParser((p, b) => b.ConnectionSettings.LimitOperationsQueueTo(int.Parse(p["operationQueue"])), "operationQueue"),
                    new PropertyParser((p, b) => b.ConnectionSettings.LimitConcurrentOperationsTo(int.Parse(p["maxConcurrentOperations"])), "maxConcurrentOperations"),
                    new PropertyParser((p, b) => b.SingleNodeAddress = p["singleNode"], "singleNode"),
                    new PropertyParser((p, b) => b.SingleNodePort = int.Parse(p["port"]), "singleNode", "port"),
                    new PropertyParser((p, b) => b.HttpEndpointAddress = p["httpAddress"], "httpAddress"),
                    new PropertyParser((p, b) => b.HttpPort = int.Parse(p["httpPort"]), "httpPort"),
                    new PropertyParser((p, b) => b.ConnectionSettings.LimitRetriesForOperationTo(int.Parse(p["maxRetries"])), "maxRetries"),
                    new PropertyParser((p, b) => b.ConnectionSettings.LimitReconnectionsTo(int.Parse(p["maxReconnections"])), "maxReconnections"),
                    new PropertyParser((p, b) => b.ConnectionSettings.SetReconnectionDelayTo(TimeSpan.Parse(p["receonnectionDelay"])), "receonnectionDelay"),
                    new PropertyParser((p, b) => b.ConnectionSettings.SetOperationTimeoutTo(TimeSpan.Parse(p["operationTimeout"])), "operationTimeout"),
                    new PropertyParser((p, b) => b.ConnectionSettings.UseSslConnection(p["sslHost"],p.ContainsKey("sslValidate")), "sslHost"),
                };

        public ConnectionConfiguration Parse(string connectionString)
        {
            var builder = new ConnectionConfigurationBuilder();
            Parse(connectionString, builder);
            return builder.Build();
        }

        public void Parse(string connectionString, ConnectionConfigurationBuilder builder)
        {
            var settingsDictionary =
                connectionString.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries)
                                .Select(x => x.Trim(new[] {' '}))
                                .Select(x => x.Split(new[] {'='}, StringSplitOptions.RemoveEmptyEntries))
                                .Where(x => x.Length == 1 || x.Length == 2)
                                .ToDictionary(x => x[0], x => x.Length == 1 ? null : x[1]);

            foreach (var parser in propertyParsers)
            {
                parser.Parse(settingsDictionary, builder);
            }
        }

        public class PropertyParser
        {
            private readonly string[] requiredParameters;
            private readonly Action<Dictionary<string, string>, ConnectionConfigurationBuilder> configAction;

            public PropertyParser(Action<Dictionary<string, string>, ConnectionConfigurationBuilder> configAction, params string[] requiredParameters)
            {
                this.requiredParameters = requiredParameters;
                this.configAction = configAction;
            }

            public void Parse(Dictionary<string, string> parameters, ConnectionConfigurationBuilder builder)
            {
                if (requiredParameters.All(parameters.ContainsKey))
                {
                    configAction(parameters, builder);
                }
            }
        }
    }
}