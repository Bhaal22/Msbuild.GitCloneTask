using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using gsx.msbuild;

namespace UT_GitTask
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            string json = @"{
                        'Username': 'gmonitor',
                        'Password': 'Mon1tor',
                        'Dependencies': {
                            'gsx.skemodel' : {
                                'Remote': 'http://{0}:{1}@gsxtfs2013:8080/tfs/common-libs/_git/Gsx.SkeModel',
                                'Branch': 'master',
                                'Commit':''
                            },
                            'powershellScanComponent': {
                                'Remote': 'http://{0}:{1}@gsxtfs2013:8080/tfs/gsx.skecomponents/_git/gsx.ske.powershellscancomponent',
                                'TopFolder': 'ScanComponents',
                                'Branch': 'master',
                                'Commit':''   
                            },
                            'diskScanComponent': {
                                'Remote': 'http://{0}:{1}@gsxtfs2013:8080/tfs/gsx.skecomponents/_git/gsx.ske.diskscancomponent',
                                'Branch': 'master',
                                'TopFolder': 'ScanComponents',
                                'Commit':''
                            }
                        }
                    }";

            var deserializedDependencies = JsonConvert.DeserializeObject<CompileDependencies>(json);
        }
    }
}
