using System;
using System.Collections.Generic;

namespace Octostache.Templates
{
    class Identifier : SymbolExpressionStep
    {
        public string Text { get; }

        public Identifier(string text)
        {
            Text = text;
        }

        public override string ToString() => Text;

        public override IEnumerable<string> GetArguments()
        {
            return new[] { Text };
        }

        public override bool Equals(SymbolExpressionStep? other) => Equals(other as Identifier);

        protected bool Equals(Identifier? other) => base.Equals(other) && string.Equals(Text, other?.Text);

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Identifier) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ (Text != null ? Text.GetHashCode() : 0);
            }
        }
    }
}
