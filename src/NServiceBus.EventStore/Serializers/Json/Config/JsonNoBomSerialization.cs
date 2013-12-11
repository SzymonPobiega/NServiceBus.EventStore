using NServiceBus.Transports.EventStore.Serializers.Json;

namespace NServiceBus.Features
{
    using MessageInterfaces.MessageMapper.Reflection;
    using Serializers.Json;
    using Settings;

    public class JsonNoBomSerialization : Feature<Categories.Serializers>
    {
        public override void Initialize()
        {
            Configure.Component<MessageMapper>(DependencyLifecycle.SingleInstance);
            Configure.Component<JsonNoBomMessageSerializer>(DependencyLifecycle.SingleInstance)
                 .ConfigureProperty(s => s.SkipArrayWrappingForSingleMessages, !SettingsHolder.GetOrDefault<bool>("SerializationSettings.WrapSingleMessages"));
        }
    }
}