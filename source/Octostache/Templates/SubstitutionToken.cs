using System;
using System.Collections.Generic;

namespace Octostache.Templates
{
    /// <summary>
    /// Example: <code>#{Octopus.Action[Foo].Name</code>.
    /// </summary>
    class SubstitutionToken : TemplateToken
    {
        public SubstitutionToken(ContentExpression expression)
        {
            Expression = expression;
        }

        public ContentExpression Expression { get; }

        public override string ToString()
        {
            return "#{" + Expression + "}";
        }

        public override IEnumerable<string> GetArguments()
        {
            return Expression.GetArguments();
        }
    }
}