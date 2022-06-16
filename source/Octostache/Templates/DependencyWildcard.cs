using System;
using System.Collections.Generic;

namespace Octostache.Templates
{
    class DependencyWildcard : SymbolExpressionStep
    {
        public override string ToString() => "*";

        public override IEnumerable<string> GetArguments() => new string[0];
    }
}
