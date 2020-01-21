﻿using EventBus.Base.Standard;

namespace Subscriber.IntegrationEvents.Events
{
    public class ItemCreatedIntegrationEvent : IntegrationEvent
    {
        public string Title { get; set; }
        public string Description { get; set; }

        public ItemCreatedIntegrationEvent(string title, string description)
        {
            Title = title;
            Description = description;
        }
    }
}
