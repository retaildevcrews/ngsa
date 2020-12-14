// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Imdb.Model
{
    public class GenreObject
    {
        public string Id { get; set; }
        public string Genre { get; set; }
        public string PartitionKey { get; set; }
        public string Type { get; set; }
    }
}
