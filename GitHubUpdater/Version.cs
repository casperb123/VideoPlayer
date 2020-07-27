using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GitHubUpdater
{
    public class Version : IEquatable<Version>
    {
        public int Major { get; private set; }
        public int Minor { get; private set; }
        public int Build { get; private set; }
        public int Revision { get; private set; }

        public Version(int major)
        {
            Major = major;
        }

        public Version(int major, int minor) : this(major)
        {
            Minor = minor;
        }

        public Version(int major, int minor, int build) : this(major, minor)
        {
            Build = build;
        }

        public Version(int major, int minor, int build, int revision) : this(major, minor, build)
        {
            Revision = revision;
        }

        public static bool operator >(Version first, Version second)
        {
            if (first.Major > second.Major)
                return true;
            else if (first.Minor > second.Minor)
                return true;
            else if (first.Build > second.Build)
                return true;
            else if (first.Revision > second.Revision)
                return true;

            return false;
        }

        public static bool operator <(Version first, Version second)
        {
            if (first.Major < second.Major)
                return true;
            else if (first.Minor < second.Minor)
                return true;
            else if (first.Build < second.Build)
                return true;
            else if (first.Revision < second.Revision)
                return true;

            return false;
        }

        public static bool operator ==(Version first, Version second)
        {
            if (first.Major == second.Major &&
                first.Minor == second.Minor &&
                first.Build == second.Build &&
                first.Revision == second.Revision)
                return true;

            return false;
        }

        public static bool operator !=(Version first, Version second)
        {
            if (first.Major != second.Major &&
                first.Minor != second.Minor &&
                first.Build != second.Build &&
                first.Revision != second.Revision)
                return true;

            return false;
        }

        public override string ToString()
        {
            string versionTxt = $"{Major}.{Minor}";

            if (Build > 0)
            {
                versionTxt += $".{Build}";
                if (Revision > 0)
                    versionTxt += $".{Revision}";
            }

            return versionTxt;
        }

        public static Version ConvertToVersion(string version)
        {
            version = version.Replace("v", "", true, CultureInfo.InvariantCulture).Split("-")[0];

            Regex regex = new Regex(@"\d+(?:\.\d+)+");

            if (regex.IsMatch(version))
            {
                var splitted = version.Split('.').Select(int.Parse).ToArray();

                if (splitted.Length == 1)
                    return new Version(splitted[0]);
                else if (splitted.Length == 2)
                    return new Version(splitted[0], splitted[1]);
                else if (splitted.Length == 3)
                    return new Version(splitted[0], splitted[1], splitted[2]);
                else if (splitted.Length >= 4)
                    return new Version(splitted[0], splitted[1], splitted[2], splitted[3]);
            }

            throw new FormatException("Version was in a invalid format");
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Version);
        }

        public bool Equals([AllowNull] Version other)
        {
            return other != null &&
                   Major == other.Major &&
                   Minor == other.Minor &&
                   Build == other.Build &&
                   Revision == other.Revision;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Major, Minor, Build, Revision);
        }
    }
}
