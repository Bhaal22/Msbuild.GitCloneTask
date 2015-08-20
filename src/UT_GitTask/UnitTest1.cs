using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using MsBuild.GitCloneTask;

namespace UT_GitTask
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            string json = @"{
                        'Username': 'username',
                        'Password': 'password',
                        'Dependencies': [
                            {
                                'Name': 'project1',
                                'Remote': 'http://serverName/repository1',
                                'Branch': 'master',
                                'Commit':''
                            },
                            { 
                                'Name': 'project2',
                                'Remote': 'http://serverName/repository2',
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

            var deserializedDependencies = JsonConvert.DeserializeObject<CompileDependencies>(json);

            Assert.AreEqual(deserializedDependencies.Dependencies[2].TopFolder, "Toto");
        }

        [TestMethod]
        public void LocalFolderTest()
        {
            string json = @"{
                        'Username': 'username',
                        'Password': 'password',
                        'Dependencies': [
                            {
                                'Name': 'project1',
                                'Remote': 'http://serverName/repository1',
                                'Branch': 'master',
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

            var deserializedDependencies = JsonConvert.DeserializeObject<CompileDependencies>(json);

            Assert.AreEqual(deserializedDependencies.Dependencies[1].OutputFolder, "http://serverName/repository2");
        }
    }
}
