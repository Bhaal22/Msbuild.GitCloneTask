using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MsBuild.GitCloneTask;
using System.Collections.Generic;
using System.Linq;

namespace UT_GitTask
{
    [TestClass]
    public class VersionLabelTests
    {
        [TestMethod]
        public void TestVersionTag_Equals()
        {
            Assert.AreEqual(new VersionLabel("1", 1), new VersionLabel("1", 1));
            Assert.AreEqual(new VersionLabel("1", 1), new VersionLabel("2", 1));
            Assert.AreEqual(new VersionLabel("1.2", 1, 2), new VersionLabel("1.2", 1, 2));
            Assert.AreEqual(new VersionLabel("1.2.3", 1, 2, 3), new VersionLabel("1.2.3", 1, 2, 3));
            Assert.AreEqual(new VersionLabel("1.2.3.4", 1, 2, 3, 4), new VersionLabel("1.2.3.4", 1, 2, 3, 4));

            Assert.AreNotEqual(new VersionLabel("1", 1), new VersionLabel("1", 2));
            Assert.AreNotEqual(new VersionLabel("1", 1), new VersionLabel("1", 1, 2));
            Assert.AreNotEqual(new VersionLabel("1", 1), new VersionLabel("1", 1, 2, 3));
            Assert.AreNotEqual(new VersionLabel("1", 1), new VersionLabel("1", 1, 2, 3, 4));
            Assert.AreNotEqual(new VersionLabel("1", 1, 2), new VersionLabel("1", 1, 2, 3, 4));
            Assert.AreNotEqual(new VersionLabel("1", 1, 2, 3), new VersionLabel("1", 1, 2, 3, 4));
        }

        [TestMethod]
        public void TestVersionTag_Parse()
        {
            // Basic valid
            Assert.AreEqual(new VersionLabel("1", 1), VersionLabel.Parse("1"));
            Assert.AreEqual(new VersionLabel("1.0", 1, 0), VersionLabel.Parse("1.0"));
            Assert.AreEqual(new VersionLabel("1.0.0", 1, 0, 0), VersionLabel.Parse("1.0.0"));
            Assert.AreEqual(new VersionLabel("1.0.0.0", 1, 0, 0, 0), VersionLabel.Parse("1.0.0.0"));

            // Basic invalid
            Assert.IsNull(VersionLabel.Parse("-1"));
            Assert.IsNull(VersionLabel.Parse("1.."));
            Assert.IsNull(VersionLabel.Parse("1.-1"));
            Assert.IsNull(VersionLabel.Parse("1.a"));
            Assert.IsNull(VersionLabel.Parse("1. "));
            Assert.IsNull(VersionLabel.Parse("1.1.a"));
            Assert.IsNull(VersionLabel.Parse("1.1.1.a"));
            Assert.IsNull(VersionLabel.Parse("1.1.1.-1"));

            // With suffix
            Assert.AreEqual(new VersionLabel("1 ", 1), VersionLabel.Parse("1 "));
            Assert.AreEqual(new VersionLabel("1_ci", 1), VersionLabel.Parse("1_ci"));
            Assert.AreEqual(new VersionLabel("1.0_ci", 1, 0), VersionLabel.Parse("1.0_ci"));
        }

        [TestMethod]
        public void TestVersionTag_ParseWithPrefix()
        {
            // Basic valid
            Assert.AreEqual(new VersionLabel("v1", 1), VersionLabel.Parse("v1"));
            Assert.AreEqual(new VersionLabel("vv1.0", 1, 0), VersionLabel.Parse("vv1.0"));
            Assert.AreEqual(new VersionLabel("1.0.0", 1, 0, 0), VersionLabel.Parse("1.0.0"));
            Assert.AreEqual(new VersionLabel("v1.0.0.1", 1, 0, 0, 1), VersionLabel.Parse("v1.0.0.1"));

            Assert.AreEqual(new VersionLabel("v1", 1), VersionLabel.Parse("v1", "v"));
            Assert.AreEqual(new VersionLabel("vv1.0", 1, 0), VersionLabel.Parse("vv1.0", "vv"));
            Assert.AreEqual(new VersionLabel("1.0.0", 1, 0, 0), VersionLabel.Parse("1.0.0", ""));
            Assert.AreEqual(new VersionLabel("v1.0.0.1", 1, 0, 0, 1), VersionLabel.Parse("v1.0.0.1", "v"));

            Assert.IsNull(VersionLabel.Parse("_1.0.0.0"));
            Assert.IsNull(VersionLabel.Parse(".1.0.0.0"));
            Assert.IsNull(VersionLabel.Parse("v1", "aa"));
            Assert.IsNull(VersionLabel.Parse("vv1.0", "v"));
            Assert.IsNull(VersionLabel.Parse("1.0.0", "v"));
            Assert.IsNull(VersionLabel.Parse("v1.0.0.1", "vv"));
        }

        public void TestVersionTag_ParseWithSuffix()
        {
            // Basic valid
            Assert.AreEqual(new VersionLabel("v1", 1), VersionLabel.Parse("v1"));
            Assert.AreEqual(new VersionLabel("vv1.0", 1, 0), VersionLabel.Parse("vv1.0"));
            Assert.AreEqual(new VersionLabel("1.0.0", 1, 0, 0), VersionLabel.Parse("1.0.0"));
            Assert.AreEqual(new VersionLabel("v1.0.0.1", 1, 0, 0, 1), VersionLabel.Parse("v1.0.0.1"));
            Assert.IsNull(VersionLabel.Parse("_1.0.0.0"));
            Assert.IsNull(VersionLabel.Parse(".1.0.0.0"));
        }

        [TestMethod]
        public void TestVersionTag_Compare()
        {
            Assert.IsTrue(new VersionLabel("1", 1).CompareTo(new VersionLabel("2", 2)) < 0);
            Assert.IsTrue(new VersionLabel("2", 2).CompareTo(new VersionLabel("1", 1)) > 0);
            Assert.IsTrue(new VersionLabel("1", 1).CompareTo(new VersionLabel("1", 1)) == 0);
            Assert.IsTrue(new VersionLabel("1.0", 1, 0).CompareTo(new VersionLabel("1", 1)) > 0);
            Assert.IsTrue(new VersionLabel("1.1", 1, 1).CompareTo(new VersionLabel("1", 1)) > 0);
            Assert.IsTrue(new VersionLabel("1.1", 1, 1).CompareTo(new VersionLabel("1.0", 1, 0)) > 0);
            Assert.IsTrue(new VersionLabel("1.1", 1, 1).CompareTo(new VersionLabel("1.2", 1, 2)) < 0);
            Assert.IsTrue(new VersionLabel("1.1", 1, 1).CompareTo(new VersionLabel("1.1", 1, 1)) == 0);
            Assert.IsTrue(new VersionLabel("1.1.0", 1, 1, 0).CompareTo(new VersionLabel("1.1", 1, 1)) > 0);
            Assert.IsTrue(new VersionLabel("1.1.0", 1, 1, 0).CompareTo(new VersionLabel("1", 1)) > 0);
            Assert.IsTrue(new VersionLabel("1.1.1.1", 1, 1, 1, 1).CompareTo(new VersionLabel("1.1.1", 1, 1, 1)) > 0);
        }

        [TestMethod]
        public void TestVersionTag_Sort()
        {
            var versionTags = new List<VersionLabel>() {
                new VersionLabel("2.2.1.1", 2, 2, 1, 1),
                new VersionLabel("1.1.1.1", 1, 1, 1, 1),
                new VersionLabel("1.2.1.1", 1, 2, 1, 1),
                new VersionLabel("1.1.5.1", 1, 1, 5, 1),
                new VersionLabel("2.1.5", 2, 1, 5),
                new VersionLabel("1.2", 1, 2),
                new VersionLabel("2.1.5.2", 2, 1, 5, 2)};
            versionTags.Sort();

            var sortedVersionTags = new List<VersionLabel>() {
                new VersionLabel("1.1.1.1", 1, 1, 1, 1),
                new VersionLabel("1.1.5.1", 1, 1, 5, 1),
                new VersionLabel("1.2", 1, 2),
                new VersionLabel("1.2.1.1", 1, 2, 1, 1),
                new VersionLabel("2.1.5", 2, 1, 5),
                new VersionLabel("2.1.5.2", 2, 1, 5, 2),
                new VersionLabel("2.2.1.1", 2, 2, 1, 1),};
            Assert.IsTrue(versionTags.SequenceEqual(sortedVersionTags));
        }
    }
}
