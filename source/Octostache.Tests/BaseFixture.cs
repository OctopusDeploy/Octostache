using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Octostache.Tests
{
    public abstract class BaseFixture
    {
        protected BaseFixture()
        {
            // The TemplateParser Cache is retained between tests. A little reflection to clear it.
            var parser = typeof(VariableDictionary).Assembly.GetType("Octostache.Templates.TemplateParser");
            var clearMethod = parser?.GetMethod("ClearCache", BindingFlags.NonPublic | BindingFlags.Static);
            clearMethod?.Invoke(null, new object[] { });
        }

        protected string Evaluate(string template, IDictionary<string, string> variables, bool haltOnError = true)
        {
            var dictionary = new VariableDictionary();
            foreach (var pair in variables)
                dictionary[pair.Key] = pair.Value;

            return dictionary.Evaluate(template, out _, haltOnError);
        }

        protected bool EvaluateTruthy(string template, IDictionary<string, string> variables)
        {
            var dictionary = new VariableDictionary();
            foreach (var pair in variables)
                dictionary[pair.Key] = pair.Value;
            return dictionary.EvaluateTruthy(template);
        }

        protected VariableDictionary ParseVariables(string variableDefinitions)
        {
            var variables = new VariableDictionary();

            var items = variableDefinitions.Split(';');
            foreach (var item in items)
            {
                var pair = item.Split('=');
                var key = pair.First();
                var value = pair.Last();
                variables[key] = value;
            }

            return variables;
        }
    }
}