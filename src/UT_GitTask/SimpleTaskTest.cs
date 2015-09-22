using Microsoft.VisualStudio.TestTools.UnitTesting;
using MsBuild.GitCloneTask;
using Msbuild;

namespace UT_GitTask
{
    [TestClass]
    public class SimpleTaskTest
    {
        [TestMethod]
        public void TestTaskMerge()
        {
            string json = @"{
                        'Username': 'username',
                        'Password': 'password',
                        'Dependencies': [
                            {
                                'Name': 'project1',
                                'Remote': 'http://serverName/repository1',
                                'Branch': 'master',
                                'TopFolder': 'plup',
                                'Commit':''
                            },
                            { 
                                'Name': 'project2',
                                'LocalFolder': 'http://serverName/repository2',
                                'TopFolder': 'Top',
                                'Branch': 'master',
                                'Commit':''   
                            },
                            {
                                'Name': 'project3',
                                'Remote': 'http://serverName/repository3',
                                'Branch': 'master',
                                'TopFolder': 'Toto',
                                'Commit':''
                            }
                        ]
                    }";

            string userJson = @"{
                        'Username': 'username',
                        'Password': 'password',
                        'Dependencies': [
                            {
                                'Name': 'project1',
                                'LocalFolder': 'c:\\toto\\plop',
                                'TopFolder': 'plop'
                            }
                        ]
                    }";

            var deps = JsonConvert.DeserializeObject<CompileDependencies>(json);
            var userDeps = JsonConvert.DeserializeObject<CompileDependencies>(userJson);

            var git = new Git();

            var dependencies = git._mergeDependencies(deps, userDeps);

            Assert.AreEqual(dependencies[0].TopFolder, "plop");
            Assert.AreEqual(dependencies[0].Remote, string.Empty);

            Assert.AreEqual(dependencies[2].OutputFolder, @"git\Top\project3");
        }   
    }
}
