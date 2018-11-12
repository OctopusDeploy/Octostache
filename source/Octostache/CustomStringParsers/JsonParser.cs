using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octostache.Templates;
using YamlDotNet.Serialization;

namespace Octostache.CustomStringParsers
{
    internal static class YamlParser
    {
        internal static bool TryParse(Binding parentBinding, string property, out Binding subBinding)
        {
            subBinding = null;
            try
            {
                var obj = Deserializer.Deserialize<dynamic>(parentBinding.Item);

                if (obj is string) //obj is string || obj is int || obj is float || obj is bool)
                {
                    subBinding = new Binding(obj);
                    return true; 
                }

                if (obj is Dictionary<object, object>)
                {
                    subBinding = ConvertToBinding(obj[property]);
                    return true;
                }

                if (obj is List<object>)
                {
                    if (!int.TryParse(property, out var index))
                        return false;

                    subBinding = ConvertToBinding(obj[index]);
                    return true;
                }
            }
            catch (Exception ex)
            {

                return false;
            }

            return false;
        }


        internal static bool TryParse(Binding binding, out Binding[] subBindings)
        {
            subBindings = new Binding[0];
            return false;
        }
        
        private static readonly Serializer Serializer = new Serializer();
        private static readonly Deserializer Deserializer = new Deserializer();
        static Binding ConvertToBinding(dynamic res)
        {
            if(res is string)
            {
                return new Binding(res);
                       
            }
            return new Binding(Serializer.Serialize(res).Trim());
        }
    }
    
    internal static class JsonParser
    {
        internal static bool TryParse(Binding parentBinding, string property, out Binding subBinding)
        {
            subBinding = null;

            try
            {
                var obj = JsonConvert.DeserializeObject(parentBinding.Item);

                if (obj is JValue jvalue)
                {
                    return TryParseJValue(jvalue, out subBinding);
                }

                if (obj is JArray jarray)
                {
                    return TryParseJArray(jarray, property, out subBinding);
                }

                if (obj is JObject jobj)
                {
                    return TryParseJObject(jobj, property, out subBinding);
                }
            }
            catch (JsonException)
            {
                return false;
            }
            return false;
        }

        internal static bool TryParse(Binding binding, out Binding[] subBindings)
        {
            subBindings = new Binding[0];

            try
            {
                var obj = JsonConvert.DeserializeObject(binding.Item);

                if (obj is JArray jarray)
                {
                    return TryParseJArray(jarray, out subBindings);
                }

                if (obj is JObject jobj)
                {
                    return TryParseJObject(jobj, out subBindings);
                }
            }
            catch (JsonException)
            {
                return false;
            }
            return false;
        }

        static bool TryParseJObject(JObject jobj, out Binding[] subBindings)
        {
            subBindings = jobj.Properties().Select(p =>
            {
                var b = new Binding(p.Name)
                        {
                            { "Key", new Binding(p.Name)},
                            { "Value", ConvertJTokenToBinding(p.Value)}
                        };
                return b;
            }).ToArray();
            return true;
        }

        static bool TryParseJArray(JArray jArray, out Binding[] subBindings)
        {
            subBindings = jArray.Select(ConvertJTokenToBinding).ToArray();
            return true;
        }

        private static bool TryParseJValue(JValue jvalue, out Binding subBinding)
        {
            subBinding = new Binding(jvalue.Value<string>());
            return true;
        }

        private static bool TryParseJObject(JObject jobj, string property, out Binding subBinding)
        {
            subBinding = ConvertJTokenToBinding(jobj[property]);
            return true;
        }

        private static bool TryParseJArray(JArray jarray, string property, out Binding subBinding)
        {
            subBinding = null;

            if (!int.TryParse(property, out var index))
                return false;

            subBinding = ConvertJTokenToBinding(jarray[index]);
            return true;
        }

        static Binding ConvertJTokenToBinding(JToken token)
        {
            if (token is JValue)
            {
                return new Binding(token.Value<string>() ?? string.Empty);
            }
            return new Binding(JsonConvert.SerializeObject(token));
        }
    }
}
