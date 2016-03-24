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
        public static PersistenceExtentions<EventStorePersistence> ConnectionString(this PersistenceExtentions<EventStorePersistence> persistence, string connectionString)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }
            var config = new ConnectionStringParser().Parse(connectionString);
            persistence.GetSettings().Set(SynchronizedStorageFeature.ConnectionConfigurationSettingsKey, config);
            return persistence;
        }

        /// <summary>
        /// Forces the peristence to not reuse the EventStore transport connection string and use its own.
        /// </summary>
        /// <param name="persistence">Persistence extensions.</param>
        /// <param name="connectionConfiguration">Connection configuration.</param>
        public static PersistenceExtentions<EventStorePersistence> ConnectionConfiguration(this PersistenceExtentions<EventStorePersistence> persistence, ConnectionConfiguration connectionConfiguration)
        {
            if (persistence == null)
            {
                throw new ArgumentNullException(nameof(persistence));
            }
            persistence.GetSettings().Set(SynchronizedStorageFeature.ConnectionConfigurationSettingsKey, connectionConfiguration);
            return persistence;
        }
    }
}