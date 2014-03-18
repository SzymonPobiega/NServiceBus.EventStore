using System.Net;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using EventStore.ClientAPI.SystemData;
using EventStore.Core.Services;

namespace NServiceBus.AcceptanceTests.BasicMessaging
{
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_aborting_the_behavior_chain : NServiceBusAcceptanceTest
    {
        [Test]
        public void Subsequent_handlers_will_not_be_invoked()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<MyEndpoint>(b => b.Given(bus =>
                    {
                        var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 45072);
                        var projClient = new ProjectionsManager(new NoopLogger(), endpoint);
                        var projections = projClient.ListAll(new UserCredentials(SystemUsers.Admin, SystemUsers.DefaultAdminPassword));
                        bus.Send(Address.Local, new SomeMessage());
                    }))
                .Done(c => c.FirstHandlerInvoked)
                .Run();

            Assert.That(context.FirstHandlerInvoked, Is.True);
            Assert.That(context.SecondHandlerInvoked, Is.False);
        }

        public class Context : ScenarioContext
        {
            public bool FirstHandlerInvoked { get; set; }
            public bool SecondHandlerInvoked { get; set; }
        }

        [Serializable]
        public class SomeMessage : IMessage { }

        public class MyEndpoint : EndpointConfigurationBuilder
        {
            public MyEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class EnsureOrdering : ISpecifyMessageHandlerOrdering
            {
                public void SpecifyOrder(Order order)
                {
                    order.Specify(First<FirstHandler>.Then<SecondHandler>());
                }
            }

            class FirstHandler : IHandleMessages<SomeMessage>
            {
                public Context Context { get; set; }
                
                public IBus Bus { get; set; }
                
                public void Handle(SomeMessage message)
                {
                    Context.FirstHandlerInvoked = true;

                    Bus.DoNotContinueDispatchingCurrentMessageToHandlers();
                }
            }

            class SecondHandler : IHandleMessages<SomeMessage>
            {
                public Context Context { get; set; }
                
                public void Handle(SomeMessage message)
                {
                    Context.SecondHandlerInvoked = true;
                }
            }
        }
    }
}