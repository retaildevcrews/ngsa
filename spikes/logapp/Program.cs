using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace logapp
{
    /// <summary>
    /// A simple app to test e2e k8s logging
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            int counter = 1;
            Dictionary<string, object> log;
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            };

            while (true)
            {
                log = new Dictionary<string, object>
                {
                    {"Date", DateTime.UtcNow },
                };

                if (counter % 10 == 0)
                {
                    log.Add("EventType", EventType.Error);
                    log.Add("Message", "some random error");
                }
                else if (counter % 5 == 0)
                {
                    log.Add("EventType", EventType.Warning);
                    log.Add("Message", "some random warning");
                }
                else
                {
                    log.Add("EventType", EventType.Counter);
                    log.Add("Counter", counter);
                }

                switch ((EventType)log["EventType"])
                {
                    case EventType.Error:
                    case EventType.Warning:
                        Console.Error.WriteLine(JsonSerializer.Serialize(log, options));
                        break;
                    default:
                        // write log to stdout
                        Console.WriteLine(JsonSerializer.Serialize(log, options));
                        break;
                }

                // increment counter 
                counter = counter < 100000000 ? counter + 1 : 1;

                // sleep
                Thread.Sleep(1000);
            }
        }
    }

    public enum EventType
    {
        Counter, Warning, Error
    }
}
