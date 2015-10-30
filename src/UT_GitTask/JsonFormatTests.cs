using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using MsBuild.GitCloneTask;

namespace UT_GitTask
{
    [TestClass]
    public class JsonFormatTests
    {
        [TestMethod]
        public void TestDeserialization_TopFolder()
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
                        'Commit':''
                    },
                    { 
                        'DependencyName': 'dependency2',
                        'Remote': 'http://serverName/repository2',
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

            var deserializedDependencies = JsonConvert.DeserializeObject<CompileDependencies>(json);

            Assert.AreEqual(deserializedDependencies.Dependencies[2].TopFolder, "Toto");
        }

        [TestMethod]
        public void TestDeserialization_LocalFolder()
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
                        'Commit':''
                    },
                    { 
                        'DependencyName': 'dependency2',
                        'LocalFolder': 'http://serverName/repository2',
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

            var deserializedDependencies = JsonConvert.DeserializeObject<CompileDependencies>(json);

            Assert.AreEqual(deserializedDependencies.Dependencies[1].OutputFolder, "http://serverName/repository2");
        }
    }
}
