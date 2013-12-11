using System;
using NServiceBus.MessageInterfaces;
using Newtonsoft.Json.Serialization;

namespace NServiceBus.Transports.EventStore.Serializers.Json.Internal
{
    public class MessageContractResolver : DefaultContractResolver
    {
#pragma warning disable 612,618
        private readonly IMessageMapper _messageMapper;
#pragma warning restore 612,618

#pragma warning disable 612,618
        public MessageContractResolver(IMessageMapper messageMapper)
#pragma warning restore 612,618
            : base(true)
        {
            _messageMapper = messageMapper;
        }

        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            var mappedTypeFor = _messageMapper.GetMappedTypeFor(objectType);

            if (mappedTypeFor == null)
                return base.CreateObjectContract(objectType);

            var jsonContract = base.CreateObjectContract(mappedTypeFor);
            jsonContract.DefaultCreator = () => _messageMapper.CreateInstance(mappedTypeFor);

            return jsonContract;
        }
    }
}