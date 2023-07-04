﻿using System;
using System.Collections.Generic;
using Sprache;

namespace Octostache.Templates
{
    /// <summary>
    /// The top-level "thing that has a textual value" that
    /// can be manipulated or inserted into the output.
    /// </summary>
    abstract class ContentExpression : IInputToken
    {
        public Position? InputPosition { get; set; }
        public abstract IEnumerable<string> GetArguments();
    }
}
