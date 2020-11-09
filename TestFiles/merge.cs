using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace merge
{
    class Program
    {
        // merges benchmark.json, genre.json, rating.json and year.json
        // outputs new-benchmark.json
        static void Main(string[] args)
        {
            string[] genres = File.ReadAllLines("genre.json");
            string[] ratings = File.ReadAllLines("rating.json");
            string[] years = File.ReadAllLines("year.json");
            string[] benchmark = File.ReadAllLines("benchmark.json");

            int count = 0;
            int gIndex = 0;
            int rIndex = 0;
            int yIndex = 0;

            string ln;

            Console.WriteLine("{\n  \"requests\":\n  [");
            Console.WriteLine("    {\"path\":\"/version\",\"perfTarget\":{\"category\":\"Ignore\"}},\n");

            foreach(string line in benchmark)
            {
                if (line.Contains("/api/actors/nm") || line.Contains("/api/movies/tt"))
                {
                    ln = line.Substring(0, line.IndexOf(",\"validation")) + "},";
                    Console.WriteLine(ln);
                }
                else
                {
                    switch (count)
                    {
                        case 0:
                        default:
                            while (!genres[gIndex].Contains("/api/movies?"))
                            {
                                gIndex = gIndex >= genres.Length - 1 ? 0 : gIndex + 1;
                            }
                            Console.WriteLine(genres[gIndex]);
                            gIndex = gIndex >= genres.Length ? 0 : gIndex + 1;
                            count = 1;
                            break;
                        case 1:
                            while (!ratings[rIndex].Contains("/api/movies?"))
                            {
                                rIndex = rIndex >= ratings.Length - 1 ? 0 : rIndex + 1;
                            }
                            Console.WriteLine(ratings[rIndex]);
                            rIndex = rIndex >= ratings.Length ? 0 : rIndex + 1;
                            count++;
                            break;
                        case 2:
                            while (!years[yIndex].Contains("/api/movies?"))
                            {
                                yIndex = yIndex >= years.Length - 1 ? 0 : yIndex + 1;
                            }
                            Console.WriteLine(years[yIndex]);
                            yIndex = yIndex >= years.Length ? 0 : yIndex + 1;
                            count = 0;
                            break;
                    }
                }
            }

            Console.WriteLine("  ]\n}");
        }
    }
}
