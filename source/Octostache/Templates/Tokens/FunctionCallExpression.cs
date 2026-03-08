using System;
using System.Collections.Generic;
using System.Linq;

namespace Octostache.Templates
{
    /// <summary>
    /// Syntactically this appears as the <code>| FilterName</code> construct, where
    /// the (single) argument is specified to the left of the bar. Under the hood this
    /// same AST node will also represent classic <code>Function(Foo,Bar)</code> expressions.
    /// </summary>
    class FunctionCallExpression : ContentExpression
    {
        public TemplateToken[] Options { get; }
        public string Function { get; }
        public ContentExpression Argument { get; }

        readonly bool filterSyntax;

        public FunctionCallExpression(bool filterSyntax, string function, ContentExpression argument, params TemplateToken[] options)
        {
            Options = options;
            this.filterSyntax = filterSyntax;
            Function = function;
            Argument = argument;
        }

        IInputToken[] GetAllArguments()
        {
            var tokens = new List<IInputToken>();
            if (Argument.InputPosition != null)
                tokens.Add(Argument);

            tokens.AddRange(Options);
            return tokens.ToArray();
        }

        public override string ToString()
        {
            if (filterSyntax)
                return $"{Argument} | {Function}{(Options.Any() ? " " : "")}{string.Join(" ", Options.Select(t => t is TextToken ? $"\"{t.ToString()}\"" : t.ToString()))}";

            return $"{Function} ({string.Join(", ", GetAllArguments().Select(t => t.ToString()))})";
        }

        public override IEnumerable<string> GetArguments()
        {
            return Argument.GetArguments()
                .Concat(Options.SelectMany(o => o.GetArguments()));
        }
    }
}
