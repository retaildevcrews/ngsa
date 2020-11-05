// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CSE.NextGenSymmetricApp.DataAccessLayer;
using CSE.NextGenSymmetricApp.Validation;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CSE.NextGenSymmetricApp
{
    /// <summary>
    /// Main application class
    /// </summary>
    public sealed partial class App
    {
        public static string CosmosName { get; set; } = string.Empty;
        public static string CosmosQueryId { get; set; } = string.Empty;
        public static string Region { get; set; } = string.Empty;
        public static string Zone { get; set; } = string.Empty;

        // ILogger instance
        private static ILogger<App> logger;

        // web host
        private static IWebHost host;

        // Key Vault configuration
        private static IConfigurationRoot config;

        private static CancellationTokenSource ctCancel;

        /// <summary>
        /// Gets or sets LogLevel
        /// </summary>
        public static LogLevel AppLogLevel { get; set; } = LogLevel.Warning;

        /// <summary>
        /// Gets or sets a value indicating whether LogLevel is set in command line or env var
        /// </summary>
        public static bool IsLogLevelSet { get; set; }

        /// <summary>
        /// Gets or sets the secrets from k8s volume
        /// </summary>
        public static Secrets Secrets { get; set; }

        public static ILogger<ValidationResult> ValidationLogger { get; set; }

        /// <summary>
        /// Main entry point
        ///
        /// Configure and run the web server
        /// </summary>
        /// <param name="args">command line args</param>
        /// <returns>IActionResult</returns>
        public static async Task<int> Main(string[] args)
        {
            // build the System.CommandLine.RootCommand
            RootCommand root = BuildRootCommand();
            root.Handler = CommandHandler.Create<string, LogLevel, bool, bool>(RunApp);

            string[] cmd = CombineEnvVarsWithCommandLine(args);

            // run the app
            return await root.InvokeAsync(cmd).ConfigureAwait(false);
        }

        /// <summary>
        /// Stop the web server via code
        /// </summary>
        public static void Stop()
        {
            if (ctCancel != null)
            {
                ctCancel.Cancel(false);
            }
        }

        /// <summary>
        /// Display the ASCII art file if it exists
        /// </summary>
        private static void DisplayAsciiArt()
        {
            const string file = "App/ascii-art.txt";

            if (File.Exists(file))
            {
                Console.WriteLine(File.ReadAllText(file));
            }
        }

        /// <summary>
        /// Creates a CancellationTokenSource that cancels on ctl-c pressed
        /// </summary>
        /// <returns>CancellationTokenSource</returns>
        private static CancellationTokenSource SetupCtlCHandler()
        {
            CancellationTokenSource ctCancel = new CancellationTokenSource();

            Console.CancelKeyPress += async (sender, e) =>
            {
                e.Cancel = true;
                ctCancel.Cancel();

                Console.WriteLine("Ctl-C Pressed - Starting shutdown ...");

                // trigger graceful shutdown for the webhost
                // force shutdown after timeout, defined in UseShutdownTimeout within BuildHost() method
                await host.StopAsync().ConfigureAwait(false);

                // end the app
                Environment.Exit(0);
            };

            return ctCancel;
        }

        /// <summary>
        /// Log startup messages
        /// </summary>
        private static void LogStartup()
        {
            // get the logger service
            logger = host.Services.GetRequiredService<ILogger<App>>();
            ValidationLogger = host.Services.GetRequiredService<ILogger<ValidationResult>>();

            if (logger != null)
            {
                // log a not using app insights warning
                if (string.IsNullOrEmpty(App.Secrets.AppInsightsKey))
                {
                    logger.LogWarning("App Insights Key not set");
                }

                logger.LogInformation("Web Server Started");
            }

            DisplayAsciiArt();

            Console.WriteLine($"\nVersion: {Middleware.VersionExtension.Version}");
        }

        /// <summary>
        /// Builds the config for the web server
        ///
        /// Uses Key Vault via Managed Identity (MI)
        /// </summary>
        /// <param name="kvClient">Key Vault Client</param>
        /// <param name="kvUrl">Key Vault URL</param>
        /// <returns>Root Configuration</returns>
        private static IConfigurationRoot BuildConfig()
        {
            try
            {
                // standard config builder
                IConfigurationBuilder cfgBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false);

                // build the config
                return cfgBuilder.Build();
            }
            catch (Exception ex)
            {
                // log and fail
                Console.WriteLine($"{ex}\nBuildConfig:Exception: {ex.Message}");
                Environment.Exit(-1);
            }

            return null;
        }

        /// <summary>
        /// Build the web host
        /// </summary>
        /// <param name="useInMemory">Use in memory DB flag</param>
        /// <returns>Web Host ready to run</returns>
        private static IWebHost BuildHost()
        {
            // build the config
            config = BuildConfig();

            // configure the web host builder
            IWebHostBuilder builder = WebHost.CreateDefaultBuilder()
                .UseConfiguration(config)
                .UseUrls(string.Format(System.Globalization.CultureInfo.InvariantCulture, $"http://*:{Constants.Port}/"))
                .UseStartup<Startup>()
                .UseShutdownTimeout(TimeSpan.FromSeconds(Constants.GracefulShutdownTimeout))
                .ConfigureServices(services =>
                {
                    // add the data access layer via DI
                    if (App.Secrets.UseInMemoryDb)
                    {
                        services.AddInMemoryDal();
                    }
                    else
                    {
                        services.AddDal(new Uri(Secrets.CosmosServer), Secrets.CosmosKey, Secrets.CosmosDatabase, Secrets.CosmosCollection);
                    }

                    // add IConfigurationRoot
                    services.AddSingleton<IConfigurationRoot>(config);
                    services.AddResponseCaching();
                });

            // configure logger based on command line
            builder.ConfigureLogging(logger =>
            {
                logger.ClearProviders();
                logger.AddConsole();

                // if you specify the --log-level option, it will override the appsettings.json options
                // remove any or all of the code below that you don't want to override
                if (App.IsLogLevelSet)
                {
                    logger.AddFilter("Microsoft", AppLogLevel)
                    .AddFilter("System", AppLogLevel)
                    .AddFilter("Default", AppLogLevel)
                    .AddFilter("CSE.NextGenSymmetricApp", AppLogLevel);
                }
            });

            // build the host
            return builder.Build();
        }
    }
}
