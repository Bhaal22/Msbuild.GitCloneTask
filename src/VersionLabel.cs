using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MsBuild.GitCloneTask
{
    public class VersionLabel : IComparable<VersionLabel>
    {
        private static readonly Regex VersionTagRegex = new Regex(@"^([A-Za-z]*)([0-9]+)(?:\.([0-9]+))?(?:\.([0-9]+))?(?:\.([0-9]+))?([^\.].*)?");

        public static VersionLabel Parse(string value, string matchPrefix)
        {
            Match match = VersionTagRegex.Match(value);
            if (!match.Success) return null;
            var prefix = match.Groups[1].Value;
            var majorVersion = int.Parse(match.Groups[2].Value);
            var minorVersion = !string.IsNullOrEmpty(match.Groups?[3].Value) ? int.Parse(match.Groups[3].Value) : (int?)null;
            var majorBuild = !string.IsNullOrEmpty(match.Groups?[4].Value) ? int.Parse(match.Groups[4].Value) : (int?)null;
            var minorBuild = !string.IsNullOrEmpty(match.Groups?[5].Value) ? int.Parse(match.Groups[5].Value) : (int?)null;

            if (matchPrefix != null && prefix != matchPrefix) return null;

            return new VersionLabel(value, majorVersion, minorVersion, majorBuild, minorBuild);
        }

        public string Raw { get; }
        public int MajorVersion { get; }
        public int? MinorVersion { get; }
        public int? MajorBuild { get; }
        public int? MinorBuild { get; }

        public VersionLabel(string raw, int majorVersion, int? minorVersion = null, int? majorBuild = null, int? minorBuild = null)
        {
            Raw = raw;
            MajorVersion = majorVersion;
            MinorVersion = minorVersion;
            MajorBuild = majorBuild;
            MinorBuild = minorBuild;
        }

        public static VersionLabel operator +(VersionLabel versionLabel, int increment)
        {
            if (versionLabel == null) throw new ArgumentNullException(nameof(versionLabel));

            int majorVersion = versionLabel.MajorVersion;
            int? minorVersion = versionLabel.MinorVersion;
            int? majorBuild = versionLabel.MajorBuild;
            int? minorBuild = versionLabel.MinorBuild;

            if (minorBuild != null) minorBuild += increment;
            else if (majorBuild != null) majorBuild += increment;
            else if (minorVersion != null) minorVersion += increment;
            else majorVersion += increment;

            string raw = FormatVersion(majorVersion, minorVersion, majorBuild, minorBuild);

            return new VersionLabel(raw, majorVersion, minorVersion, majorBuild, minorBuild);         
        }

        public static VersionLabel operator -(VersionLabel versionLabel, int increment) => versionLabel + (-increment);

        public static VersionLabel operator ++(VersionLabel versionLabel) => versionLabel + 1;

        public static VersionLabel operator --(VersionLabel versionLabel) => versionLabel - 1;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            var other = obj as VersionLabel;
            if (other == null) return false;
            return
                MajorVersion == other.MajorVersion &&
                MinorVersion == other.MinorVersion &&
                MajorBuild == other.MajorBuild &&
                MinorBuild == other.MinorBuild;
        }

        public override int GetHashCode() => MajorVersion ^ MinorVersion ?? 0 ^ MajorBuild ?? 0 ^ MinorBuild ?? 0;

        protected static string FormatVersion(int majorVersion, int? minorVersion, int? majorBuild, int? minorBuild)
        {
            var minorVersionStr = minorVersion != null ? "." + minorVersion.ToString() : string.Empty;
            var majorBuildStr = majorBuild != null ? "." + majorBuild.ToString() : string.Empty;
            var minorBuildStr = minorBuild != null ? "." + minorBuild.ToString() : string.Empty;
            return $"{majorVersion}{minorVersionStr}{majorBuildStr}{minorBuildStr}";
        }

        public override string ToString() => FormatVersion(MajorVersion, MinorVersion, MajorBuild, MinorBuild);

        public int CompareTo(VersionLabel other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            if (MajorVersion != other.MajorVersion) return MajorVersion - other.MajorVersion;
            if (MinorVersion != other.MinorVersion) return (MinorVersion ?? -1) - (other.MinorVersion ?? -1);
            if (MajorBuild != other.MajorBuild) return (MajorBuild ?? -1) - (other.MajorBuild ?? -1);
            if (MinorBuild != other.MinorBuild) return (MinorBuild ?? -1) - (other.MinorBuild ?? -1);

            return 0;
        }

    }

    public static class VersionLabelExtensions
    {
        public static VersionLabel FindLastVersionLabelSmallerOrEqualThan(this IList<VersionLabel> versionLabels, VersionLabel versionLabel)
        {
            var sortedLabels = from vt in versionLabels orderby vt select vt;
            var lowerLabel = (from vt in sortedLabels where vt.CompareTo(versionLabel) <= 0 orderby vt select vt).LastOrDefault();
            if (lowerLabel == null) throw new InvalidOperationException($"{nameof(FindLastVersionLabelSmallerOrEqualThan)}: unable to find a version tag lower than '{versionLabel}': versionTags = [{string.Join(",", versionLabels)}]");
            return lowerLabel;
        }

        public static VersionLabel FindLastVersionLabelSmallerThan(this IList<VersionLabel> versionLabels, VersionLabel versionLabel)
        {
            var sortedLabels = from vt in versionLabels orderby vt select vt;
            var lowerLabel = (from vt in sortedLabels where vt.CompareTo(versionLabel) < 0 orderby vt select vt).LastOrDefault();
            if (lowerLabel == null) throw new InvalidOperationException($"{nameof(FindLastVersionLabelSmallerOrEqualThan)}: unable to find a version tag lower than '{versionLabel}': versionTags = [{string.Join(",", versionLabels)}]");
            return lowerLabel;
        }
    }

}
