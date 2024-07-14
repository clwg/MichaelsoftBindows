using EtwTracer.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EtwTracer.Logging
{

    internal class JsonOutput
    {
        //Serializes any object to a json string
        internal static string JsonSeralize<T>(T obj)
        {
            JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };
            JsonSerializerOptions joptions = jsonSerializerOptions;
            string jsonStr = JsonSerializer.Serialize(obj, joptions);
            
            Console.WriteLine(jsonStr);
            return jsonStr;
        }
    }
}
