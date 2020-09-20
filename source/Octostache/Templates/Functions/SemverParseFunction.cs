using Octopus.Versioning;

namespace Octostache.Templates.Functions
{
    internal class SemverParseFunction
    {
        public static string? SemverMajor(string? argument, string[] options)
        {
            if (argument == null)
            {
                return null;
            }

            return VersionFactory.TryCreateSemanticVersion(argument, out var version) ? version.Major.ToString() : null;
        }
        
        public static string? SemverMinor(string? argument, string[] options)
        {
            if (argument == null)
            {
                return null;
            }

            return VersionFactory.TryCreateSemanticVersion(argument, out var version) ? version.Minor.ToString() : null;
        }
        
        public static string? SemverPatch(string? argument, string[] options)
        {
            if (argument == null)
            {
                return null;
            }

            return VersionFactory.TryCreateSemanticVersion(argument, out var version) ? version.Patch.ToString() : null;
        }
        
        public static string? SemverRevision(string? argument, string[] options)
        {
            if (argument == null)
            {
                return null;
            }

            return VersionFactory.TryCreateSemanticVersion(argument, out var version) ? version.Revision.ToString() : null;
        }
        
        public static string? SemverRelease(string? argument, string[] options)
        {
            if (argument == null)
            {
                return null;
            }

            return VersionFactory.TryCreateSemanticVersion(argument, out var version) ? version.Release : null;
        }
        
        public static string? SemverMetadata(string? argument, string[] options)
        {
            if (argument == null)
            {
                return null;
            }

            return VersionFactory.TryCreateSemanticVersion(argument, out var version) ? version.Metadata : null;
        }
    }
}