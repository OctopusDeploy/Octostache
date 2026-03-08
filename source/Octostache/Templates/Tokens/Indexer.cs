using System;
using System.Collections.Generic;

namespace Octostache.Templates
{
    class Indexer : SymbolExpressionStep
    {
        public string? Index { get; }
        public SymbolExpression? Symbol { get; }
        public bool IsSymbol => Symbol != null;

        public Indexer(string? index)
        {
            Index = index;
        }

        public Indexer(SymbolExpression expression)
        {
            Symbol = expression;
        }

        public override string ToString() => "[" + (IsSymbol ? "#{" + Symbol + "}" : Index) + "]";

        public override IEnumerable<string> GetArguments() => Symbol?.GetArguments() ?? new string[0];

        public override bool Equals(SymbolExpressionStep? other) => other != null && Equals((other as Indexer)!);

        protected bool Equals(Indexer other) => base.Equals(other) && string.Equals(Index, other.Index) && Equals(Symbol, other.Symbol);

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Indexer) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable once SuggestVarOrType_BuiltInTypes
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (Index != null ? Index.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Symbol != null ? Symbol.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
