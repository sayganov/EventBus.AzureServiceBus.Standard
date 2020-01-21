using System;
using EventBus.AzureServiceBus.Standard.Options;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

namespace EventBus.AzureServiceBus.Standard.Configuration
{
    public static class Connection
    {
        public static IServiceCollection AddAsbConnection(this IServiceCollection services, AzureServiceBusOptions options)
        {
            if (string.IsNullOrEmpty(options.ConnectionString))
            {
                throw new NullReferenceException("ConnectionString is null or empty.");
            }

            services.AddSingleton<IServiceBusPersistentConnection>(sp =>
            {
                var serviceBusConnection = new ServiceBusConnectionStringBuilder(options.ConnectionString);

                return new DefaultServiceBusPersistentConnection(serviceBusConnection);
            });

            return services;
        }
    }
}