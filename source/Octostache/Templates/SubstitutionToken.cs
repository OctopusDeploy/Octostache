using System;
using System.Collections.Generic;

namespace Octostache.Templates
{
    /// <summary>
    /// Example: <code>#{Octopus.Action[Foo].Name</code>.
    /// </summary>
    class SubstitutionToken : TemplateToken
    {
        public ContentExpression Expression { get; }

        public SubstitutionToken(ContentExpression expression)
        {
            Expression = expression;
        }

        public override string ToString() => "#{" + Expression + "}";

        public override IEnumerable<string> GetArguments() => Expression.GetArguments();
    }
}
