using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Octostache.Templates;

namespace Octostache
{
    public class VariableDictionary
    {
        readonly Dictionary<string, string> variables;

        public VariableDictionary() : this(null)
        {
        }

        public VariableDictionary(Dictionary<string, string> variables)
        {
            variables = variables ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (variables.Comparer != StringComparer.OrdinalIgnoreCase)
            {
                // Rather than copying/creating a new dictionary (slow), force them to set the comparer properly
                throw new Exception("The dictionary passed into VariableDictionary must have a comparer set to StringComparer.OrdinalIgnoreCase, to ensure case-insensitive matches.");
            }

            this.variables = variables;
        }

        /// <summary>
        /// Sets a variable value.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="value">The value of the variable.</param>
        public void Set(string name, string value)
        {
            if (name == null) return;

            variables[name] = value;
        }

        /// <summary>
        /// Sets a variable to a list of strings, by joining each value with a separator.
        /// </summary>
        /// <param name="variableName">The name of the variable to set.</param>
        /// <param name="values">The list of values.</param>
        /// <param name="separator">The separator character to join by.</param>
        public void SetStrings(string variableName, IEnumerable<string> values, string separator = ",")
        {
            var value = string.Join(separator, values.Where(v => !string.IsNullOrWhiteSpace(v)));
            Set(variableName, value);
        }

        /// <summary>
        /// Sets a variable to a list of values, by putting each value on a newline. Mostly used for file paths.
        /// </summary>
        /// <param name="variableName">The name of the variable to set.</param>
        /// <param name="values">The list of values.</param>
        public void SetPaths(string variableName, IEnumerable<string> values)
        {
            SetStrings(variableName, values, Environment.NewLine);
        }

        /// <summary>
        /// Performs the raw (not evaluated) value of a variable.
        /// </summary>
        /// <param name="variableName">Name of the variable.</param>
        /// <returns>The value of the variable, or null if one is not defined.</returns>
        public string GetRaw(string variableName)
        {
            string variable;
            if (variables.TryGetValue(variableName, out variable) && variable != null)
                return variable;

            return null;
        }

        /// <summary>
        /// Gets the value of a variable, or returns a default value if the variable is not defined. If the variable contains an expression, it will be evaluated first.
        /// </summary>
        /// <param name="variableName">The name of the variable.</param>
        /// <param name="defaultValue">The default value to return.</param>
        /// <returns>The value of the variable, or the default value if the variable is not defined.</returns>
        public string Get(string variableName, string defaultValue = null)
        {
            string variable;
            if (!variables.TryGetValue(variableName, out variable) || variable == null)
                return defaultValue;

            return Evaluate(variable);
        }

        /// <summary>
        /// Evaluates a given expression as if it were the value of a variable.
        /// </summary>
        /// <param name="expressionOrVariableOrText">The value or expression to evaluate.</param>
        /// <returns>The result of the expression.</returns>
        public string Evaluate(string expressionOrVariableOrText)
        {
            if (expressionOrVariableOrText == null) return null;

            Template template;
            string error;
            if (!TemplateParser.TryParseTemplate(expressionOrVariableOrText, out template, out error))
                return expressionOrVariableOrText;

            var binder = PropertyListBinder.CreateFrom(variables);

            using (var writer = new StringWriter())
            {
                TemplateEvaluator.Evaluate(template, binder, writer, true);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Gets a list of strings, assuming each path is separated by commas or some other separator character. If the variable contains an expression, it will be evaluated first.
        /// </summary>
        /// <param name="variableName">The name of the variable to find.</param>
        /// <param name="separators">The separators to split the list by. Defaults to a comma if no other separators are passed.</param>
        /// <returns>The list of strings, or an empty list if the value is null or empty.</returns>
        public List<string> GetStrings(string variableName, params char[] separators)
        {
            separators = separators ?? new char[0];
            if (separators.Length == 0) separators = new[] { ',' };

            var value = Get(variableName);
            if (string.IsNullOrWhiteSpace(value))
                return new List<string>();

            var values = value.Split(separators)
                .Select(v => v.Trim())
                .Where(v => v != "");

            return values.ToList();
        }

        /// <summary>
        /// Gets a list of paths, assuming each path is separated by newlines. If the variable contains an expression, it will be evaluated first.
        /// </summary>
        /// <param name="variableName">The name of the variable to find.</param>
        /// <returns>The list of strings, or an empty list if the value is null or empty.</returns>
        public List<string> GetPaths(string variableName)
        {
            return GetStrings(variableName, '\r', '\n');
        }

        /// <summary>
        /// Gets a given variable by name. If the variable contains an expression, it will be evaluated. Converts the variable to a boolean using <code>bool.TryParse()</code>. Returns a given  
        /// default value if the variable is not defined, is empty, or isn't a valid boolean value.
        /// </summary>
        /// <param name="variableName">The name of the variable to find.</param>
        /// <param name="defaultValueIfUnset">The default value to return if the variable is not defined.</param>
        /// <returns>The boolean value of the variable, or the default value.</returns>
        public bool GetFlag(string variableName, bool defaultValueIfUnset = false)
        {
            bool value;
            var text = GetRaw(variableName);
            if (string.IsNullOrWhiteSpace(text) || !bool.TryParse(text, out value))
            {
                value = defaultValueIfUnset;
            }

            return value;
        }

        /// <summary>
        /// Gets a given variable by name. If the variable contains an expression, it will be evaluated. Converts the variable to an integer using <code>int.TryParse()</code>. Returns null 
        /// if the variable is not defined.
        /// </summary>
        /// <param name="variableName">The name of the variable to find.</param>
        /// <returns>The integer value of the variable, or null if not defined.</returns>
        public int? GetInt32(string variableName)
        {
            int value;
            var text = Get(variableName);
            if (string.IsNullOrWhiteSpace(text) || !int.TryParse(text, out value))
            {
                return null;
            }

            return value;
        }

        /// <summary>
        /// Gets a given variable by name. If the variable contains an expression, it will be evaluated. Throws an <see cref="ArgumentOutOfRangeException"/> if the variable is not defined.
        /// </summary>
        /// <param name="name">The name of the variable to find.</param>
        /// <returns>The value </returns>
        public string Require(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            var value = Get(name);
            if (string.IsNullOrEmpty(value))
                throw new ArgumentOutOfRangeException("name", "The variable '" + name + "' is required but no value is set.");
            return value;
        }

        /// <summary>
        /// Gets the names of all variables in this dictionary.
        /// </summary>
        /// <returns>A list of variable names.</returns>
        public List<string> GetNames()
        {
            return variables.Keys.ToList();
        }
    }
}