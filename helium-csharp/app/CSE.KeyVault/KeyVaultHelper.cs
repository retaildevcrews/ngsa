﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Logging;

namespace CSE.KeyVault
{
    /// <summary>
    /// Static helper methods for working with Key Vault
    /// </summary>
    public sealed class KeyVaultHelper
    {
        /// <summary>
        /// Build the Key Vault URL from the name
        /// </summary>
        /// <param name="name">Key Vault Name</param>
        /// <param name="keyvaultConnection">KeyVault URL</param>
        /// <returns>URL to Key Vault</returns>
        public static bool BuildKeyVaultConnectionString(string name, out string keyvaultConnection)
        {
            keyvaultConnection = name?.Trim();

            // name is required
            if (string.IsNullOrEmpty(keyvaultConnection))
            {
                return false;
            }

            // build the URL
            if (!keyvaultConnection.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                keyvaultConnection = "https://" + keyvaultConnection;
            }

            if (!keyvaultConnection.EndsWith(".vault.azure.net/", StringComparison.OrdinalIgnoreCase) && !keyvaultConnection.EndsWith(".vault.azure.net", StringComparison.OrdinalIgnoreCase))
            {
                keyvaultConnection += ".vault.azure.net/";
            }

            if (!keyvaultConnection.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                keyvaultConnection += "/";
            }

            return true;
        }

        /// <summary>
        /// Validate the keyvault name
        /// </summary>
        /// <param name="name">string</param>
        /// <returns>bool</returns>
        public static bool ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            name = name.Trim();

            if (name.Length < 3 || name.Length > 24)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get a valid key vault client
        /// AKS takes time to spin up the first pod identity, so
        ///   we retry for up to 90 seconds
        /// </summary>
        /// <param name="kvUrl">URL of the key vault</param>
        /// <param name="authType">MI, CLI or VS</param>
        /// <param name="keyVaultTestKey">Keyvault key to test the connection</param>
        /// <param name="logger">ILogger</param>
        /// <returns>KeyVaultClient</returns>
        public static async Task<KeyVaultClient> GetKeyVaultClient(string kvUrl, AuthenticationType authType, string keyVaultTestKey, ILogger logger = null)
        {
            // retry Managed Identity for 90 seconds
            //   AKS has to spin up an MI pod which can take a while the first time on the pod
            DateTime timeout = DateTime.Now.AddSeconds(90.0);

            // use MI as default
            string authString = "RunAs=App";

#if DEBUG
            // Only support CLI and VS credentials in debug mode
            switch (authType)
            {
                case AuthenticationType.CLI:
                    authString = "RunAs=Developer; DeveloperTool=AzureCli";
                    break;
                case AuthenticationType.VS:
                    authString = "RunAs=Developer; DeveloperTool=VisualStudio";
                    break;
            }
#else
            if (authType != AuthenticationType.MI)
            {
                string warningMsg = "Release builds require MI authentication for Key Vault";
                if (logger == null)
                {
                    Console.WriteLine(warningMsg);
                }
                else
                {
                    logger.LogWarning(warningMsg);
                }

                return null;
            }
#endif

            KeyVaultClient keyVaultClient;

            while (true)
            {
                try
                {
                    AzureServiceTokenProvider tokenProvider = new AzureServiceTokenProvider(authString);

                    // use Managed Identity (MI) for secure access to Key Vault
                    keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback));

                    // read a key to make sure the connection is valid
                    await keyVaultClient.GetSecretAsync(kvUrl, keyVaultTestKey).ConfigureAwait(false);

                    // return the client
                    return keyVaultClient;
                }
                catch (Exception ex)
                {
                    if (DateTime.Now <= timeout && authType == AuthenticationType.MI)
                    {
                        // retry MI connections for pod identity
#if DEBUG
                        // Don't retry in debug mode
                        string message = $"KeyVault:Exception: Unable to connect to Key Vault using MI";
                        if (logger == null)
                        {
                            Console.WriteLine(message);
                        }
                        else
                        {
                            logger.LogError(message);
                        }

                        return null;
#else
                        if (logger == null)
                        {
                            Console.WriteLine($"KeyVault:Retry");
                        }
                        else
                        {
                            logger.LogInformation($"KeyVault:Retry");
                        }

                        await Task.Delay(1000).ConfigureAwait(false);
#endif
                    }
                    else
                    {
                        // log and fail
                        string error = $"{ex}\nKeyVault:Exception: {ex.Message}";
                        if (logger == null)
                        {
                            Console.WriteLine(error);
                        }
                        else
                        {
                            logger.LogError(error);
                        }

                        return null;
                    }
                }
            }
        }
    }
}
