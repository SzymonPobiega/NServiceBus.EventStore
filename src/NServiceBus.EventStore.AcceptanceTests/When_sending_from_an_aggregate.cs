namespace NServiceBus.AcceptanceTests.EventSourcing
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.EventSourcing;
    using NUnit.Framework;

    public class When_sending_from_an_aggregate : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_the_message()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<Client>(b => b.When((session, c) => session.Send(new MyRequest {Data = 1, Id = Guid.NewGuid()})))
                    .WithEndpoint<Server>()
                    .Done(c => c.LastResponse == 5)
                    .Run();

            Assert.AreEqual(5, context.LastResponse);
        }

        public class Context : ScenarioContext
        {
            public int LastResponse { get; set; }
        }

        public class Client : EndpointConfigurationBuilder
        {
            public Client()
            {
                EndpointSetup<DefaultServer>()
                    .AddMapping<MyRequest>(typeof(Server));
            }

            public class MyReplyHandler : IHandleMessages<MyReply>
            {
                public Context Context { get; set; }

                public  async Task Handle(MyReply message, IMessageHandlerContext context)
                {
                    Context.LastResponse = message.Data;
                    if (message.Data < 5)
                    {
                        await context.Reply(new MyRequest
                        {
                            Data = 1,
                            Id = message.Id
                        }).ConfigureAwait(false);
                    }
                }
            }
        }

        public class Server : EndpointConfigurationBuilder
        {
            public Server()
            {
                EndpointSetup<DefaultServer>(c => c.EnableOutbox())
                    .AddMapping<MyReply>(typeof(Client));
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public async Task Handle(MyRequest message, IMessageHandlerContext context)
                {
                    var repo = new Repository<MyAggregate>(context, "my-aggregate");

                    var instance = await repo.Load(message.Id).ConfigureAwait(false);

                    instance.ProcessRequest(message.Data);

                    await repo.Store(instance).ConfigureAwait(false);
                }
            }
        }

        public class MyRequest : IMessage
        {
            public Guid Id { get; set; }
            public int Data { get; set; }
        }

        public class MyReply : IMessage
        {
            public int Data { get; set; }
            public Guid Id { get; set; }
        }

        public class MyAggregate : Aggregate
        {
            public MyAggregate() : base(new MyPort())
            {
            }

            public void ProcessRequest(int data)
            {
                Emit(new RequestProcessedEvent
                {
                    Data = data
                });
            }

            public void Apply(RequestProcessedEvent e)
            {
                //NOOP
            }

            public class MyPort : Port
            {
                int accumulator;

                public void Apply(RequestProcessedEvent e)
                {
                    accumulator += e.Data;
                    Send(new MyReply
                    {
                        Data = accumulator,
                        Id = Id
                    }, new SendOptions());
                }
            }
        }

        public class RequestProcessedEvent
        {
            public int Data { get; set; } 
        }
    }
}
