using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using maxbl4.Infrastructure;
using maxbl4.Race.Logic.CheckpointService.Client;
using maxbl4.Race.Logic.WsHub;
using maxbl4.Race.Logic.WsHub.Messages;
using maxbl4.Race.WsHub;
using Serilog;
using Serilog.Core;
using Serilog.Core.Enrichers;
using Xunit;
using Xunit.Abstractions;

namespace maxbl4.Race.Tests.WsHub
{
    public class WsHubTests: IntegrationTestBase
    {
        public WsHubTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public async Task Send_simple_message()
        {
            using var svc = CreateWsHubService();
            using var cli1 = new WsClientTestWrapper(svc.ListenUri, "cli1");
            await cli1.Connect();
            await cli1.ExpectConnected();
            using var cli2 = new WsClientTestWrapper(svc.ListenUri, "cli2");
            await cli2.Connect();
            await cli2.ExpectConnected();

            await cli1.Client.SendTo("cli2", new TestMessage {Payload = "some"});
            await new Timing()
                .Logger(Logger)
                .ExpectAsync(() => cli2.ClientMessages.OfType<TestMessage>().Any(x => x.Payload == "some"));
            cli1.ClientMessages.Should().BeEmpty();
            
            await cli2.Client.SendTo("cli1", new TestMessage {Payload = "222"});
            await new Timing()
                .Logger(Logger)
                .ExpectAsync(() => cli1.ClientMessages.OfType<TestMessage>().Any(x => x.Payload == "222"));
            cli2.ClientMessages.Should().HaveCount(1);
        }
        
        [Fact]
        public async Task Send_to_multiple_clients_of_same_user()
        {
            using var svc = CreateWsHubService();
            using var sender = new WsClientTestWrapper(svc.ListenUri, "sender");
            await sender.Connect();
            await sender.ExpectConnected();

            var tasks = Enumerable.Range(0, 5).Select(async x =>
            {
                var cli2 = new WsClientTestWrapper(svc.ListenUri, "receiver");
                await cli2.Connect();
                await cli2.ExpectConnected();
                return cli2;
            }).ToList();
            await Task.WhenAll(tasks);
            
            await sender.Client.SendTo("receiver", new TestMessage {Payload = "some"});
            for (var i = 0; i < 5; i++)
            {
                await new Timing()
                    .Logger(Logger)
                    .ExpectAsync(() => tasks[i].Result.ClientMessages.OfType<TestMessage>().Any(x => x.Payload == "some"));
            }
        }
    }

    public class WsClientTestWrapper: IDisposable
    {
        private readonly WsHubClient client;
        public string ClientId { get; }
        public List<MessageBase> ClientMessages { get; } = new List<MessageBase>();
        public List<WsConnectionStatus> ConnectionStatuses { get; } = new List<WsConnectionStatus>();

        public WsHubClient Client => client;

        public WsClientTestWrapper(string address, string clientId)
        {
            ClientId = clientId;
            client = new WsHubClient(address, clientId);
            Client.WebSocketConnected.Subscribe(ConnectionStatuses.Add);
            Client.Messages.Subscribe(ClientMessages.Add);
        }
        
        public Task Connect() => Client.Connect();
        
        public async Task ExpectConnected()
        {
            await new Timing()
                .Logger(Log.ForContext(new PropertyEnricher(Constants.SourceContextPropertyName, $"{nameof(WsClientTestWrapper)}: {ClientId}")))
                .ExpectAsync(() => ConnectionStatuses.LastOrDefault()?.IsConnected == true);
        }

        public void Dispose()
        {
            Client?.Dispose();
        }
    }
}