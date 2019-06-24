﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octostache.CustomStringParsers;

namespace Octostache.Templates
{

    class EvaluationContext
    {
        readonly Binding binding;
        readonly EvaluationContext parent;
        readonly Stack<SymbolExpression> symbolStack = new Stack<SymbolExpression>();

        public EvaluationContext(Binding binding, TextWriter output, EvaluationContext parent = null)
        {
            this.binding = binding;
            this.Output = output;
            this.parent = parent;
        }

        public TextWriter Output { get; }

        public string Resolve(SymbolExpression expression, out string[] missingTokens)
        {
            var val = WalkTo(expression, out missingTokens);
            if (val == null) return "";
            return val.Item ?? "";
        }

        private void ValidateThatRecursionIsNotOccuring(SymbolExpression expression)
        {
            var ancestor = this;
            while(ancestor != null)
            {
                if (ancestor.symbolStack.Contains(expression, SymbolExpression.StepsComparer))
                {
                    throw new RecursiveDefinitionException(expression, ancestor.symbolStack);
                }
                ancestor = ancestor.parent;
            }
        }

        public string ResolveOptional(SymbolExpression expression, out string[] missingTokens)
        {
            var val = WalkTo(expression, out missingTokens);
            if (val == null) return null;
            return val.Item;
        }

        public Binding Walker(TemplateToken token, out string[] missingTokens)
        {
            
            missingTokens = null;
            return null;
        }

        Binding WalkTo(SymbolExpression expression, out string[] missingTokens)
        {
            ValidateThatRecursionIsNotOccuring(expression);
            symbolStack.Push(expression);

            try
            {
                var val = binding;
                missingTokens = new string[0];

                
                expression = CopyExpression(expression);

                foreach (var step in expression.Steps)
                {
                    var iss = step as Identifier;
                    if (iss != null)
                    {
                        Binding newVal;
                        if (val.TryGetValue(iss.Text, out newVal))
                        {
                            val = newVal;
                            continue;
                        }

                        if (TryCustomParsers(val, iss.Text, out newVal))
                        {
                            val = newVal;
                            continue;
                        }
                    }
                    else
                    {
                        var ix = step as Indexer;
                        if (ix != null)
                        {
                            if (ix.IsSymbol)
                            {
                                // Substitution should have taken place in previous CopyExpression above. 
                                // If not then it must not be found.
                                return null;
                            }

                            if (ix.Index == "*" && val.Indexable.Count > 0)
                            {
                                val = val.Indexable.First().Value;
                                continue;
                            }


                            Binding newVal;
                            if (val.Indexable.TryGetValue(ix.Index, out newVal))
                            {
                                val = newVal;
                                continue;
                            }

                            if (TryCustomParsers(val, ix.Index, out newVal))
                            {
                                val = newVal;
                                continue;
                            }
                        }
                        else
                        {
                            throw new NotImplementedException("Unknown step type: " + step);
                        }
                    }

                    if (parent == null)
                        return null;

                    return parent.WalkTo(expression, out missingTokens);
                }
                return ParseTemplate(val, out missingTokens);
            }
            finally
            {
                symbolStack.Pop();
            }
        }


        Binding ParseTemplate(Binding b, out string[] missingTokens)
        {
            if (b?.Item != null)
            {
                Template template;
                string error;
                if (TemplateParser.TryParseTemplate(b.Item, out template, out error))
                {
                    using (var x = new StringWriter())
                    {
                        var context = new EvaluationContext(new Binding(), x, this);

                        TemplateEvaluator.Evaluate(template, context, out missingTokens);
                        x.Flush();
                        return new Binding(x.ToString());
                    }
                }
            }
            missingTokens = new string[0];
            return b;
        }

        bool TryCustomParsers(Binding parentBinding, string property, out Binding subBinding)
        {

            subBinding = null;
            if (string.IsNullOrEmpty(parentBinding.Item) || string.IsNullOrEmpty(property))
                return false;


            if (parentBinding.Item.Contains("#{")) //Shortcut the inner variable replacement if no templates are detected
            {
                try
                {
                    string[] missingTokens;
                    parentBinding = ParseTemplate(parentBinding, out missingTokens);
                }
                catch (InvalidOperationException ex)
                {
                    if(ex.Message.Contains("self referencing loop"))
                        return false;
                }
            }


            return JsonParser.TryParse(parentBinding, property, out subBinding);
        }

        private SymbolExpression CopyExpression(SymbolExpression expression)
        {
            //any indexers that are lookups, do them now so we are in the right context
            //take a copy so the lookup version remains for later use
            return new SymbolExpression(expression.Steps.Select(s =>
            {
                var indexer = s as Indexer;
                if (indexer != null && indexer.IsSymbol)
                {
                    string[] missing;
                    var index = WalkTo(indexer.Symbol, out missing);
                    
                    return index == null
                        ? new Indexer(CopyExpression(indexer.Symbol))
                        : new Indexer(index.Item);
                }
                return s;
            }));
        }

        public IEnumerable<Binding> ResolveAll(SymbolExpression collection, out string[] missingTokens)
        {
            var val = WalkTo(collection, out missingTokens);
            if (val == null)
                return Enumerable.Empty<Binding>();

            if (val.Indexable.Count != 0)
                return val.Indexable.Select(c => c.Value);

            if (val.Item == null)
                return Enumerable.Empty<Binding>();

            Binding[] bindings;
            if (JsonParser.TryParse(new Binding(val.Item), out bindings))
            {
                return bindings;
            }

            return val.Item.Split(',').Select(s => new Binding(s));
        }

        public EvaluationContext BeginChild(Binding locals)
        {
            return new EvaluationContext(locals, Output, this);
        }
    }
}