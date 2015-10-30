using LibGit2Sharp;
using System;
using System.Diagnostics;
using System.Linq;

namespace MsBuild.GitCloneTask
{
    public static class LibGit2Extensions
    {
        public static void CheckoutAllRemoteBranches(this Repository repository, bool onlyUntracked = true)
        {
            var localBranches =
                from branch in repository.Branches
                where !branch.IsRemote
                select branch;

            var untrackedRemoteBranches =
                from remoteBranch in repository.Branches
                where remoteBranch.IsRemote
                where !onlyUntracked ||
                    (from localBranch in localBranches 
                    where remoteBranch.FriendlyName.EndsWith("/" + localBranch.FriendlyName)
                    select localBranch).Count() == 0
                select remoteBranch;

            foreach (var untrackedRemoteBranch in untrackedRemoteBranches)
            {
                var simpleName = untrackedRemoteBranch.FriendlyName.Split('/')?[1];
                if (simpleName == null) throw new InvalidOperationException($"{nameof(LibGit2Extensions)}.{nameof(CheckoutAllRemoteBranches)}: the untracked remote branch name '{untrackedRemoteBranch.FriendlyName}' is not in the form '(remote)/(branch)'");
                Branch branch = repository.CreateBranch(simpleName, untrackedRemoteBranch.Tip);
                repository.Branches.Update(branch, (updater) => 
                { 
                    updater.Remote = untrackedRemoteBranch.Remote.Name; 
                    updater.UpstreamBranch = untrackedRemoteBranch.CanonicalName;
                });

            }
        }
    }
}
