using System;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Internal;

namespace NServiceBus
{
    /// <summary>
    /// Provides configuration for the EventStore persistence.
    /// </summary>
    public static class EventStorePersistenceExtensions
    {
        /// <summary>
        /// Forces the peristence to not reuse the EventStore transport connection string and use its own.
        /// </summary>
        /// <param name="persistence">Persistence extensions.</param>
        /// <param name="connectionString">Connection string.</param>
        public static PersistenceExtensions<EventStorePersistence> ConnectionString(this PersistenceExtensions<EventStorePersistence> persistence, string connectionString)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }
            var config = new ConnectionStringParser().Parse(connectionString);
            persistence.GetSettings().Set(OutboxPersisterFeature.ConnectionConfigurationSettingsKey, config);
            return persistence;
        }

        /// <summary>
        /// Forces the peristence to not reuse the EventStore transport connection string and use its own.
        /// </summary>
        /// <param name="persistence">Persistence extensions.</param>
        /// <param name="connectionConfiguration">Connection configuration.</param>
        public static PersistenceExtensions<EventStorePersistence> ConnectionConfiguration(this PersistenceExtensions<EventStorePersistence> persistence, ConnectionConfiguration connectionConfiguration)
        {
            if (persistence == null)
            {
                throw new ArgumentNullException(nameof(persistence));
            }
            persistence.GetSettings().Set(OutboxPersisterFeature.ConnectionConfigurationSettingsKey, connectionConfiguration);
            return persistence;
        }
    }
}