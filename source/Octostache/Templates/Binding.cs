using System;
using System.Collections.Generic;

namespace Octostache.Templates
{
    class Binding : Dictionary<string, Binding>
    {
        readonly Dictionary<string, Binding> indexable; 

        public Binding(string? item = null)
            : base(StringComparer.OrdinalIgnoreCase)
        {
            Item = item;
            indexable = new Dictionary<string, Binding>(StringComparer.OrdinalIgnoreCase);
        }

        public string? Item { get; set; }

        public Dictionary<string, Binding> Indexable => indexable;
    }
}