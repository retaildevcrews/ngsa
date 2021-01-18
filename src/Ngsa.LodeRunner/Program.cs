﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ngsa.Middleware;

namespace Ngsa.LodeRunner
{
    /// <summary>
    /// Main application class
    /// </summary>
    public sealed partial class App
    {
        /// <summary>
        /// Gets or sets json serialization options
        /// </summary>
        public static JsonSerializerOptions JsonSerializerOptions { get; set; } = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        /// <summary>
        /// Gets or sets cancellation token
        /// </summary>
        public static CancellationTokenSource TokenSource { get; set; } = new CancellationTokenSource();

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args">Command Line Parameters</param>
        /// <returns>0 on success</returns>
        public static async Task<int> Main(string[] args)
        {
            // add ctl-c handler
            AddControlCHandler();

            // build the System.CommandLine.RootCommand
            RootCommand root = BuildRootCommand();
            root.Handler = CommandHandler.Create((Config cfg) => App.Run(cfg));

            if (args == null)
            {
                args = Array.Empty<string>();
            }

            if (!args.Contains("--version") &&
                (args.Contains("-h") ||
                args.Contains("--help") ||
                args.Contains("-d") ||
                args.Contains("--dry-run")))
            {
#if DEBUG
                await AsciiArt.DisplayAsciiArt("Core/ascii-art.txt", ConsoleColor.DarkMagenta, AsciiArt.Animation.Dissolve).ConfigureAwait(false);
#else
                await AsciiArt.DisplayAsciiArt("Core/ascii-art.txt", ConsoleColor.DarkMagenta, AsciiArt.Animation.None).ConfigureAwait(false);
#endif
            }

            int ret = await root.InvokeAsync(args).ConfigureAwait(false);

            if (!args.Contains("-h") &&
                !args.Contains("--help") &&
                !args.Contains("--version"))
            {
                // log the shutdown event
                Dictionary<string, object> log = new Dictionary<string, object>
                {
                    { "Date", DateTime.UtcNow },
                    { "EventType", "Shutdown" },
                };

                Console.WriteLine(JsonSerializer.Serialize(log));
            }

            return ret;
        }

        /// <summary>
        /// System.CommandLine.CommandHandler implementation
        /// </summary>
        /// <param name="config">configuration</param>
        /// <returns>non-zero on failure</returns>
        public static async Task<int> Run(Config config)
        {
            if (config == null)
            {
                Console.WriteLine("CommandOptions is null");
                return -1;
            }

            // set any missing values
            config.SetDefaultValues();

            // don't run the test on a dry run
            if (config.DryRun)
            {
                return DoDryRun(config);
            }

            // set json options based on --strict-json
            App.JsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = !config.StrictJson,
                AllowTrailingCommas = !config.StrictJson,
                ReadCommentHandling = config.StrictJson ? JsonCommentHandling.Disallow : JsonCommentHandling.Skip,
            };

            // create the test
            try
            {
                ValidationTest lrt = new ValidationTest(config);

                if (config.DelayStart > 0)
                {
                    Console.WriteLine($"Waiting {config.DelayStart} seconds to start test ...\n");

                    // wait to start the test run
                    await Task.Delay(config.DelayStart * 1000, TokenSource.Token).ConfigureAwait(false);
                }

                if (config.RunLoop)
                {
                    // run in a loop
                    return lrt.RunLoop(config, TokenSource.Token);
                }
                else
                {
                    // run one iteration
                    return await lrt.RunOnce(config, TokenSource.Token).ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException tce)
            {
                // log exception
                if (!tce.Task.IsCompleted)
                {
                    Console.WriteLine($"Exception: {tce}");
                    return 1;
                }

                // task is completed
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nException:{ex.Message}");
                return 1;
            }
        }

        /// <summary>
        /// Check to see if the file exists in the current directory
        /// </summary>
        /// <param name="name">file name</param>
        /// <returns>bool</returns>
        public static bool CheckFileExists(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && System.IO.File.Exists(name.Trim());
        }

        /// <summary>
        /// Add a ctl-c handler
        /// </summary>
        private static void AddControlCHandler()
        {
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                TokenSource.Cancel();
            };
        }
    }
}
