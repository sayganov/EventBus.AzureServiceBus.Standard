using System;
using Microsoft.Azure.ServiceBus;

namespace EventBus.AzureServiceBus.Standard
{
    public class DefaultServiceBusPersistentConnection : IServiceBusPersistentConnection
    {
        private ITopicClient _topicClient;

        private bool _disposed;

        public DefaultServiceBusPersistentConnection(ServiceBusConnectionStringBuilder serviceBusConnectionStringBuilder)
        {
            ServiceBusConnectionStringBuilder = serviceBusConnectionStringBuilder ?? throw new ArgumentNullException(nameof(serviceBusConnectionStringBuilder));
            _topicClient = new TopicClient(ServiceBusConnectionStringBuilder, RetryPolicy.Default);
        }

        public ServiceBusConnectionStringBuilder ServiceBusConnectionStringBuilder { get; }

        public ITopicClient CreateModel()
        {
            if (_topicClient.IsClosedOrClosing) _topicClient = new TopicClient(ServiceBusConnectionStringBuilder, RetryPolicy.Default);

            return _topicClient;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
        }
    }
}