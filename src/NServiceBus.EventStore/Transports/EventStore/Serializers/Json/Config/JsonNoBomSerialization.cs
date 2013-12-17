using NServiceBus.Features;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.Settings;
using NServiceBus.Transports.EventStore.Serializers.Json;

namespace NServiceBus.Features
{
    public class JsonNoBomSerialization : Feature<Features.Categories.Serializers>
    {
        public override void Initialize()
        {
            Configure.Component<MessageMapper>(DependencyLifecycle.SingleInstance);
            Configure.Component<JsonNoBomMessageSerializer>(DependencyLifecycle.SingleInstance)
                 .ConfigureProperty(s => s.SkipArrayWrappingForSingleMessages, !SettingsHolder.GetOrDefault<bool>("SerializationSettings.WrapSingleMessages"));
        }
    }
}