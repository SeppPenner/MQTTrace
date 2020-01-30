using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTrace.Models;

namespace MQTTrace.Services
{
    public interface IMqttService
    {
        Task ConnectAsync();
        Task DisconnectAsync();
        bool Connected { get; }
        Task SubscribeAsync(string topic);
        List<ReceivedMessage> ReceivedMessages { get; }
        event Func<ReceivedMessage, Task> MessageReceived;
        Task PublishAsync(string topic, string payload);

    }

    public class MqttService : IMqttService
    {
        private readonly IMqttClient mqttClient;
        private int lastMessageId = -1;

        public MqttService(IMqttClient mqttClient)
        {
            this.mqttClient = mqttClient;

            mqttClient.UseApplicationMessageReceivedHandler(HandleApplicationMessagereceived);
        }

        public async Task ConnectAsync()
        {
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithClientId("MQTTrace")
                .WithTcpServer("test.mosquitto.org")
                .WithCleanSession()
                .Build();

            await mqttClient.ConnectAsync(mqttClientOptions);
        }

        public async Task DisconnectAsync()
        {
            await mqttClient.DisconnectAsync();
        }

        public bool Connected => mqttClient.IsConnected;

        public async Task SubscribeAsync(string topic)
        {
            await mqttClient.SubscribeAsync(topic);
        }

        public List<ReceivedMessage> ReceivedMessages { get; } = new List<ReceivedMessage>();

        private async Task HandleApplicationMessagereceived(MqttApplicationMessageReceivedEventArgs args)
        {
            var messageId = Interlocked.Increment(ref lastMessageId);

            var receivedMessage = new ReceivedMessage
            {
                MessageId = messageId,
                Timestamp = DateTime.Now,
                ClientId = args.ClientId,
                Message = args.ApplicationMessage,
            };

            ReceivedMessages.Add(receivedMessage);

            if (MessageReceived != null)
            {
                await MessageReceived.Invoke(receivedMessage);
            }
        }

        public event Func<ReceivedMessage, Task> MessageReceived;

        public async Task PublishAsync(string topic, string payload)
        {
            await mqttClient.PublishAsync(topic, payload);
        }
    }
}
