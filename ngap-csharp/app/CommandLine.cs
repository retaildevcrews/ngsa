// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Globalization;
using System.Threading.Tasks;
using CSE.KeyVault;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CSE.NextGenApp
{
    /// <summary>
    /// Main application class
    /// </summary>
    public sealed partial class App
    {
        /// <summary>
        /// Combine env vars and command line values
        /// </summary>
        /// <param name="args">command line args</param>
        /// <returns>string[]</returns>
        public static string[] CombineEnvVarsWithCommandLine(string[] args)
        {
            if (args == null)
            {
                args = Array.Empty<string>();
            }

            List<string> cmd = new List<string>(args);

            string kv = Environment.GetEnvironmentVariable(Constants.KeyVaultName);
            string auth = Environment.GetEnvironmentVariable(Constants.AuthType);
            string logLevel = Environment.GetEnvironmentVariable(Constants.LogLevel);

            // add --keyvault-name from environment
            if (!string.IsNullOrEmpty(kv) && !cmd.Contains("--keyvault-name") && !cmd.Contains("-k"))
            {
                cmd.Add("--keyvault-name");
                cmd.Add(kv);
            }

            // add --auth-type value from environment or default
            if (!cmd.Contains("--auth-type") && !cmd.Contains("-a"))
            {
                cmd.Add("--auth-type");
                cmd.Add(string.IsNullOrEmpty(auth) ? "MI" : auth);
            }

            // add --log-level value from environment or default
            if (!cmd.Contains("--log-level") && !cmd.Contains("-l"))
            {
                cmd.Add("--log-level");
                cmd.Add(string.IsNullOrEmpty(logLevel) ? "Warning" : logLevel);
                App.IsLogLevelSet = !string.IsNullOrEmpty(logLevel);
            }
            else
            {
                App.IsLogLevelSet = true;
            }

            return cmd.ToArray();
        }

        /// <summary>
        /// Build the RootCommand for parsing
        /// </summary>
        /// <returns>RootCommand</returns>
        public static RootCommand BuildRootCommand()
        {
            RootCommand root = new RootCommand
            {
                Name = "ngap",
                Description = "Next Gen App",
                TreatUnmatchedTokensAsErrors = true,
            };

            // add options
            Option optKv = new Option<string>(new string[] { "-k", "--keyvault-name" }, "The name or URL of the Azure Keyvault")
            {
                Argument = new Argument<string>(),

                // IsRequired = true,
            };

            optKv.AddValidator(v =>
            {
                if (v.Tokens == null ||
                v.Tokens.Count != 1 ||
                !KeyVaultHelper.ValidateName(v.Tokens[0].Value))
                {
                    return "--keyvault-name must be 3-20 characters [a-z][0-9]";
                }

                return string.Empty;
            });

            // add the options
            root.AddOption(optKv);
            root.AddOption(new Option<AuthenticationType>(new string[] { "-a", "--auth-type" }, "Authentication type (Release builds require MI; Debug builds support all 3 options)"));
            root.AddOption(new Option<LogLevel>(new string[] { "-l", "--log-level" }, "Log Level"));
            root.AddOption(new Option(new string[] { "-d", "--dry-run" }, "Validates configuration"));

            return root;
        }

        /// <summary>
        /// Run the app
        /// </summary>
        /// <param name="keyvaultName">Keyvault Name</param>
        /// <param name="authType">Authentication Type</param>
        /// <param name="logLevel">Log Level</param>
        /// <param name="dryRun">Dry Run flag</param>
        /// <returns>status</returns>
        public static async Task<int> RunApp(string keyvaultName, AuthenticationType authType, LogLevel logLevel, bool dryRun)
        {
            try
            {
                // setup ctl c handler
                ctCancel = SetupCtlCHandler();

                AppLogLevel = logLevel;

                // build the host
                host = BuildHost();

                if (host == null)
                {
                    return -1;
                }

                // don't start the web server
                if (dryRun)
                {
                    return DoDryRun(authType);
                }

                // log startup messages
                LogStartup();

                // verify key vault access
                // IKeyVaultConnection kvConnection = host.Services.GetService<IKeyVaultConnection>();
                // Task<Microsoft.Azure.KeyVault.Models.SecretBundle> secret = kvConnection.Client.GetSecretAsync(kvConnection.Address, Constants.CosmosDatabase);

                // start the webserver
                Task w = host.RunAsync();

                // this doesn't return except on ctl-c
                await w.ConfigureAwait(false);

                // use this line instead if you want to re-read the Cosmos connection info on a timer
                // await RunKeyRotationCheck(ctCancel, Constants.KeyVaultChangeCheckSeconds).ConfigureAwait(false);

                // if not cancelled, app exit -1
                return ctCancel.IsCancellationRequested ? 0 : -1;
            }
            catch (Exception ex)
            {
                // end app on error
                if (logger != null)
                {
                    logger.LogError($"Exception: {ex}");
                }
                else
                {
                    Console.WriteLine($"{ex}\nError in Main() {ex.Message}");
                }

                return -1;
            }
        }

        /// <summary>
        /// Display the dry run message
        /// </summary>
        /// <param name="authType">authentication type</param>
        /// <returns>0</returns>
        private static int DoDryRun(AuthenticationType authType)
        {
            Console.WriteLine($"Version            {Middleware.VersionExtension.Version}");
            Console.WriteLine($"Auth Type          {authType}");
            Console.WriteLine($"Log Level          {AppLogLevel}");
            Console.WriteLine($"Cosmos Server      {config.GetValue<string>(Constants.CosmosUrl)}");
            Console.WriteLine($"Cosmos Key         Length({config.GetValue<string>(Constants.CosmosKey).Length})");
            Console.WriteLine($"Cosmos Database    {config.GetValue<string>(Constants.CosmosDatabase)}");
            Console.WriteLine($"Cosmos Collection  {config.GetValue<string>(Constants.CosmosCollection)}");
            Console.WriteLine($"App Insights Key   {(string.IsNullOrEmpty(config.GetValue<string>(Constants.AppInsightsKey)) ? "(not set" : "Length(" + config.GetValue<string>(Constants.AppInsightsKey).Length.ToString(CultureInfo.InvariantCulture))})");

            // always return 0 (success)
            return 0;
        }
    }
}
