using EventStore.ClientAPI;
using NServiceBus.Transports.EventStore.Config;
using NUnit.Framework;

namespace NServiceBus.AddIn.Tests.Unit
{
    [TestFixture]
    public class ConnectionStringParserTests
    {
        [Test]
        public void It_can_parse_multiple_properties()
        {
            var result = Parse(@"name=Conn1;keepRetrying;singleNode=127.0.0.1");
            ConnectionSettings connectionSettings = result.ConnectionSettings;

            Assert.AreEqual("Conn1", result.Name);
            Assert.AreEqual(-1, connectionSettings.MaxRetries);
            Assert.AreEqual("127.0.0.1", result.SingleNodeAddress);
        }

        [Test]
        public void It_can_parse_name()
        {
            var result = Parse(@"name=Conn1");
            Assert.AreEqual("Conn1", result.Name);
        }

        [Test]
        public void It_can_parse_retry_settings()
        {
            var result = ParseConnectionSettings(@"keepRetrying");
            Assert.AreEqual(-1, result.MaxRetries);

            result = ParseConnectionSettings(@"maxRetries=77");
            Assert.AreEqual(77, result.MaxRetries);
        }
        
        [Test]
        public void It_can_parse_reconnect_settings()
        {
            var result = ParseConnectionSettings(@"keepReconnecting");
            Assert.AreEqual(-1, result.MaxReconnections);

            result = ParseConnectionSettings(@"maxReconnections=77");
            Assert.AreEqual(77, result.MaxReconnections);
        }

        [Test]
        public void It_can_parse_read_consistency_settings()
        {
            var result = ParseConnectionSettings(@"readFromMaster");
            Assert.IsTrue(result.RequireMaster);

            result = ParseConnectionSettings(@"readFromAny");
            Assert.IsFalse(result.RequireMaster);
        }

        [Test]
        public void It_can_parse_operations_settings()
        {
            var result = ParseConnectionSettings(@"operationQueue=77");
            Assert.AreEqual(77, result.MaxQueueSize);

            result = ParseConnectionSettings(@"maxConcurrentOperations=77");
            Assert.AreEqual(77, result.MaxConcurrentItems);
        }

        private static ConnectionConfigurationBuilder Parse(string connectionString)
        {
            var parser = new ConnectionStringParser();
            var builder = new ConnectionConfigurationBuilder();
            parser.Parse(connectionString, builder);
            return builder;
        }

        private static ConnectionSettings ParseConnectionSettings(string connectionString)
        {
            var parser = new ConnectionStringParser();
            var builder = new ConnectionConfigurationBuilder();
            parser.Parse(connectionString, builder);
            return builder.ConnectionSettings;
        }
    }
}