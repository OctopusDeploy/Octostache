using System;
using Octopus.Versioning.Octopus;

namespace Octostache.Templates.Functions
{
    class VersionParseFunction
    {
        static readonly OctopusVersionParser OctopusVersionParser = new OctopusVersionParser();

        public static string? VersionMajor(string? argument, string[] options)
        {
            if (argument == null)
                return null;

            return OctopusVersionParser.Parse(argument).Major.ToString();
        }

        public static string? VersionMinor(string? argument, string[] options)
        {
            if (argument == null)
                return null;

            return OctopusVersionParser.Parse(argument).Minor.ToString();
        }

        public static string? VersionPatch(string? argument, string[] options)
        {
            if (argument == null)
                return null;

            return OctopusVersionParser.Parse(argument).Patch.ToString();
        }

        public static string? VersionRevision(string? argument, string[] options)
        {
            if (argument == null)
                return null;

            return OctopusVersionParser.Parse(argument).Revision.ToString();
        }

        public static string? VersionRelease(string? argument, string[] options)
        {
            if (argument == null)
                return null;

            return OctopusVersionParser.Parse(argument).Release;
        }

        public static string? VersionReleasePrefix(string? argument, string[] options)
        {
            if (argument == null)
                return null;

            return OctopusVersionParser.Parse(argument).ReleasePrefix;
        }

        public static string? VersionReleaseCounter(string? argument, string[] options)
        {
            if (argument == null)
                return null;

            return OctopusVersionParser.Parse(argument).ReleaseCounter;
        }

        public static string? VersionMetadata(string? argument, string[] options)
        {
            if (argument == null)
                return null;

            return OctopusVersionParser.Parse(argument).Metadata;
        }
    }
}