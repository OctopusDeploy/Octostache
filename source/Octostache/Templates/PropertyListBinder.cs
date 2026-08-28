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
            {
                if (TemplateParser.TryParseIdentifierPath(property.Key, out var pathExpression) && pathExpression != null)
                {
                    Add(result, pathExpression.Steps, property.Value ?? "");
                }
            }

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
                    {
                        result[iss.Text] = next = new Binding();
                    }

                    break;
                }
                // ReSharper disable once MergeIntoPattern
                case Indexer ix when ix.Index != null:
                {
                    if (!result.Indexable.TryGetValue(ix.Index, out next))
                    {
                        result.Indexable[ix.Index] = next = new Binding(ix.Index);

                        // Expose the index as a `Key` child binding so `#{x.Key}` works when iterating
                        // an indexed collection, matching how JSON-backed collections behave. The seeded
                        // `Item` above is overwritten whenever a value is assigned directly at the index
                        // (e.g. certificate variables), so `#{x}` alone cannot be relied on for the key.
                        // A user-defined `Collection[index].Key` variable will find this binding and
                        // overwrite its `Item`, so real variable data always wins.
                        next["Key"] = new Binding(ix.Index);
                    }

                    break;
                }
                default:
                    throw new NotImplementedException("Unknown step type: " + first);
            }

            Add(next, steps.Skip(1).ToList(), value);
        }
    }
}
