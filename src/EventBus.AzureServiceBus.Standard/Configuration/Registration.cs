using Autofac;
using EventBus.AzureServiceBus.Standard.Options;
using EventBus.Base.Standard;
using Microsoft.Extensions.DependencyInjection;

namespace EventBus.AzureServiceBus.Standard.Configuration
{
    public static class Registration
    {
        public static IServiceCollection AddAsbRegistration(this IServiceCollection services, AzureServiceBusOptions options)
        {
            services.AddSingleton<IEventBus, EventBusServiceBus>(sp =>
            {
                var serviceBusPersistentConnection = sp.GetRequiredService<IServiceBusPersistentConnection>();
                var lifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var eventBusSubscriptionsManager = sp.GetRequiredService<IEventBusSubscriptionManager>();

                var brokerName = options.BrokerName;
                var autofacScopeName = options.AutofacScopeName;
                var queueName = options.QueueName;

                return new EventBusServiceBus(serviceBusPersistentConnection,
                    lifetimeScope,
                    eventBusSubscriptionsManager,
                    brokerName,
                    autofacScopeName,
                    queueName);
            });

            services.AddSingleton<IEventBusSubscriptionManager, InMemoryEventBusSubscriptionManager>();

            return services;
        }
    }
}
