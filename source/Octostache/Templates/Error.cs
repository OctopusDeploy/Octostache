using System;
using System.Runtime.CompilerServices;

namespace Octostache.Templates
{
    public static class Error
    {
        public static string Format(string message, string? context = null, [CallerMemberName] string? caller = null) => $"[{caller}{(!string.IsNullOrWhiteSpace(context) ? $" {context}" : string.Empty)} error: {message}]";
        public static string Format(Exception ex, string? context = null, [CallerMemberName] string? caller = null) => Format(ex.Message, context, caller);
    }
}