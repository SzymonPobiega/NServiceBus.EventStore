using System;
using System.Runtime.Serialization;
using NServiceBus.MessageInterfaces;

namespace NServiceBus.Transports.EventStore.Serializers.Json.Internal
{
    public class MessageSerializationBinder : SerializationBinder
    {
#pragma warning disable 612,618
        private readonly IMessageMapper _messageMapper;
#pragma warning restore 612,618

#pragma warning disable 612,618
        public MessageSerializationBinder(IMessageMapper messageMapper)
#pragma warning restore 612,618
        {
            _messageMapper = messageMapper;
        }

        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            var mappedType = _messageMapper.GetMappedTypeFor(serializedType) ?? serializedType;

            assemblyName = null;
            typeName = mappedType.AssemblyQualifiedName;
        }

        public override Type BindToType(string assemblyName, string typeName)
        {
          throw new NotImplementedException();
          //string resolvedTypeName = typeName + ", " + assemblyName;
          //return Type.GetType(resolvedTypeName, true);
        }
    }
}