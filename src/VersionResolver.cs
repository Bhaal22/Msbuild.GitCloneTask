using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MsBuild.GitCloneTask
{
    public interface ILogger
    {
        void Debug(string message);
        void Log(string message);
        void Warn(string message);
    }

    public static class VersionResolver
    {
        public const string BranchPrefix = "v";
        public const string BuildBranchName = "build";

        public static readonly IDictionary<string, int> FallbackBranches = new Dictionary<string, int>() {["stable"] = 1,["develop"] = 2,["master"] = 3 };

        public static DirectoryInfo GetRootRepositoryDirectoryOf(string path)
        {
            var currentDirectoryInfo = new DirectoryInfo(path);
            do
            {
                var subDirNames = from d in currentDirectoryInfo.GetDirectories() select d.Name;
                if (subDirNames.Contains(".git")) return currentDirectoryInfo;
            }
            while ((currentDirectoryInfo = Directory.GetParent(currentDirectoryInfo.FullName)) != null);
            return null;
        }

        public static IList<VersionLabel> GetTagVersionLabels(Repository repository, string prefix)
        {
            var tagVersionLabels =
                from tag in repository.Tags
                let tagVersionLabel = VersionLabel.Parse(tag.FriendlyName, prefix)
                where tagVersionLabel != null
                select tagVersionLabel;

            return tagVersionLabels.ToList();
        }

        public static IList<VersionLabel> GetBranchVersionLabels(Repository repository, string prefix = BranchPrefix)
        {
            var branchVersionLabels =
                from branch in repository.Branches
                let branchVersionLabel = VersionLabel.Parse(branch.FriendlyName, prefix)
                where branchVersionLabel != null
                select branchVersionLabel;

            return branchVersionLabels.ToList();
        }

        public static void CheckoutBranchInDependencyRepository(Repository otherRepository, Repository myRepository, string myShortName, ILogger logger)
        {
            // Check HEAD branch
            string headBranchName = myRepository.Head.FriendlyName;

            var currentBranchVersionLabel = VersionLabel.Parse(headBranchName, BranchPrefix); // Looking for my version label branch => no myShortName
            if (currentBranchVersionLabel != null)
            {
                logger.Log($"My repository: HEAD branch '{headBranchName}' is a version branch for '{myShortName}': {currentBranchVersionLabel}({currentBranchVersionLabel.Raw})");
                CheckoutVersionBranchInDependencyRepository(otherRepository, myRepository, myShortName, currentBranchVersionLabel, logger);
            }
            else
            {
                logger.Log($"My repository: HEAD branch '{headBranchName}' is a NON-version branch: develop, stable, etc... Looking for a branch with the same name in the other repository...");
                CheckoutNonVersionBranchInDependencyRepository(otherRepository, headBranchName, logger);
            }
        }

        private static void CheckoutVersionBranchInDependencyRepository(Repository otherRepository, Repository myRepository, string myShortName, VersionLabel currentBranchVersionLabel, ILogger logger)
        {
            // The HEAD points to a branch which is a version branch => check if there's at least a version label tag on the HEAD commit
            // In case multiple version label tags are found, take the most recent one
            var headCommit = myRepository.Commits.FirstOrDefault();
            if (headCommit == null) 
                throw new InvalidOperationException($"My repository: the repository is empty: couldn't find any commit");
            logger.Log($"My repository: HEAD commit: {headCommit.ToString()}. Checking if there's at least a version label tag on the HEAD commit...");

            var headCommitVersionLabelTags =
                from tag in myRepository.Tags
                where tag.Target == headCommit
                let tagVersionLabel = VersionLabel.Parse(tag.FriendlyName, "") // Looking for _my_ version tags => No prefix
                where tagVersionLabel != null
                orderby tagVersionLabel descending
                select new { Tag = tag, VersionLabel = tagVersionLabel };
            logger.Log($"My repository: HEAD commit version label tags: [{string.Join(",", from hct in headCommitVersionLabelTags select $"{hct.VersionLabel}({hct.VersionLabel.Raw})")}]");

            var headCommitLastVersionLabelTag = headCommitVersionLabelTags.FirstOrDefault();
            if (headCommitLastVersionLabelTag == null)
            {
                var otherRepositoryBranch = BranchPrefix + myShortName + currentBranchVersionLabel;

                logger.Log($"My repository: no valid version label tag has been found on the HEAD commit => we look for the branch '{otherRepositoryBranch}' in the other repository...");

                var otherRepositoryVersionLabelBranch =
                    (from branch in otherRepository.Branches
                     where branch.FriendlyName == otherRepositoryBranch
                     select branch).SingleOrDefault();

                if (otherRepositoryVersionLabelBranch != null)
                {
                    logger.Log($"Other repository: branch '{otherRepositoryBranch}' found => checking it out...");

                    otherRepository.Checkout(otherRepositoryVersionLabelBranch, new CheckoutOptions() { CheckoutModifiers = CheckoutModifiers.Force });
                }
                else 
                {
                    var nextBranchVersionLabel = currentBranchVersionLabel + 1;
                    logger.Log($"Other repository: unable to find a brach named '{otherRepositoryBranch}' => we look for a tag version label < '{myShortName}{nextBranchVersionLabel}' in the other repository...");

                    var otherRepositoryTagVersionLabels = GetTagVersionLabels(otherRepository, myShortName);
                    var otherRepositoryVersionLabel = otherRepositoryTagVersionLabels.FindLastVersionLabelSmallerThan(nextBranchVersionLabel);

                    logger.Log($"Other repository: last tag version label < '{myShortName}{nextBranchVersionLabel}': {otherRepositoryVersionLabel}({otherRepositoryVersionLabel.Raw})");

                    var otherRepositoryVersionLabelTag =
                        (from tag in otherRepository.Tags
                         where tag.FriendlyName == otherRepositoryVersionLabel.Raw
                         select tag).Single();

                    ResetBuildBranchToTag(otherRepository, logger, otherRepositoryVersionLabel, otherRepositoryVersionLabelTag);
                }   
            }
            else
            {
                if (headCommitVersionLabelTags.Count() > 1)
                    throw new InvalidOperationException($"My repository: multiple version label tags found on the HEAD commit: [{string.Join(",", from hct in headCommitVersionLabelTags select hct.Tag.FriendlyName)}].");

                var headCommitVersionLabelTag = headCommitVersionLabelTags.Single().Tag;
                var headCommitVersionLabel = headCommitVersionLabelTags.Single().VersionLabel;

                logger.Log($"My repository: a valid version label tag has been found on the HEAD commit: {headCommitVersionLabelTag.FriendlyName} => we look for the last tag <= '{myShortName}{headCommitVersionLabel}' in the other repository");

                // A valid version label tag has been found on the HEAD commit => we look for the last tag <= this tag in the other repository
                // Remark: the tag in the other repo must have a prefix equal to the shortName of my repo
                // Remark: we are in a version branch (i.e. a branch named x[.y[.z[.w]]]) => we only look for version label tags (i.e. fixed 
                // instant of time in the past) and never a branch in the other repo with the same name (that can change state over time).
                var otherRepositoryTagVersionLabels = GetTagVersionLabels(otherRepository, myShortName);
                var otherRepositoryVersionLabel = otherRepositoryTagVersionLabels.FindLastVersionLabelSmallerOrEqualThan(headCommitVersionLabel);
                logger.Log($"Other repository: last tag version label <= '{myShortName}{headCommitVersionLabel}': {otherRepositoryVersionLabel}({otherRepositoryVersionLabel.Raw})");

                var otherRepositoryVersionLabelTag =
                    (from tag in otherRepository.Tags
                     where tag.FriendlyName == otherRepositoryVersionLabel.Raw
                     select tag).Single();

                ResetBuildBranchToTag(otherRepository, logger, otherRepositoryVersionLabel, otherRepositoryVersionLabelTag);
            }
        }

        private static void ResetBuildBranchToTag(Repository repository, ILogger logger, VersionLabel versionLabel, Tag versionLabelTag)
        {
            var buildBranch =
                (from branch in repository.Branches
                 where branch.FriendlyName == BuildBranchName
                 select branch).SingleOrDefault();
            if (buildBranch == null)
            {
                logger.Log($"The branch '{BuildBranchName}' doesn't exist: creating it and resetting it to '{versionLabel.Raw}'");

                // If the build branch doesn't exist, create it and check it out
                buildBranch = repository.CreateBranch(BuildBranchName, versionLabelTag.Target as Commit);
                repository.Checkout(buildBranch, new CheckoutOptions() { CheckoutModifiers = CheckoutModifiers.Force });
            }
            else
            {
                logger.Log($"The branch '{BuildBranchName}' already exists: checking it out and resetting it to '{versionLabel.Raw}'");

                // If the build branch exists, check it out and do an hard reset
                repository.Checkout(buildBranch, new CheckoutOptions() { CheckoutModifiers = CheckoutModifiers.Force });
                repository.Reset(ResetMode.Hard, versionLabelTag.Target as Commit);
            }
        }

        private static void CheckoutNonVersionBranchInDependencyRepository(Repository otherRepository, string headBranchName, ILogger logger)
        {
            // The HEAD points to a branch which is a NON-version branch 
            // Well-known cases: develop => develop, stable => stable, ...

            // Find a branch with the same exact name in the other repository
            var otherRepositoryBranch =
                (from branch in otherRepository.Branches
                where branch.FriendlyName.Equals(headBranchName, StringComparison.InvariantCultureIgnoreCase)
                select branch).SingleOrDefault();

            if (otherRepositoryBranch == null)
            {
                // If a branch with the same exact name doesn't exist in the other repository => fallback 
                // to the following branches in the following order: stable => develop => master => exception!
                logger.Warn($"Other repository: no branch '{headBranchName}' found. Branches available: [{string.Join(",", from branch in otherRepository.Branches select branch.FriendlyName)}]");

                var fallbackBranch =
                    (from branch in otherRepository.Branches
                    where FallbackBranches.ContainsKey(branch.FriendlyName)
                    orderby FallbackBranches[branch.FriendlyName]
                    select branch).FirstOrDefault();

                if (fallbackBranch == null)
                    throw new InvalidOperationException($"Other repository: couldn't find a fallback branch. Fallback branches: [{string.Join(",", FallbackBranches.Keys)}]. Found branches: [{string.Join(",", from branch in otherRepository.Branches select branch.FriendlyName)}]");

                logger.Warn($"Other repository: fallback to '{fallbackBranch.FriendlyName}'");
                otherRepositoryBranch = fallbackBranch;
            }

            logger.Log($"Other repository: perform checkout on '{otherRepositoryBranch.FriendlyName}'");
            otherRepository.Checkout(otherRepositoryBranch, new CheckoutOptions() { CheckoutModifiers = CheckoutModifiers.Force });
        }
    }
}
