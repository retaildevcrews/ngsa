// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace CSE.NextGenSymmetricApp.DataAccessLayer
{
    /// <summary>
    /// Data Access Layer for CosmosDB
    /// </summary>
    public partial class DAL
    {
        private const string GenresSelect = "select value m.genre from m where m.type = 'Genre' order by m.genre";

        /// <summary>
        /// Read the genres from CosmosDB
        /// </summary>
        /// <returns>List of strings</returns>
        public async Task<IEnumerable<string>> GetGenresAsync()
        {
            // get all genres as a list of strings
            // the "select value" converts m.genre to a string instead of a document
            List<string> results = new List<string>();

            IEnumerable<string> q = await InternalCosmosDBSqlQuery<string>(GenresSelect).ConfigureAwait(false);

            foreach (string g in q)
            {
                results.Add(g);
            }

            return results;
        }
    }
}