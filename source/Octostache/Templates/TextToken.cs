﻿using System;
using System.Collections.Generic;

namespace Octostache.Templates
{
    class TextToken : TemplateToken
    {
        public IEnumerable<string> Text { get; }

        public TextToken(params string[] text)
        {
            Text = text;
        }

        public override string ToString() => string.Concat(Text).Replace("#{", "##{");

        public override IEnumerable<string> GetArguments() => new string[0];
    }
}
