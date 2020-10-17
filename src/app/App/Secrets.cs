// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;

namespace CSE.NextGenSymmetricApp
{
    /// <summary>
    /// Application secrets
    /// </summary>
    public class Secrets
    {
        public string AppInsightsKey { get; set; }
        public string CosmosServer { get; set; }
        public string CosmosKey { get; set; }
        public string CosmosDatabase { get; set; }
        public string CosmosCollection { get; set; }

        /// <summary>
        /// Get the secrets from the k8s volume
        /// </summary>
        /// <param name="volume">k8s volume name</param>
        /// <returns>Secrets or null</returns>
        public static Secrets GetSecrets(string volume = "secrets")
        {
            // get k8s secrets from files
            if (Directory.Exists(volume))
            {
                Secrets sec = new Secrets
                {
                    AppInsightsKey = GetSecretFromFile(volume, "AppInsightsKey"),
                    CosmosCollection = GetSecretFromFile(volume, "CosmosCollection"),
                    CosmosDatabase = GetSecretFromFile(volume, "CosmosDatabase"),
                    CosmosKey = GetSecretFromFile(volume, "CosmosKey"),
                    CosmosServer = GetSecretFromFile(volume, "CosmosUrl"),
                };

                return sec;
            }

            return null;
        }

        // read a secret from a k8s volume
        private static string GetSecretFromFile(string volume, string key)
        {
            string val = string.Empty;

            if (File.Exists($"{volume}/{key}"))
            {
                val = File.ReadAllText($"{volume}/{key}").Trim();
            }

            return val;
        }
    }
}
