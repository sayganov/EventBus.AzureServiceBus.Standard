# EventBus.AzureServiceBus.Standard  

![Actions Status](https://github.com/sayganov/EventBus.AzureServiceBus.Standard/workflows/Build/badge.svg) [![NuGet Badge](https://buildstats.info/nuget/EventBus.AzureServiceBus.Standard?includePreReleases=false)](https://www.nuget.org/packages/EventBus.azureservicebus.Standard) [![CodeFactor](https://www.codefactor.io/repository/github/sayganov/eventbus.azureservicebus.standard/badge)](https://www.codefactor.io/repository/github/sayganov/eventbus.azureservicebus.standard)

A library for event-based communication by using Azure Service Bus.

## Samples

- [Publisher app](https://github.com/sayganov/EventBus.AzureServiceBus.Standard/tree/master/samples/Publisher)
- [Subscriber app](https://github.com/sayganov/EventBus.AzureServiceBus.Standard/tree/master/samples/Subscriber)

## How-To

Install a couple of **`NuGet`** packages.

```console
PM> Install-Package Autofac.Extensions.DependencyInjection
PM> Install-Package EventBus.AzureServiceBus.Standard
```

Add configuration to **`appsettings.json`**.

```json
{
  "AzureServiceBus": {
    "AutofacScopeName": "test_autofac",
    "SubscriptionClientName": "test_client",
    "ConnectionString": "your_connection"
  }
}
```

In **`publisher`** and **`subscriber`** apps, create a new class called **`ItemCreatedIntegrationEvent`**.

```csharp
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
```

In the **`subscriber`** app, create a new class called **`ItemCreatedIntegrationEventHandler`**.

```csharp
public class ItemCreatedIntegrationEventHandler : IIntegrationEventHandler<ItemCreatedIntegrationEvent>
{
    public ItemCreatedIntegrationEventHandler()
    {
    }

    public async Task Handle(ItemCreatedIntegrationEvent @event)
    {
        //Handle the ItemCreatedIntegrationEvent event here.
    }
}
```

Modify **`Program.cs`** by adding one line of code to the class.

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}
```

In the **`publisher`** app, modify the method **`ConfigureServices`** in **`Startup.cs`**.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        ...

        var eventBusOptions = Configuration.GetSection("AzureServiceBus").Get<AzureServiceBusOptions>();

        services.AddAsbConnection(eventBusOptions);
        services.AddAsbRegistration(eventBusOptions);

        ...
    }
}
```

In the **`subscriber`** app, create an extension called **`EventBusExtension`**.

```csharp
public static class EventBusExtension
{
    public static IEnumerable<IIntegrationEventHandler> GetHandlers()
    {
        return new List<IIntegrationEventHandler>
        {
            new ItemCreatedIntegrationEventHandler()
        };
    }

    public static IApplicationBuilder SubscribeToEvents(this IApplicationBuilder app)
    {
        var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();

        eventBus.Subscribe<ItemCreatedIntegrationEvent, ItemCreatedIntegrationEventHandler>();

        return app;
    }
}
```

In the **`subscriber`** app, modify **`ConfigureServices`** and **`Configure`** methods in **`Startup.cs`**.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        ...

        var eventBusOptions = Configuration.GetSection("AzureServiceBus").Get<AzureServiceBusOptions>();

        services.AddAsbConnection(eventBusOptions);
        services.AddAsbRegistration(eventBusOptions);
        services.AddEventBusHandling(EventBusExtension.GetHandlers());

        ...
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        ...

        app.UseAuthorization();

        ...
    }
}
```

Publish the ItemCreatedIntegrationEvent event in the **`publisher`** app by using the following code, for example in a controller.

```csharp
public class ItemController : ControllerBase
{
    private readonly IEventBus _eventBus;

    public ItemController(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    [HttpPost]
    public IActionResult Publish()
    {
        var message = new ItemCreatedIntegrationEvent("Item title", "Item description");

        _eventBus.Publish(message);

        return Ok();
    }
}
```

## Code of Conduct

See [CODE_OF_CONDUCT.md](https://github.com/sayganov/EventBus.AzureServiceBus.Standard/blob/master/CODE_OF_CONDUCT.md).

## Contributing

See [CONTRIBUTING.md](https://github.com/sayganov/EventBus.AzureServiceBus.Standard/blob/master/CONTRIBUTING.md).

## References

- [Publisher-Subscriber pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/publisher-subscriber)
- [Azure Service Bus](https://azure.microsoft.com/en-us/services/service-bus/)
- [eShopOnContainers](https://github.com/dotnet-architecture/eShopOnContainers)
