using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.AcceptanceTests.ScenarioDescriptors;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Transports;

public class ConfigureScenariosForEventStoreTransport : IConfigureSupportedScenariosForTestExecution
{
    public IEnumerable<Type> UnsupportedScenarioDescriptorTypes { get; } = new[]
    {
        typeof(AllTransportsWithMessageDrivenPubSub),
        typeof(AllTransportsWithoutNativeDeferralAndWithAtomicSendAndReceive),
        typeof(AllTransportsWithoutNativeDeferral),
        typeof(AllDtcTransports),
        typeof(AllNativeMultiQueueTransactionTransports),
    };
}

public class ConfigureEndpointEventStoreTransport : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings)
    {
        queueBindings = configuration.GetSettings().Get<QueueBindings>();
        var connectionString = settings.Get<string>("Transport.ConnectionString");
        configuration.UseTransport<EventStoreTransport>().ConnectionString(connectionString).DisableExchangeCaching();
        configuration.UseSerialization<JsonSerializer>().Encoding(new UTF8Encoding(false));
        return Task.FromResult(0);
    }

    public async Task Cleanup()
    {
        var connection = EventStoreConnection.Create(new IPEndPoint(IPAddress.Loopback, 1113));

        await connection.ConnectAsync().ConfigureAwait(false);

        foreach (var receivingAddress in queueBindings.ReceivingAddresses)
        {
            await connection.DeleteStreamAsync(receivingAddress, ExpectedVersion.Any, false).ConfigureAwait(false);
        }

        await connection.DeleteStreamAsync("nsb-exchanges", ExpectedVersion.Any, false).ConfigureAwait(false);
    }

    QueueBindings queueBindings;
}