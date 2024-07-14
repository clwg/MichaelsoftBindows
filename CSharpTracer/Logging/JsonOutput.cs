using System.Text.Json;

namespace CsharpTracer.Logging
{
    internal class JsonOutput
    {
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
