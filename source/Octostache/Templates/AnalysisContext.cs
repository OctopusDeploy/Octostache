using System;
using System.Collections.Generic;
using System.Linq;

namespace Octostache.Templates
{
    class AnalysisContext
    {
        readonly AnalysisContext parent;
        readonly Identifier identifier;
        readonly SymbolExpression expansion;

        AnalysisContext(AnalysisContext parent, Identifier identifier, SymbolExpression expansion)
        {
            this.parent = parent;
            this.identifier = identifier;
            this.expansion = expansion;
        }

        public string Expand(SymbolExpression expression) => new SymbolExpression(Expand(expression.Steps)).ToString();

        public IEnumerable<SymbolExpressionStep> Expand(IEnumerable<SymbolExpressionStep> expression)
        {
            var nodes = expression.ToArray();
            if (nodes.FirstOrDefault() is Identifier first
                && string.Equals(first.Text, identifier.Text, StringComparison.OrdinalIgnoreCase))
            {
                nodes = expansion.Steps.Concat(new[] { new DependencyWildcard() }).Concat(nodes.Skip(1)).ToArray();
            }

            nodes = parent.Expand(nodes).ToArray();

            return nodes;
        }

        public AnalysisContext BeginChild(Identifier ident, SymbolExpression expan) => new AnalysisContext(this, ident, expan);
    }
}
