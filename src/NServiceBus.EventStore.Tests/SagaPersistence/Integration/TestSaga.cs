using System;
using NServiceBus.Saga;

namespace NServiceBus.AddIn.Tests.SagaPersistence.Integration
{
    public class TestSaga : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        public Guid GuidField { get; set; }
        public string StringField { get; set; }
        public int IntField { get; set; }
        public DateTime DateField { get; set; }
    }
}