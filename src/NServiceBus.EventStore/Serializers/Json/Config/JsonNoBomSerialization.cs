using NServiceBus.Features;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.Settings;

namespace NServiceBus.Transports.EventStore.Serializers.Json.Config
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