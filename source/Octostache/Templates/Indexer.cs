namespace Octostache.Templates
{
    class Indexer : SymbolExpressionStep
    {
        public Indexer(string index)
        {
            Index = index;
        }

        public Indexer(SymbolExpression expression)
        {
            Symbol = expression;
            
        }

        public string Index { get; }

        public SymbolExpression Symbol { get; }

        public bool IsSymbol => Symbol != null; 

        public override string ToString()
        {
            return "[" + (IsSymbol ? Symbol.ToString() : Index) + "]";
        }
    }
}