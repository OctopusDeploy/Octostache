﻿using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octostache.Templates;

namespace Octostache.CustomStringParsers
{
    internal static class JsonParser
    {
        internal static bool TryParse(Binding parentBinding, string property, out Binding subBinding)
        {
            subBinding = null;

            try
            {
                var obj = JsonConvert.DeserializeObject(parentBinding.Item);

                var jvalue = obj as JValue;
                if (jvalue != null)
                {
                    return TryParseJValue(jvalue, out subBinding);
                }

                var jarray = obj as JArray;
                if (jarray != null)
                {
                    return TryParseJArray(jarray, property, out subBinding);
                }

                var jobj = obj as JObject;
                if (jobj != null)
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

                var jarray = obj as JArray;
                if (jarray != null)
                {
                    return TryParseJArray(jarray, out subBindings);
                }

                var jobj = obj as JObject;
                if (jobj != null)
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
            int index;
            subBinding = null;

            if (!int.TryParse(property, out index))
                return false;

            subBinding = ConvertJTokenToBinding(jarray[index]);
            return true;
        }

        static Binding ConvertJTokenToBinding(JToken token)
        {
            if(token == null)
            {
                return new Binding(string.Empty);
            }
            if (token is JValue)
            {
                return new Binding(token.Value<string>() ?? string.Empty);
            }
            return new Binding(JsonConvert.SerializeObject(token));
        }
    }
}
