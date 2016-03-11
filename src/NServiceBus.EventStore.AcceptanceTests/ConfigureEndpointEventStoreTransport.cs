using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Threading.Tasks;
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
        configuration.UseTransport<EventStoreTransport>().ConnectionString(connectionString);
        configuration.UseSerialization<JsonSerializer>().Encoding(new UTF8Encoding(false));
        return Task.FromResult(0);
    }

    public Task Cleanup()
    {
        return Task.FromResult(0);
    }

    QueueBindings queueBindings;
}