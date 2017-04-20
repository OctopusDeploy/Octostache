using System;
using System.Collections;
using Sprache;

namespace Octostache.Templates
{
    /// <summary>
    /// A segment of a <see cref="SymbolExpression"/>,
    /// e.g. <code>Octopus</code>, <code>[Foo]</code>.
    /// </summary>
    abstract class SymbolExpressionStep : IInputToken
    {
        public Position InputPosition { get; set; }

        public virtual bool Equals(SymbolExpressionStep other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(InputPosition, other.InputPosition);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SymbolExpressionStep) obj);
        }

        public override int GetHashCode()
        {
            return (InputPosition != null ? InputPosition.GetHashCode() : 0);
        }
    }
}