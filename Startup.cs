// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PizzOrBurger
{
    /// <summary>
    /// The Startup class configures services and the request pipeline.
    /// </summary>
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddBot<PizzOrBurgerBot>(options =>
           {
               var secretKey = Configuration.GetSection("botFileSecret")?.Value;

                // Loads .bot configuration file and adds a singleton that your Bot can access through dependency injection.
                var botConfig = BotConfiguration.Load(@".\PizzOrBurger.bot", secretKey);
               services.AddSingleton(sp => botConfig);

                // Retrieve current endpoint.
                var service = botConfig.Services.Where(s => s.Type == "endpoint" && s.Name == "development").FirstOrDefault();
               if (!(service is EndpointService endpointService))
               {
                   throw new InvalidOperationException($"The .bot file does not contain a development endpoint.");
               }

               var appId = Configuration.GetSection("MicrosoftAppId").Value;
               var appPassword = Configuration.GetSection("MicrosoftAppPassword").Value;
               options.CredentialProvider = new SimpleCredentialProvider(appId, appPassword);

                // Catches any errors that occur during a conversation turn and logs them.
                options.OnTurnError = async (context, exception) =>
               {
                   await context.SendActivityAsync("Sorry, it looks like something went wrong.");
               };
           });

            services.AddSingleton<IConfiguration>(Configuration);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseBotFramework();
        }
    }
}
