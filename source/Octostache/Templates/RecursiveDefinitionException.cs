using System;
using System.Collections.Generic;
using System.Linq;

namespace Octostache.Templates
{
    public class RecursiveDefinitionException : InvalidOperationException
    {
        const string MessageTemplate = "An attempt to parse the variable symbol \"{0}\" appears to have resulted in a self referencing loop ({1} -> {0}). Ensure that recursive loops do not exist in the variable values.";

        internal RecursiveDefinitionException(SymbolExpression symbol, Stack<SymbolExpression> ancestorSymbolStack)
            : base(string.Format(MessageTemplate, symbol, string.Join(" -> ", ancestorSymbolStack.Reverse().Select(x => x.ToString()))))
        {
        }
    }
}
