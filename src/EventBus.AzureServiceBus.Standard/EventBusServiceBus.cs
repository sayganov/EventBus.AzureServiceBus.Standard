﻿using System;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using EventBus.Base.Standard;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EventBus.AzureServiceBus.Standard
{
    public class EventBusServiceBus : IEventBus
    {
        private readonly IServiceBusPersistentConnection _persistentConnection;
        private readonly IEventBusSubscriptionManager _subsManager;
        private readonly ILifetimeScope _autofac;

        private readonly SubscriptionClient _subscriptionClient;

        private readonly string _autofacScopeName;
        private readonly string _brokerName;

        public EventBusServiceBus(IServiceBusPersistentConnection persistentConnection,
            ILifetimeScope autofac,
            IEventBusSubscriptionManager subsManager,
            string brokerName,
            string autofacScopeName,
            string queueName = null)
        {
            _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
            _subsManager = subsManager ?? new InMemoryEventBusSubscriptionManager();
            _autofac = autofac ?? throw new ArgumentNullException(nameof(autofac));
            _subscriptionClient = new SubscriptionClient(persistentConnection.ServiceBusConnectionStringBuilder, queueName);

            RemoveDefaultRule();
            RegisterSubscriptionClientMessageHandler();

            _brokerName = brokerName;
            _autofacScopeName = autofacScopeName;
        }

        public void Publish(IntegrationEvent @event)
        {
            var eventName = @event.GetType().Name.Replace(_brokerName, "");
            var jsonMessage = JsonConvert.SerializeObject(@event);
            var body = Encoding.UTF8.GetBytes(jsonMessage);

            var message = new Message
            {
                MessageId = Guid.NewGuid().ToString(),
                Body = body,
                Label = eventName
            };

            var topicClient = _persistentConnection.CreateModel();

            topicClient.SendAsync(message)
                .GetAwaiter()
                .GetResult();
        }

        public void SubscribeDynamic<TH>(string eventName) where TH : IDynamicIntegrationEventHandler
        {
            _subsManager.AddDynamicSubscription<TH>(eventName);
        }

        public void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
        {
            var eventName = typeof(T).Name.Replace(_brokerName, "");
            var containsKey = _subsManager.HasSubscriptionsForEvent<T>();

            if (!containsKey)
                try
                {
                    _subscriptionClient.AddRuleAsync(new RuleDescription
                    {
                        Filter = new CorrelationFilter {Label = eventName},
                        Name = eventName
                    }).GetAwaiter().GetResult();
                }
                catch (ServiceBusException)
                {
                }

            _subsManager.AddSubscription<T, TH>();
        }

        public void Unsubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
        {
            var eventName = typeof(T).Name.Replace(_brokerName, "");

            try
            {
                _subscriptionClient
                    .RemoveRuleAsync(eventName)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (MessagingEntityNotFoundException)
            {
            }

            _subsManager.RemoveSubscription<T, TH>();
        }

        public void UnsubscribeDynamic<TH>(string eventName) where TH : IDynamicIntegrationEventHandler
        {
            _subsManager.RemoveDynamicSubscription<TH>(eventName);
        }

        public void Dispose()
        {
            _subsManager.Clear();
        }

        private void RegisterSubscriptionClientMessageHandler()
        {
            _subscriptionClient.RegisterMessageHandler(
                async (message, token) =>
                {
                    var eventName = $"{message.Label}{_brokerName}";
                    var messageData = Encoding.UTF8.GetString(message.Body);

                    // Complete the message so that it is not received again.
                    if (await ProcessEvent(eventName, messageData)) await _subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
                },
                new MessageHandlerOptions(ExceptionReceivedHandler) {MaxConcurrentCalls = 10, AutoComplete = false});
        }

        private static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            var ex = exceptionReceivedEventArgs.Exception;
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;

            return Task.CompletedTask;
        }

        private async Task<bool> ProcessEvent(string eventName, string message)
        {
            if (!_subsManager.HasSubscriptionsForEvent(eventName)) return false;

            using (var scope = _autofac.BeginLifetimeScope(_autofacScopeName))
            {
                var subscriptions = _subsManager.GetHandlersForEvent(eventName);
                
                foreach (var subscription in subscriptions)
                    if (subscription.IsDynamic)
                    {
                        var handler = scope.ResolveOptional(subscription.HandlerType) as IDynamicIntegrationEventHandler;

                        if (handler == null) continue;
                        dynamic eventData = JObject.Parse(message);
                        await handler.Handle(eventData);
                    }
                    else
                    {
                        var handler = scope.ResolveOptional(subscription.HandlerType);
                        
                        if (handler == null) continue;
                        
                        var eventType = _subsManager.GetEventTypeByName(eventName);
                        var integrationEvent = JsonConvert.DeserializeObject(message, eventType);
                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                        
                        await (Task) concreteType.GetMethod("Handle").Invoke(handler, new[] {integrationEvent});
                    }
            }

            return true;
        }

        private void RemoveDefaultRule()
        {
            try
            {
                _subscriptionClient
                    .RemoveRuleAsync(RuleDescription.DefaultRuleName)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (MessagingEntityNotFoundException)
            {
            }
        }
    }
}