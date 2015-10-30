using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LibGit2Sharp;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using MsBuild.GitCloneTask;

namespace UT_GitTask
{
    [TestClass]
    public class VersionResolverTests
    {
        [TestMethod]
        public void TestVesionResolver_SearchForRepositoryRootFolder()
        {
            Trace.WriteLine(VersionResolver.GetRootRepositoryDirectoryOf(Directory.GetCurrentDirectory()));
        }

        [TestMethod]
        public void TestLibGit2_CreateLocalBranches()
        {
            using (var myRepository = new Repository(@"C:\dev1\Gsx.Monitor.Project"))
            {
                Trace.WriteLine(string.Join("\n", from b in myRepository.Branches select $"{b.FriendlyName}({b.IsRemote})"));
            }
        }

        private class TraceLogger : ILogger
        {
            public void Debug(string message)
            {
                Trace.WriteLine($"DEBUG: {message}");
            }

            public void Log(string message)
            {
                Trace.WriteLine($"LOG: {message}");
            }

            public void Warn(string message)
            {
                Trace.WriteLine($"WARN: {message}");
            }
        }

        [TestMethod]
        public void TestVersionResolver_CheckoutBranchInDepencendyRepository()
        {
            var myRepositoryRootFolder = new DirectoryInfo(@"C:\dev1\GitReposTest\Repo1"); //GetRootRepositoryFolderOf(Directory.GetCurrentDirectory());
            var otherRepositoryRootFolder = new DirectoryInfo(@"C:\dev1\GitReposTest\Repo2");

            using (var myRepository = new Repository(myRepositoryRootFolder.FullName))
            using (var otherRepository = new Repository(otherRepositoryRootFolder.FullName))
            {
                VersionResolver.CheckoutBranchInDependencyRepository(otherRepository, myRepository, "", new TraceLogger());
            }
        }
    }
}
