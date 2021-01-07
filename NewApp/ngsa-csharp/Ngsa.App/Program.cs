// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ngsa.Middleware;

namespace Ngsa.App
{
    /// <summary>
    /// Main application class
    /// </summary>
    public sealed partial class App
    {
        // ILogger instance
        private static readonly NgsaLog Logger = new NgsaLog { Name = typeof(App).FullName, Method = "Main" };

        // web host
        private static IWebHost host;

        // Key Vault configuration
        private static IConfigurationRoot config;

        private static CancellationTokenSource ctCancel;

        public static string DataService { get; set; }
        public static string CosmosName { get; set; } = string.Empty;
        public static string CosmosQueryId { get; set; } = string.Empty;
        public static string Region { get; set; } = string.Empty;
        public static string Zone { get; set; } = string.Empty;
        public static string PodType { get; set; }

        /// <summary>
        /// Gets or sets LogLevel
        /// </summary>
        public static LogLevel AppLogLevel { get; set; } = LogLevel.Warning;

        /// <summary>
        /// Gets or sets a value indicating whether LogLevel is set in command line or env var
        /// </summary>
        public static bool IsLogLevelSet { get; set; }

        /// <summary>
        /// Main entry point
        ///
        /// Configure and run the web server
        /// </summary>
        /// <param name="args">command line args</param>
        /// <returns>IActionResult</returns>
        public static async Task<int> Main(string[] args)
        {
            // add pod, region, zone info to logger
            Logger.EnrichLog();

            // build the System.CommandLine.RootCommand
            RootCommand root = BuildRootCommand();
            root.Handler = CommandHandler.Create<string, LogLevel, bool>(RunApp);

            List<string> cmd = CombineEnvVarsWithCommandLine(args);

            if (cmd.Contains("-h") ||
                cmd.Contains("--help") ||
                cmd.Contains("-d") ||
                cmd.Contains("--dry-run"))
            {
                await AsciiArt.DisplayAsciiArt("Core/ascii-art.txt", ConsoleColor.DarkMagenta, AsciiArt.Animation.Fade).ConfigureAwait(false);
            }

            // run the app
            return await root.InvokeAsync(cmd.ToArray()).ConfigureAwait(false);
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

                Logger.Method = "CtlCHandler";
                Logger.Data.Clear();
                Logger.LogInformation("Ctl-C Pressed");

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
            Logger.Data.Add("version", Ngsa.Middleware.VersionExtension.Version);
            Logger.LogInformation("Web Server Started");
            Logger.Data.Clear();
        }

        /// <summary>
        /// Builds the config for the web server
        /// </summary>
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
                Logger.Method = nameof(BuildConfig);
                Logger.LogError($"Exception: {ex.Message}", ex);
                Environment.Exit(-1);
            }

            return null;
        }

        /// <summary>
        /// Build the web host
        /// </summary>
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
                    // add IConfigurationRoot
                    services.AddSingleton<IConfigurationRoot>(config);
                    services.AddResponseCaching();
                });

            // configure logger based on command line
            builder.ConfigureLogging(logger =>
            {
                LogLevel logLevel = AppLogLevel <= LogLevel.Information ? AppLogLevel : LogLevel.Information;

                logger.ClearProviders();
                logger.AddNgsaLogger(config => { config.LogLevel = logLevel; });

                // if you specify the --log-level option, it will override the appsettings.json options
                // remove any or all of the code below that you don't want to override
                if (App.IsLogLevelSet)
                {
                    logger.AddFilter("Microsoft", LogLevel.Error)
                    .AddFilter("System", LogLevel.Error)
                    .AddFilter("Default", LogLevel.Error)
                    .AddFilter("Ngsa.App", logLevel);
                }
            });

            // build the host
            return builder.Build();
        }
    }
}
