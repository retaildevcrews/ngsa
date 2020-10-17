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
        public string CosmosUrl { get; set; }
        public string CosmosKey { get; set; }
        public string CosmosDatabase { get; set; }
        public string CosmosCollection { get; set; }

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
                    CosmosUrl = GetSecretFromFile(volume, "CosmosUrl"),
                };

                return sec;
            }

            return null;
        }

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
