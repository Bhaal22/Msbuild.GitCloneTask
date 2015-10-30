using Microsoft.VisualStudio.TestTools.UnitTesting;
using MsBuild.GitCloneTask;
using Msbuild;
using Newtonsoft.Json;

namespace UT_GitTask
{
    [TestClass]
    public class GitTests
    {
        [TestMethod]
        public void TestGit_MergeDependencies()
        {
            string json = @"{
                'Name': 'rootProject',
                'ShortName': 'rp',
                'Username': 'username',
                'Password': 'password',
                'Dependencies': [
                    {
                        'DependencyName': 'dependency1',
                        'Remote': 'http://serverName/repository1',
                        'Branch': 'master',
                        'TopFolder': 'plup',
                        'Commit':''
                    },
                    { 
                        'DependencyName': 'dependency2',
                        'LocalFolder': 'C:\\myLocalFolder\\',
                        'TopFolder': 'Top',
                        'Branch': 'master',
                        'Commit':''   
                    },
                    {
                        'DependencyName': 'dependency3',
                        'Remote': 'http://serverName/repository3',
                        'Branch': 'master',
                        'TopFolder': 'Toto',
                        'Commit':''
                    }
                ]
            }";

            string userJson = @"{
                'Name': 'rootProject',
                'ShortName': 'rp',
                'Username': 'username',
                'Password': 'password',
                'Dependencies': [
                    {
                        'DependencyName': 'dependency1',
                        'LocalFolder': 'c:\\toto\\plop',
                        'TopFolder': 'plop'
                    }
                ]
            }";

            var deps = JsonConvert.DeserializeObject<CompileDependencies>(json);
            Assert.AreEqual("rootProject", deps.Name);
            Assert.AreEqual("rp", deps.ShortName);
            Assert.AreEqual("username", deps.Username);
            Assert.AreEqual("password", deps.Password);
            Assert.AreEqual(3, deps.Dependencies.Count);
            
            Assert.AreEqual("dependency1", deps.Dependencies[0].DependencyName);
            Assert.AreEqual(@".\git\plup\dependency1", deps.Dependencies[0].OutputFolder);
            
            Assert.AreEqual("dependency2", deps.Dependencies[1].DependencyName);
            Assert.AreEqual(@"C:\myLocalFolder\", deps.Dependencies[1].OutputFolder);
            
            Assert.AreEqual("dependency3", deps.Dependencies[2].DependencyName);
            Assert.AreEqual(@".\git\Toto\dependency3", deps.Dependencies[2].OutputFolder);

            var userDeps = JsonConvert.DeserializeObject<CompileDependencies>(userJson);
            Assert.AreEqual("rootProject", userDeps.Name);
            Assert.AreEqual("rp", userDeps.ShortName);
            Assert.AreEqual("username", userDeps.Username);
            Assert.AreEqual("password", userDeps.Password);
            Assert.AreEqual(1, userDeps.Dependencies.Count);
            Assert.AreEqual("dependency1", userDeps.Dependencies[0].DependencyName);

            var git = new Git();
            var dependencies = git.MergeDependencies(deps, userDeps);

            Assert.AreEqual(3, deps.Dependencies.Count);

            Assert.AreEqual("dependency1", deps.Dependencies[0].DependencyName);
            Assert.AreEqual("plop", dependencies[0].TopFolder);
            Assert.AreEqual(string.Empty, dependencies[0].Remote);
            Assert.AreEqual(@".\git\plup\dependency1", deps.Dependencies[0].OutputFolder);
            
            Assert.AreEqual("dependency2", deps.Dependencies[1].DependencyName);
            Assert.AreEqual(@"C:\myLocalFolder\", deps.Dependencies[1].OutputFolder);
            
            Assert.AreEqual("dependency3", deps.Dependencies[2].DependencyName);
            Assert.AreEqual(@".\git\Toto\dependency3", dependencies[2].OutputFolder);
        }   
    }
}
