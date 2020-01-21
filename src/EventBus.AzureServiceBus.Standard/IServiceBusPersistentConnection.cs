using System;
using Microsoft.Azure.ServiceBus;

namespace EventBus.AzureServiceBus.Standard
{
    public interface IServiceBusPersistentConnection : IDisposable
    {
        ServiceBusConnectionStringBuilder ServiceBusConnectionStringBuilder { get; }

        ITopicClient CreateModel();
    }
}