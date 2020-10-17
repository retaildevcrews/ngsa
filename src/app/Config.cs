// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace CSE.NextGenSymmetricApp
{
    public class Config
    {
        public string CosmosUrl { get; set; }
        public string CosmosKey { get; set; }
        public string CosmosDatabase { get; set; }
        public string CosmosCollection { get; set; }
        public string AppInsightsKey { get; set; }

        public Config()
        {
        }
    }
}
