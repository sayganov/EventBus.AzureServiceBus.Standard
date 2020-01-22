using EventBus.AzureServiceBus.Standard.Configuration;
using EventBus.AzureServiceBus.Standard.Options;
using EventBus.Base.Standard.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Subscriber.Extensions;

namespace Subscriber
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            //Event Bus
            var azureServiceBusOptions = Configuration.GetSection("AzureServiceBus").Get<AzureServiceBusOptions>();

            services.AddAsbConnection(azureServiceBusOptions);
            services.AddAsbRegistration(azureServiceBusOptions);
            services.AddEventBusHandling(EventBusExtension.GetHandlers());

            //Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Subscriber API", 
                    Version = "v1"
                });
            });

            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            //Event Bus
            app.SubscribeToEvents();

            //Swagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Subscriber API V1");
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
