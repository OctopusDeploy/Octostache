namespace Octostache.Templates
{
    class Identifier : SymbolExpressionStep
    {
        readonly string text;

        public Identifier(string text)
        {
            this.text = text;
        }

        public string Text
        {
            get { return text; }
        }

        public override string ToString()
        {
            return Text;
        }
    }
}