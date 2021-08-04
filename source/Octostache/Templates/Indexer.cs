using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Octostache.Templates
{
    class Indexer : SymbolExpressionStep
    {
        public Indexer(string? index)
        {
            Index = index;
        }

        public Indexer(SymbolExpression expression)
        {
            Symbol = expression;
        }

        public string? Index { get; }

        public SymbolExpression? Symbol { get; }

        public bool IsSymbol => Symbol != null; 

        public override string ToString()
        {
            return "[" + (IsSymbol ? "#{"+ Symbol +"}" : Index) + "]";
        }

        public override IEnumerable<string> GetArguments() => Symbol?.GetArguments() ?? (Index != null ? new string[] { $"[${Index}]" } : new string[0]);

        public override bool Equals(SymbolExpressionStep? other) => other != null && Equals((other as Indexer)!);

        protected bool Equals(Indexer other)
        {
            return base.Equals(other) && string.Equals(Index, other.Index) && Equals(Symbol, other.Symbol);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Indexer) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (Index != null ? Index.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Symbol != null ? Symbol.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}