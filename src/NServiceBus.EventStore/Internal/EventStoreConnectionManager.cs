using System;
using System.Configuration;
using NServiceBus.Features;

namespace NServiceBus.Internal
{
    internal class EventStoreConnectionManager : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            var connectionStringElement = ConfigurationManager.ConnectionStrings["NServiceBus/EventStore"];
            string connectionString;
            if (connectionStringElement == null || string.IsNullOrEmpty(connectionStringElement.ConnectionString))
            {
                connectionString = DefaultConnectionString;
                //throw new InvalidOperationException(string.Format(ErrorMessage, GetConfigFileIfExists()));
            }
            else
            {
                connectionString = connectionStringElement.ConnectionString;
            }
            var connectionConfiguration = new ConnectionStringParser().Parse(connectionString);

            context.Container.RegisterSingleton<IConnectionConfiguration>(connectionConfiguration);
            context.Container.ConfigureComponent<DefaultConnectionManager>(DependencyLifecycle.SingleInstance);
        }

        static string GetConfigFileIfExists()
        {
            return AppDomain.CurrentDomain.SetupInformation.ConfigurationFile ?? "App.config";
        }

        private const string ErrorMessage = @"No default connection string found in your config file ({0}) for the EventStore connection.

To run NServiceBus with EventStore transport and/or persistence you need to specify the database connectionstring.
Here is an example of what is required:
  
  <connectionStrings>
    <add name=""NServiceBus/EventStore"" connectionString=""singleNode=127.0.0.1;user=admin;password=changeit"" />
  </connectionStrings>";
        private const string DefaultConnectionString = @"singleNode=127.0.0.1;user=admin;password=changeit";
    }
}