using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Octostache.Templates
{
    /// <summary>
    /// A value, identified using dotted/bracketed notation, e.g.:
    /// <code>Octopus.Action[Name].Foo</code>. This would classically
    /// be represented using nesting "property expressions" rather than a path, but in the
    /// current very simple language a path is more convenient to work with.
    /// </summary>
    class SymbolExpression : ContentExpression
    {
        readonly SymbolExpressionStep[] steps;

        public SymbolExpression(IEnumerable<SymbolExpressionStep> steps)
        {
            this.steps = steps.ToArray();
        }

        public SymbolExpressionStep[] Steps
        {
            get { return steps; }
        }

        public override string ToString()
        {
            var result = new StringBuilder();
            var identifierJoin = "";
            foreach (var step in Steps)
            {
                if (step is Identifier)
                    result.Append(identifierJoin);

                result.Append(step);

                identifierJoin = ".";
            }

            return result.ToString();
        }

        private sealed class StepsEqualityComparer : IEqualityComparer<SymbolExpression>
        {
            public bool Equals(SymbolExpression x, SymbolExpression y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.steps.SequenceEqual(y.steps);
            }

            public int GetHashCode(SymbolExpression obj)
            {
                return obj.steps?.GetHashCode() ?? 0;
            }
        }

        public static IEqualityComparer<SymbolExpression> StepsComparer { get; } = new StepsEqualityComparer();
    }
}
