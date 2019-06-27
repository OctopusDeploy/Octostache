using System;
using System.Collections.Generic;
using System.Linq;

namespace Octostache.Templates
{
    public class RecursiveDefinitionException : InvalidOperationException
    {
        internal RecursiveDefinitionException(SymbolExpression symbol, Stack<SymbolExpression> ancestorSymbolStack)
            : base ($"An attempt to parse the variable symbol \"{symbol}\" appears to have resulted in a self referencing " +
                    $"loop ({string.Join(" -> ", ancestorSymbolStack.Reverse().Select(x => x.ToString()))} -> {symbol}). " +
                    $"Ensure that recursive loops do not exist in the variable values.")
        {
        }
    }
}
