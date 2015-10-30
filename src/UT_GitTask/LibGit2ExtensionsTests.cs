using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using LibGit2Sharp;
using MsBuild.GitCloneTask;

namespace UT_GitTask
{
    [TestClass]
    public class LibGit2ExtensionsTests
    {
        [TestMethod]
        public void TestLibGit2Extensions_CheckoutAllRemoteBranches()
        {
            var otherRepositoryRootFolder = new DirectoryInfo(@"C:\dev1\GitReposTest\Repo3");

            using (var otherRepository = new Repository(otherRepositoryRootFolder.FullName))
            {
                otherRepository.CheckoutAllRemoteBranches();
            }
        }
    }
}
