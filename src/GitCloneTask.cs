using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.Build.Framework;
using Newtonsoft.Json;
using MsBuild.GitCloneTask;

namespace Msbuild
{

    public enum BuildTool { MSBuild }

    public class BaseGit
    {
        public BaseGit()
            : this(BuildTool.MSBuild) {
        }

        protected BaseGit(BuildTool t)
        { currentBuildTool = t; }

        private string _dependencyFile = "git.json";

        public string DependencyFile 
        {
            get 
            { return _dependencyFile; }
            set 
            { _dependencyFile = value; }
        }

        private string _userDefinedDependencyFile = "git.user.json";
        public string UserDefinedDependencyFile 
        {
            get
            { return _userDefinedDependencyFile; }
            set
            { _userDefinedDependencyFile = value; }
        }

        [Output]
        public string[] Names { get; private set; }

        #region personality checks
        private readonly BuildTool currentBuildTool;
        protected bool RunningInMSBuild {
            get {
                return currentBuildTool == BuildTool.MSBuild;
            }
        }
        #endregion

        #region Logging
        protected void Debug(string p) {
#if MSBUILD
            if (RunningInMSBuild) {
                GenericMSBuildLog(p, MessageImportance.Low);
            }
#endif
        }

        protected void Log(string p) {
#if MSBUILD
            if (RunningInMSBuild) {
                GenericMSBuildLog(p, MessageImportance.Normal);
            }
#endif
        }

        protected void Warn(string p) {
#if MSBUILD
            if (RunningInMSBuild) {
                GenericMSBuildLog(p, MessageImportance.High);
            }
#endif
        }
        #endregion

        #region Build Tool specific stuff
#if MSBUILD
        #region MSBuild ITask Members

        private IBuildEngine buildEngine;
        public IBuildEngine BuildEngine
        {
            get
            {
                return buildEngine;
            }
            set
            {
                buildEngine = value;
            }
        }

        private ITaskHost hostObject;
        public ITaskHost HostObject
        {
            get
            {
                return hostObject;
            }
            set
            {
                hostObject = value;
            }
        }

        #endregion

        #region support stuff
        private static readonly string HELP_KEYWORD = string.Empty;

        private void GenericMSBuildLog(string message, MessageImportance i) {
            BuildEngine.LogMessageEvent(new BuildMessageEventArgs(message, HELP_KEYWORD, "git", i));
        }
        #endregion
#endif

        #endregion

        protected bool Run() {

            string gitCommandTemplate  = "{0} -b {1} {2} {3}";

            var _rawDependencies = JsonConvert.DeserializeObject<CompileDependencies>(File.ReadAllText(DependencyFile));
            var _userDefinedDependencies = readUserDefinedDependencies();


            var dependencies = _mergeDependencies(_rawDependencies, _userDefinedDependencies);

            Names = dependencies.Select(d => string.Format(@".\git\{0}\{1}\build.xml", d.TopFolder, d.Name)).ToArray();
            

            foreach (var dependency in dependencies)
            {
                var gitCommand = string.Empty;
                var folder = string.Format(@".\git\{0}\{1}", dependency.TopFolder, dependency.Name);
                if (!Directory.Exists(folder))
                {
                    gitCommand = string.Format(gitCommandTemplate, "clone", dependency.Branch, dependency.Remote, folder);

                    Log(gitCommand);
                    Run("git", gitCommand);
                }
                else
                {
                }

            }

            return true;
        }

        private List<Dependency> _mergeDependencies(CompileDependencies _rawDependencies, CompileDependencies _userDefinedDependencies)
        {
            Log(string.Format("Raw Dependencies: Count = {0}", _rawDependencies.Dependencies.Count));
            IDictionary<string, Dependency> transformedRawDependencies = _rawDependencies.Dependencies.Select(p =>
                new Dependency
                {
                    Branch = p.Branch,
                    Commit = p.Commit,
                    Name = p.Name,
                    Remote = string.Format(p.Remote, _rawDependencies.Username, _rawDependencies.Password)
                }).ToDictionary(p => p.Name);

            Log("UserDefined Dependencies");
            Log(string.Format("IsNull {0}", _userDefinedDependencies.Dependencies == null));
            var transformedUserDefinedDependencies = _userDefinedDependencies.Dependencies.Select(p =>
                new Dependency
                {
                    Branch = p.Branch,
                    Commit = p.Commit,
                    Name = p.Name,
                    Remote = string.Format(p.Remote, _userDefinedDependencies.Username, _userDefinedDependencies.Password)
                }).ToDictionary(p => p.Name);

            Log("Merge Dependencies");
            foreach (var p in transformedUserDefinedDependencies)
            {
                transformedRawDependencies[p.Key] = p.Value;
            }


            Log(string.Format("Dependencies Count = {0}", transformedUserDefinedDependencies.Count));
            return transformedRawDependencies.Select(p => p.Value).ToList();
        }

        private CompileDependencies readUserDefinedDependencies()
        {
            CompileDependencies _userDependencies = new CompileDependencies();
            if (File.Exists(DependencyFile))
            {
                try
                {
                    _userDependencies = JsonConvert.DeserializeObject<CompileDependencies>(File.ReadAllText(UserDefinedDependencyFile));
                }
                catch(Exception ex)
                {
                    Warn(ex.Message);
                    Warn(ex.StackTrace);
                }
            }

            return _userDependencies;
        }

        private int Run(string command, string parameters)
        {
            try
            {
                ProcessStartInfo pi = new ProcessStartInfo(command);
                pi.Arguments = parameters;
                pi.UseShellExecute = false;
                pi.RedirectStandardOutput = true;
                Debug(string.Format("running {0} with args {1}", command, pi.Arguments));

                using (Process p = Process.Start(pi))
                {
                    Log(p.StandardOutput.ReadToEnd());
                    p.WaitForExit();
                    return p.ExitCode;
                }
            }
            catch (Exception e)
            {
                Warn(string.Format("Git execution failed because of: '{0}'", e.ToString()));
                return -1;
            }
        }
    }

#if MSBUILD
    /// <summary>
    /// MSBuild task to run Ant.
    /// </summary>
    public class Git : BaseGit, ITask {
        public Git()
            : base(BuildTool.MSBuild) {
        }

        public bool Execute() {
            return Run();
        }
    }
#endif

}
