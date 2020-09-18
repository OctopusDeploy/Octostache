using System;
using System.Collections.Generic;

namespace Octostache.Templates
{
    class Binding : Dictionary<string, Binding>
    {
        public Binding(string? item = null)
            : base(StringComparer.OrdinalIgnoreCase)
        {
            Item = item;
            Indexable = new Dictionary<string, Binding>(StringComparer.OrdinalIgnoreCase);
        }

        public string? Item { get; set; }

        public Dictionary<string, Binding> Indexable { get; }
    }
}