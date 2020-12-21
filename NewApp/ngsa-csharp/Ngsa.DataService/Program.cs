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
        private static readonly bool Cache = true;

        // ILogger instance
        private static ILogger<App> logger;

        // web host
        private static IWebHost host;

        // Key Vault configuration
        private static IConfigurationRoot config;

        private static CancellationTokenSource ctCancel;

        public static InMemoryDal CacheDal { get; set; }
        public static InMemoryDal SearchService => CacheDal;

        public static IDAL CosmosDal { get; set; }

        public static string CosmosName { get; set; } = string.Empty;
        public static string CosmosQueryId { get; set; } = string.Empty;
        public static string Region { get; set; } = string.Empty;
        public static string Zone { get; set; } = string.Empty;
        public static string PodType { get; set; }

        public static bool UseCache => Cache || Ngsa.Middleware.RequestLogger.RequestsPerSecond > Constants.MaxReqSecBeforeCache;

        /// <summary>
        /// Gets or sets LogLevel
        /// </summary>
        public static LogLevel AppLogLevel { get; set; } = LogLevel.Warning;
        public static bool InMemory { get; set; }
        public static bool NoCache { get; set; }
        public static int PerfCache { get; set; }
        public static int CacheDuration { get; set; }

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

            string[] cmd = CombineEnvVarsWithCommandLine(args);

            // run the app
            return await root.InvokeAsync(cmd).ConfigureAwait(false);
        }

        /// <summary>
        /// Display the ASCII art file if it exists
        /// </summary>
        private static async Task DisplayAsciiArt()
        {
            // todo - move to middleware

            const string file = "Core/ascii-art.txt";

            if (File.Exists(file))
            {
                string txt = File.ReadAllText(file);
                string[] lines = File.ReadAllLines(file);

                int top = Console.CursorTop;
                int row = top + Console.WindowHeight - lines.Length - 5;
                Console.CursorVisible = false;

                // scroll the window
                for (int i = 0; i < Console.WindowHeight; i++)
                {
                    Console.WriteLine();
                }

                int key = 0;
                Random rnd = new Random(DateTime.Now.Millisecond);

                SortedList<int, ConsoleCharacter> lrandom = new SortedList<int, ConsoleCharacter>();
                List<ConsoleCharacter> list = new List<ConsoleCharacter>();

                // create the random list
                for (int r = 0; r < lines.Length; r++)
                {
                    string line = lines[r];

                    for (int c = 0; c < line.Length; c++)
                    {
                        if (!char.IsWhiteSpace(line[c]))
                        {
                            while (key < 1 || lrandom.ContainsKey(key))
                            {
                                key = rnd.Next(1, int.MaxValue);
                            }

                            ConsoleCharacter l = new ConsoleCharacter
                            {
                                Color = ConsoleColor.DarkMagenta,
                                Row = top + r,
                                Col = c,
                                Value = line[c],
                            };

                            // add to random list and in-order list
                            list.Add(l);
                            lrandom.Add(key, l);
                        }
                    }
                }

                // show the art - random dissolve
                // todo - make a method
                foreach (ConsoleCharacter l in lrandom.Values)
                {
                    Console.SetCursorPosition(l.Col, l.Row);
                    Console.ForegroundColor = l.Color;
                    Console.Write(l.Value);
                    await Task.Delay(10);
                }

                Console.SetCursorPosition(0, top + lines.Length + 1);
                Console.CursorVisible = true;
                Console.ResetColor();
                return;

                // todo - move animations into methods

                row = top + Console.WindowHeight - lines.Length - 2;

                // scroll the art down
                for (int i = top; i < row; i++)
                {
                    Console.MoveBufferArea(0, i, Console.BufferWidth, lines.Length, 0, i + 1);
                    await Task.Delay(100);
                }

                // scroll the art up
                for (int i = row; i > top; i--)
                {
                    Console.MoveBufferArea(0, i, Console.BufferWidth, lines.Length, 0, i - 1);
                    await Task.Delay(100);
                }

                // clear the art from bottom right
                for (int r = lines.Length - 1 + top; r >= top; r--)
                {
                    string line = lines[r - top];

                    for (int c = line.Length - 1; c >= 0; c--)
                    {
                        Console.SetCursorPosition(c, r);
                        Console.Write(' ');

                        if (!char.IsWhiteSpace(line[c]))
                        {
                            await Task.Delay(20);
                        }
                    }
                }

                Console.SetCursorPosition(0, top);

                // show the logo from top left
                Console.ForegroundColor = ConsoleColor.Green;
                foreach (char c in txt)
                {
                    Console.Write(c);

                    if (!char.IsWhiteSpace(c))
                    {
                        await Task.Delay(20);
                    }
                }

                // change art color from bottom right
                Console.ForegroundColor = ConsoleColor.Blue;
                for (int r = lines.Length - 1 + top; r >= top; r--)
                {
                    string line = lines[r - top];

                    for (int c = line.Length - 1; c >= 0; c--)
                    {
                        Console.SetCursorPosition(c, r);
                        Console.Write(line[c]);

                        if (!char.IsWhiteSpace(line[c]))
                        {
                            await Task.Delay(20);
                        }
                    }
                }

                // change art color from top left
                Console.SetCursorPosition(0, top);
                Console.ForegroundColor = ConsoleColor.Cyan;
                foreach (char c in txt)
                {
                    Console.Write(c);

                    if (!char.IsWhiteSpace(c))
                    {
                        await Task.Delay(20);
                    }
                }

                // change art to two color
                int end = list.Count - 1;
                int last = end;

                for (int i = 0; i <= end; i++)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.SetCursorPosition(list[i].Col, list[i].Row);
                    Console.Write(list[i].Value);

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.SetCursorPosition(list[end].Col, list[end].Row);
                    Console.Write(list[end].Value);
                    end--;

                    await Task.Delay(30);
                }

                // reset console
                Console.SetCursorPosition(0, top + lines.Length + 2);
                Console.CursorVisible = true;
                Console.ResetColor();
            }
        }

        // TODO - delete this
        private static async Task Santa()
        {
            const string file = "Core/happy.txt";

            if (File.Exists(file))
            {
                string[] lines = File.ReadAllLines(file);

                int top = Console.CursorTop;
                Console.CursorVisible = false;

                // scroll the window
                for (int i = 0; i < Console.WindowHeight; i++)
                {
                    Console.WriteLine();
                }

                int key = 0;
                Random rnd = new Random(DateTime.Now.Millisecond);

                SortedList<int, ConsoleCharacter> lrandom = new SortedList<int, ConsoleCharacter>();
                List<ConsoleCharacter> list = new List<ConsoleCharacter>();

                // create the random list
                for (int r = 0; r < lines.Length; r++)
                {
                    string line = lines[r];

                    for (int c = 0; c < line.Length; c++)
                    {
                        if (!char.IsWhiteSpace(line[c]))
                        {
                            while (key < 1 || lrandom.ContainsKey(key))
                            {
                                key = rnd.Next(1, int.MaxValue);
                            }

                            ConsoleCharacter l = new ConsoleCharacter
                            {
                                Row = top + r + 2,
                                Col = c + 24,
                                Value = line[c],
                            };

                            if (r < 9)
                            {
                                l.Color = ConsoleColor.Green;
                            }

                            if (r >= 25 && r <= 27)
                            {
                                l.Color = ConsoleColor.Green;
                            }

                            list.Add(l);
                            lrandom.Add(key, l);
                        }
                    }
                }

                // show the art
                foreach (ConsoleCharacter l in lrandom.Values)
                {
                    Console.SetCursorPosition(l.Col, l.Row);
                    Console.ForegroundColor = l.Color;
                    Console.Write(l.Value);
                    await Task.Delay(2);
                }

                Console.ResetColor();
                Console.CursorVisible = true;
                Console.SetCursorPosition(0, Console.WindowHeight - 1 + top);
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

                Console.ResetColor();

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

            if (logger != null)
            {
                logger.LogInformation("Web Server Started");
            }

            Console.WriteLine($"\nVersion: {Ngsa.Middleware.VersionExtension.Version}");
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
                    // add IConfigurationRoot
                    services.AddSingleton<IConfigurationRoot>(config);
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
                    logger.AddFilter("Microsoft", AppLogLevel)
                    .AddFilter("System", AppLogLevel)
                    .AddFilter("Default", AppLogLevel)
                    .AddFilter("Ngsa.DataService", logLevel);
                }
            });

            // build the host
            return builder.Build();
        }

        internal class ConsoleCharacter
        {
            public int Row { get; set; }
            public int Col { get; set; }
            public char Value { get; set; }
            public ConsoleColor Color { get; set; } = ConsoleColor.DarkMagenta;
        }
    }
}
