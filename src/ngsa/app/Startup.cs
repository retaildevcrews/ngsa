// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CSE.Middleware;
using CSE.NextGenSymmetricApp.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CSE.NextGenSymmetricApp
{
    /// <summary>
    /// WebHostBuilder Startup
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">the configuration for WebHost</param>
        public Startup(IConfiguration configuration)
        {
            // keep a local reference
            Configuration = configuration;
        }

        /// <summary>
        /// Gets IConfiguration
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Configure the application builder
        /// </summary>
        /// <param name="app">IApplicationBuilder</param>
        /// <param name="env">IWebHostEnvironment</param>
        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (env == null)
            {
                throw new ArgumentNullException(nameof(env));
            }

            // log http responses to the console
            // this should be first as it "wraps" all requests
            if (App.AppLogLevel != LogLevel.None)
            {
                app.UseLogger(new LoggerOptions
                {
                    Log2xx = App.AppLogLevel <= LogLevel.Information,
                    Log3xx = App.AppLogLevel <= LogLevel.Information,
                    Log4xx = App.AppLogLevel <= LogLevel.Warning,
                    Log5xx = true,
                });
            }

            // differences based on dev or prod
            if (env.IsDevelopment())
            {
                //app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseResponseCaching();

            app.UseStaticFiles();

            // use routing
            app.UseRouting();

            // map the controllers
            app.UseEndpoints(ep => { ep.MapControllers(); });

            // rewrite root to /index.html
            app.UseSwaggerRoot();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint(Constants.SwaggerPath, Constants.SwaggerTitle);
                c.RoutePrefix = string.Empty;
            });

            // use the robots middleware to handle /robots*.txt requests
            app.UseRobots();

            // use the version middleware to handle /version
            app.UseVersion();
        }

        /// <summary>
        /// Service configuration
        /// </summary>
        /// <param name="services">The services in the web host</param>
        public static void ConfigureServices(IServiceCollection services)
        {
            // set json serialization defaults and api behavior
            services.AddControllers()
                .ConfigureApiBehaviorOptions(o =>
                {
                    o.InvalidModelStateResponseFactory = ctx => new ValidationResult();
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.IgnoreNullValues = true;
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.Converters.Add(new TimeSpanConverter());
                });

            // add healthcheck service
            services.AddHealthChecks().AddCosmosHealthCheck(CosmosHealthCheck.ServiceId);
        }
    }
}
