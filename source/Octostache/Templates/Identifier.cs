using System.Collections.Generic;

namespace Octostache.Templates
{
    class Identifier : SymbolExpressionStep
    {
        public Identifier(string text)
        {
            Text = text;
        }

        public string Text { get; }

        public override string ToString()
        {
            return Text;
        }

        public override IEnumerable<string> GetArguments() => new[] {Text};

        public override bool Equals(SymbolExpressionStep other) => Equals(other as Identifier);


        protected bool Equals(Identifier other)
        {
            return base.Equals(other) && string.Equals(Text, other.Text);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
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