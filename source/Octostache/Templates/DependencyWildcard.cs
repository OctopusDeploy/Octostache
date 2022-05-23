using System;
using System.Collections.Generic;

namespace Octostache.Templates
{
    class DependencyWildcard : SymbolExpressionStep
    {
        public override string ToString()
        {
            return "*";
        }

        public override IEnumerable<string> GetArguments()
        {
            return new string[0];
        }
    }
}