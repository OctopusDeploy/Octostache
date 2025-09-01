using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octostache.Templates;
#if HAS_NULLABLE_REF_TYPES
using System.Diagnostics.CodeAnalysis;
#endif

namespace Octostache.CustomStringParsers
{
    static class JsonParser
    {
        internal static bool TryParse(Binding parentBinding, string property, [NotNullWhen(true)] out Binding? subBinding)
        {
            subBinding = null;

            try
            {
                var obj = JsonConvert.DeserializeObject(parentBinding.Item!);

                if (obj is JValue jvalue)
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
                var obj = JsonConvert.DeserializeObject(binding.Item!);

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

        static bool TryParseJObject(JObject jObject, out Binding[] subBindings)
        {
            subBindings = jObject.Properties().Select(p =>
            {
                var b = new Binding(p.Name)
                {
                    { "Key", new Binding(p.Name) },
                    { "Value", ConvertJTokenToBinding(p.Value) },
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

        static bool TryParseJValue(JValue jvalue, out Binding subBinding)
        {
            subBinding = new Binding(jvalue.Value<string>());
            return true;
        }

        static bool TryParseJObject(JObject jObject, string property, out Binding subBinding)
        {
            subBinding = ConvertJTokenToBinding(jObject[property]!);
            return true;
        }

        static bool TryParseJArray(JArray jarray, string property, [NotNullWhen(true)] out Binding? subBinding)
        {
            int index;
            subBinding = null;

            if (!int.TryParse(property, out index))
                return false;

            var value = index > 0 && index < jarray.Count ? jarray[index] : null;

            subBinding = ConvertJTokenToBinding(value);
            return true;
        }

        static Binding ConvertJTokenToBinding(JToken? token)
        {
            if (token == null)
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
