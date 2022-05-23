using System;
using System.Collections.Generic;
using System.Linq;

namespace Octostache.Templates
{
    static class PropertyListBinder
    {
        public static Binding CreateFrom(IDictionary<string, string?> properties)
        {
            var result = new Binding();
            foreach (var property in properties)
                if (TemplateParser.TryParseIdentifierPath(property.Key, out var pathExpression) && pathExpression != null)
                    Add(result, pathExpression.Steps, property.Value ?? "");
            return result;
        }

        static void Add(Binding result, IList<SymbolExpressionStep> steps, string value)
        {
            var first = steps.FirstOrDefault();

            if (first == null)
            {
                result.Item = value;
                return;
            }

            Binding next;

            switch (first)
            {
                case Identifier iss:
                {
                    if (!result.TryGetValue(iss.Text, out next))
                        result[iss.Text] = next = new Binding();

                    break;
                }
                // ReSharper disable once MergeIntoPattern
                case Indexer ix when ix.Index != null:
                {
                    if (!result.Indexable.TryGetValue(ix.Index, out next))
                        result.Indexable[ix.Index] = next = new Binding(ix.Index);

                    break;
                }
                default:
                    throw new NotImplementedException("Unknown step type: " + first);
            }

            Add(next, steps.Skip(1).ToList(), value);
        }
    }
}