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
                    return ParseJValue(jvalue, out subBinding);
                }

                var jarray = obj as JArray;
                if (jarray != null)
                {
                    return ParseJArray(jarray, property, out subBinding);
                }

                var jobj = obj as JObject;
                if (jobj != null)
                {
                    return ParseJObject(jobj, property, out subBinding);
                }
            }
            catch (JsonException)
            {
                return false;
            }
            return false;
        }

        private static bool ParseJValue(JValue jvalue, out Binding subBinding)
        {
            subBinding = new Binding(jvalue.Value<string>());
            return true;
        }

        private static bool ParseJObject(JObject jobj, string property, out Binding subBinding)
        {
            var subProperty = jobj[property];
            if (subProperty is JValue)
            {
                subBinding = new Binding(subProperty.Value<string>());
            }
            else
            {
                subBinding = new Binding(JsonConvert.SerializeObject(subProperty));
            }

            return true;
        }

        private static bool ParseJArray(JArray jarray, string property, out Binding subBinding)
        {
            int index;
            subBinding = null;

            if (!int.TryParse(property, out index))
                return false;

            var subProperty = jarray[index];
            if (subProperty is JValue)
            {
                subBinding = new Binding(subProperty.Value<string>());
            }
            else
            {
                subBinding = new Binding(JsonConvert.SerializeObject(subProperty));
            }

            return true;
        }
    }
}
