namespace NServiceBus.EventSourcing
{
    class OutgoingMessage
    {
        public object Message { get; }
        public SendOptions SendOptions { get; }

        public OutgoingMessage(object message, SendOptions sendOptions)
        {
            Message = message;
            SendOptions = sendOptions;
        }
    }
}