using Octopus.Versioning;

namespace Octostache.Templates.Functions
{
    internal class MavenParseFunction
    {
        public static string? MavenMajor(string? argument, string[] options)
        {
            if (argument == null)
            {
                return null;
            }

            return VersionFactory.TryCreateMavenVersion(argument, out var version) ? version.Major.ToString() : null;
        }
        
        public static string? MavenMinor(string? argument, string[] options)
        {
            if (argument == null)
            {
                return null;
            }

            return VersionFactory.TryCreateMavenVersion(argument, out var version) ? version.Minor.ToString() : null;
        }
        
        public static string? MavenPatch(string? argument, string[] options)
        {
            if (argument == null)
            {
                return null;
            }

            return VersionFactory.TryCreateMavenVersion(argument, out var version) ? version.Patch.ToString() : null;
        }
        
        public static string? MavenRevision(string? argument, string[] options)
        {
            if (argument == null)
            {
                return null;
            }

            return VersionFactory.TryCreateMavenVersion(argument, out var version) ? version.Revision.ToString() : null;
        }
        
        public static string? MavenRelease(string? argument, string[] options)
        {
            if (argument == null)
            {
                return null;
            }

            return VersionFactory.TryCreateMavenVersion(argument, out var version) ? version.Release : null;
        }
    }
}