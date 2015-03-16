using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Octostache.Templates
{
    class EvaluationContext
    {
        readonly Binding binding;
        readonly TextWriter output;
        readonly EvaluationContext parent;

        public EvaluationContext(Binding binding, TextWriter output, EvaluationContext parent = null)
        {
            this.binding = binding;
            this.output = output;
            this.parent = parent;
        }

        public TextWriter Output
        {
            get { return output; }
        }

        public string Resolve(SymbolExpression expression)
        {
            var val = WalkTo(expression);
            if (val == null) return "";
            return val.Item ?? "";
        }

        public string ResolveOptional(SymbolExpression expression)
        {
            var val = WalkTo(expression);
            if (val == null) return null;
            return val.Item;
        }

        Binding WalkTo(SymbolExpression expression)
        {
            var val = binding;

            foreach (var step in expression.Steps)
            {
                var iss = step as Identifier;
                if (iss != null)
                {
                    if (val.TryGetValue(iss.Text, out val))
                        continue;
                }
                else
                {
                    var ix = step as Indexer;
                    if (ix != null)
                    {
                        if (ix.Index == "*" && val.Indexable.Count > 0)
                        {
                            val = val.Indexable.First().Value;
                            continue;
                        }

                        if (val.Indexable.TryGetValue(ix.Index, out val))
                            continue;
                    }
                    else
                    {
                        throw new NotImplementedException("Unknown step type: " + step);
                    }
                }

                if (parent == null)
                    return null;

                return parent.WalkTo(expression);
            }

            if (val != null && val.Item != null)
            {
                Template template;
                string error;
                if (TemplateParser.TryParseTemplate(val.Item, out template, out error))
                {
                    using (var x = new StringWriter())
                    {
                        TemplateEvaluator.Evaluate(template, binding, x, true);
                        x.Flush();
                        return new Binding(x.ToString());
                    }
                }
            }

            return val;
        }

        public IEnumerable<Binding> ResolveAll(SymbolExpression collection)
        {
            var val = WalkTo(collection);
            if (val == null) return Enumerable.Empty<Binding>();

            if (val.Indexable.Count != 0)
                return val.Indexable.Select(c => c.Value);

            if (val.Item != null)
                return val.Item.Split(',').Select(s => new Binding(s));

            return Enumerable.Empty<Binding>();
        }

        public EvaluationContext BeginChild(Binding locals)
        {
            return new EvaluationContext(locals, Output, this);
        }
    }
}