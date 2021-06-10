using Octostache.Templates;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Octostache
{
    public class VariableDictionary : IEnumerable<KeyValuePair<string, string>>
    {
        readonly Dictionary<string, string?> variables = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        string? storageFilePath;
        Binding? binding;
        Dictionary<string, Func<string?, string[], string?>> extensions = new Dictionary<string, Func<string?, string[], string?>>();

        public VariableDictionary() : this(null)
        {
        }

        public VariableDictionary(string? storageFilePath)
        {
            if (string.IsNullOrWhiteSpace(storageFilePath)) return;
            this.storageFilePath = Path.GetFullPath(storageFilePath);
            Reload();
        }

        Binding Binding => binding ?? (binding = PropertyListBinder.CreateFrom(variables));

        /// <summary>
        /// Sets a variable value.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="value">The value of the variable.</param>
        public void Set(string name, string? value)
        {
            if (name == null) return;
            variables[name] = value;
            binding = null;
            Save();
        }

        /// <summary>
        /// Gets or sets a variable by name.
        /// </summary>
        /// <param name="name">The name of the variable to set.</param>
        /// <returns>The current (evaluated) value of the variable.</returns>
        public string? this[string name]
        {
            get => Get(name);
            set => Set(name, value);
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
        /// If this variable dictionary was read from a file, reloads all variables from the file.
        /// </summary>
        public void Reload()
        {
            if (!string.IsNullOrWhiteSpace(storageFilePath))
            {
                VariablesFileFormatter.Populate(variables, storageFilePath);
                binding = null;
            }
        }

        public void Save(string path)
        {
            storageFilePath = Path.GetFullPath(path);
            Save();
        }

        public void Save()
        {
            if (!string.IsNullOrWhiteSpace(storageFilePath))
            {
                VariablesFileFormatter.Persist(variables, storageFilePath);
            }
        }

        public string SaveAsString()
        {
            using (var writer = new StringWriter())
            {
                VariablesFileFormatter.Persist(variables, writer);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Performs the raw (not evaluated) value of a variable.
        /// </summary>
        /// <param name="variableName">Name of the variable.</param>
        /// <returns>The value of the variable, or null if one is not defined.</returns>
        public string? GetRaw(string variableName)
        {
            if (variables.TryGetValue(variableName, out string? variable) && variable != null)
                return variable;

            return null;
        }

        /// <summary>
        /// Gets the value of a variable, or returns a default value if the variable is not defined. If the variable contains an expression, it will be evaluated first.
        /// </summary>
        /// <param name="variableName">The name of the variable.</param>
        /// <param name="defaultValue">The default value to return.</param>
        /// <returns>The value of the variable, or the default value if the variable is not defined.</returns>
        [return: NotNullIfNotNull("defaultValue")]
        public string? Get(string variableName, string? defaultValue = null)
        {
            return Get(variableName, out string _, defaultValue);
        }

        /// <summary>
        /// Gets the value of a variable, or returns a default value if the variable is not defined. If the variable contains an expression, it will be evaluated first.
        /// </summary>
        /// <param name="variableName">The name of the variable.</param>
        /// <param name="error">Any parsing errors silently found.</param>
        /// <param name="defaultValue">The default value to return.</param>
        /// <returns>The value of the variable, or the default value if the variable is not defined.</returns>
        [return: NotNullIfNotNull("defaultValue")]
        public string? Get(string variableName, out string? error, string? defaultValue = null)
        {
            error = null;
            if (!variables.TryGetValue(variableName, out string? variable) || variable == null)
                return defaultValue;

            return Evaluate(variable, out error);
        }

        /// <summary>
        /// Evaluates a given expression as if it were the value of a variable.
        /// </summary>
        /// <param name="expressionOrVariableOrText">The value or expression to evaluate.</param>
        /// <param name="error">Any parsing errors silently found.</param>
        /// <param name="haltOnError">Stop parsing if an error is found.</param>
        /// <returns>The result of the expression.</returns>
        [return: NotNullIfNotNull("expressionOrVariableOrText")]
        public string? Evaluate(string? expressionOrVariableOrText, out string? error, bool haltOnError = true)
        {
            error = null;
            if (expressionOrVariableOrText == null) return null;

            if (CanEvaluationBeSkippedForExpression(expressionOrVariableOrText))
                return expressionOrVariableOrText;

            if (!TemplateParser.TryParseTemplate(expressionOrVariableOrText, out var template, out error, haltOnError))
                return expressionOrVariableOrText;

            using (var writer = new StringWriter())
            {
                TemplateEvaluator.Evaluate(template, Binding, writer, extensions, out var missingTokens);
                if (missingTokens.Any())
                {
                    var tokenList = string.Join(", ", missingTokens.Select(token => "'" + token + "'"));
                    error = string.Format("The following tokens were unable to be evaluated: {0}", tokenList);
                }


                return writer.ToString();
            }
        }

        /// <summary>
        /// Evaluates a given expression for truthiness.
        /// </summary>
        /// <param name="expressionOrVariableOrText">The value or expression to evaluate.</param>
        /// <returns>Whether the expression evaluates with no errors and the result is truthy (Not empty, 0 or false).</returns>
        public bool EvaluateTruthy(string? expressionOrVariableOrText)
        {
            var result = Evaluate(expressionOrVariableOrText, out var error);
            return string.IsNullOrWhiteSpace(error) && result != null && TemplateEvaluator.IsTruthy(result);
        }

        /// <summary>
        /// Evaluates a given expression as if it were the value of a variable.
        /// </summary>
        /// <param name="expressionOrVariableOrText">The value or expression to evaluate.</param>
        /// <returns>The result of the expression.</returns>
        [return: NotNullIfNotNull("expressionOrVariableOrText")]
        public string? Evaluate(string? expressionOrVariableOrText)
        {
            return Evaluate(expressionOrVariableOrText, out string _);
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
            var text = Get(variableName);
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

        /// <summary>
        /// Returns any index values for a collection.
        /// For example, given keys: Package[A].Name, Package[B].Name
        /// GetIndexes("Package") would return {A, B}
        /// </summary>
        /// <param name="variableCollectionName"></param>
        /// <returns>A list of index values for the specified collection name.</returns>
        public List<string> GetIndexes(string variableCollectionName)
        {
            if (string.IsNullOrWhiteSpace(variableCollectionName))
                throw new ArgumentOutOfRangeException(nameof(variableCollectionName),
                    $"{nameof(variableCollectionName)} must not be null or empty");

            if (!TemplateParser.TryParseIdentifierPath(variableCollectionName, out var symbolExpression))
                throw new Exception($"Could not evaluate indexes for path {variableCollectionName}");

            var context = new EvaluationContext(Binding, TextWriter.Null);
            var bindings = context.ResolveAll(symbolExpression, out _);
            // ReSharper disable once RedundantEnumerableCastCall
            return bindings.Select(b => b.Item).Where(x => x != null).Cast<string>().ToList();

        }

        /// <summary>
        /// Determines whether an expression/variable value/text needs to be evaluated before being used.
        /// If true is returned from this method, the raw value of <paramref name="expressionOrVariableOrText" />
        /// can be used without running it through a VariableDictionary. Even if false is returned, the value
        /// may not contain subsitution tokens, and may be unchanged after evaluation.
        /// </summary>
        /// <param name="expressionOrVariableOrText">The variable to evaluate</param>
        /// <returns>False if the variable contains something that looks like a substitution tokens, otherwise true</returns>
        public static bool CanEvaluationBeSkippedForExpression(string expressionOrVariableOrText)
            => expressionOrVariableOrText == null || !expressionOrVariableOrText.Contains("#{");

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return variables.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(string key, string? value)
        {
            Set(key, value);
        }

        /// <summary>
        /// Adds a custom extension function
        /// </summary>
        /// <param name="name"></param>
        /// <param name="func"></param>
        public void AddExtension(string name, Func<string?, string[], string?> func)
        {
            // Naming conflicts with BuiltInFunctions?
            extensions[name.ToLowerInvariant()] = func;
        }
    }
}