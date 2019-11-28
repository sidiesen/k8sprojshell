using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Templating.BuildTask
{
    static class JsonHelpers
    {
        public static JsonElement p(this JsonElement jsonElement, string propertyName)
        {
            return jsonElement.GetProperty(propertyName);
        }

        public static string s(this JsonElement jsonElement)
        {
            return jsonElement.GetString();
        }
    }
}