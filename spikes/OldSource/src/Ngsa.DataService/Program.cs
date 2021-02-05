// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ngsa.DataService.DataAccessLayer;
using Ngsa.Middleware;

namespace Ngsa.DataService
{
    /// <summary>
    /// Main application class
    /// </summary>
    public sealed partial class App
    {
        // ILogger instance
        private static readonly NgsaLog Logger = new NgsaLog { Name = typeof(App).FullName };

        // web host
        private static IWebHost host;

        // Key Vault configuration
        private static IConfigurationRoot config;

        private static CancellationTokenSource ctCancel;

        public static InMemoryDal CacheDal { get; set; }
        public static InMemoryDal SearchService => CacheDal;

        public static IDAL CosmosDal { get; set; }

        public static string CosmosName { get; set; } = string.Empty;

        // caching config
        public static bool Cache { get; set; } = true;
        public static int PerfCache { get; set; }
        public static int CacheDuration { get; set; }
        public static bool InMemory { get; set; }

        /// <summary>
        /// Gets or sets LogLevel
        /// </summary>
        public static LogLevel AppLogLevel { get; set; } = LogLevel.Error;

        /// <summary>
        /// Gets or sets a value indicating whether LogLevel is set in command line or env var
        /// </summary>
        public static bool IsLogLevelSet { get; set; }

        /// <summary>
        /// Gets or sets the secrets from k8s volume
        /// </summary>
        public static Secrets Secrets { get; set; }

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
            root.Handler = CommandHandler.Create<string, LogLevel, bool, bool, bool, int, int>(RunApp);

            List<string> cmd = CombineEnvVarsWithCommandLine(args);

            if (!cmd.Contains("--version") &&
                (cmd.Contains("-h") ||
                cmd.Contains("--help") ||
                cmd.Contains("-d") ||
                cmd.Contains("--dry-run")))
            {
#if DEBUG
                await AsciiArt.DisplayAsciiArt("Core/ascii-art.txt", ConsoleColor.DarkMagenta, AsciiArt.Animation.TwoColor).ConfigureAwait(false);
#else
                await AsciiArt.DisplayAsciiArt("Core/ascii-art.txt", ConsoleColor.DarkMagenta, AsciiArt.Animation.None).ConfigureAwait(false);
#endif
            }

            // run the app
            return await root.InvokeAsync(cmd.ToArray()).ConfigureAwait(false);
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

                Logger.LogInformation("CtlCHandler", "Ctl-C Pressed");

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
            if (Logger != null)
            {
                Logger.LogInformation("Startup", $"Version: {VersionExtension.Version}");
            }
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
                Logger.LogError(nameof(BuildConfig), "Exception: {ex.Message}", ex: ex);

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
                    // add IConfigurationRoot
                    services.AddSingleton<IConfigurationRoot>(config);
                });

            // configure logger based on command line
            builder.ConfigureLogging(logger =>
            {
                logger.ClearProviders();
                logger.AddNgsaLogger(config => { config.LogLevel = AppLogLevel; });

                // if you specify the --log-level option, it will override the appsettings.json options
                // remove any or all of the code below that you don't want to override
                if (App.IsLogLevelSet)
                {
                    logger.AddFilter("Microsoft", AppLogLevel)
                    .AddFilter("System", AppLogLevel)
                    .AddFilter("Default", AppLogLevel)
                    .AddFilter("Ngsa.DataService", AppLogLevel);
                }
            });

            // build the host
            return builder.Build();
        }
    }
}
