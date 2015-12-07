using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.Build.Framework;
using Newtonsoft.Json;
using MsBuild.GitCloneTask;


using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace Msbuild
{
    public enum BuildTool { MSBuild }

    public class BaseGit : MsBuild.GitCloneTask.ILogger
    {
        public BaseGit()
            : this(BuildTool.MSBuild) 
        {
        }

        protected BaseGit(BuildTool t)
        { 
            currentBuildTool = t; 
        }

        public string DependencyFile { get; set; } = "git.json";

        public string UserDefinedDependencyFile { get; set; } = "git.user.json";

        public bool Pull { get; set; }

        public string Authentication { get; set; } = "Default";

        [Output]
        public string[] Names { get; private set; }

        #region personality checks
        private readonly BuildTool currentBuildTool;
        protected bool RunningInMSBuild => currentBuildTool == BuildTool.MSBuild;
        #endregion

        #region IMsBuildLogger members

        private int IndentLevel { get; set; } = 0;

        public virtual void Debug(string message) 
        {
            if (RunningInMSBuild) {
                GenericMSBuildLog(new string('\t', IndentLevel) + message, MessageImportance.Low);
            }
        }

        public virtual void Log(string message) 
        {
            if (RunningInMSBuild) {
                GenericMSBuildLog(new string('\t', IndentLevel) + message, MessageImportance.Normal);
            }
        }

        public virtual void Warn(string message) 
        {
            if (RunningInMSBuild) {
                GenericMSBuildLog(new string('\t', IndentLevel) + message, MessageImportance.High);
            }
        }
        #endregion

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


        #region support stuff
        private static readonly string HELP_KEYWORD = string.Empty;

        private void GenericMSBuildLog(string message, MessageImportance messageImportance) {
            if (BuildEngine != null)
                BuildEngine.LogMessageEvent(new BuildMessageEventArgs(message, HELP_KEYWORD, "git", messageImportance));
        }
        #endregion

        #endregion

        protected virtual bool Run() 
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var rootRepositoryDirectory = VersionResolver.GetRootRepositoryDirectoryOf(currentDirectory);
            Log($"{nameof(BaseGit)}.{nameof(Run)}: current directory = {currentDirectory}, root repository directory = {rootRepositoryDirectory}");
            
            var rawDependencies = JsonConvert.DeserializeObject<CompileDependencies>(File.ReadAllText(DependencyFile));
            var userDefinedDependencies = readUserDefinedDependencies();

            var dependencies = MergeDependencies(rawDependencies, userDefinedDependencies);

            Names = dependencies.Select(d => $@"{d.OutputFolder}\build.xml").ToArray();

            try
            {
                IndentLevel++;

                using (var myRepository = new Repository(rootRepositoryDirectory.FullName))
                {
                    foreach (var dependency in dependencies)
                    {
                        if (dependency.UseGit)
                        {
                            var cloneOptions = new CloneOptions()
                            {
                                CredentialsProvider = (_url, _user, _cred) => dependency.GetCredentials(Authentication)
                            };

                            if (dependency.Branch == "autoversioning")
                                HandleAutoVersioning(myRepository, rawDependencies.ShortName, dependency, cloneOptions);
                            else
                                HandleFixedVersioning(dependency, cloneOptions);
                        }
                        else
                            Log($"Dependency {dependency} does not use a remote repository");
                    }
                }
            }
            finally
            {
                IndentLevel--; 
            }

            return true;
        }

        protected virtual void HandleAutoVersioning(Repository myRepository, string myShortName, Dependency dependency, CloneOptions cloneOptions)
        {
            if (!string.IsNullOrEmpty(dependency.Commit))
                throw new InvalidOperationException($"Dependency {dependency}: autoversioning: definition is incoherent: Branch = '{dependency.Branch}' and a Commit = '{dependency.Commit}' => you can't have both set!");

            if (Directory.Exists(dependency.OutputFolder))
            {
                Log($"Dependency {dependency}: autoversioning: output folder already exists: removing...");
                new DirectoryInfo(dependency.OutputFolder).ForceDelete();
            }

            Log($"Dependency {dependency}: autoversioning: cloning default branch of '{dependency.Remote}' into '{dependency.OutputFolder}'");
            Repository.Clone(dependency.Remote, dependency.OutputFolder, cloneOptions);

            try
            {
                IndentLevel++;

                using (var otherRepository = new Repository(dependency.OutputFolder))
                {
                    otherRepository.CheckoutAllRemoteBranches();
                    VersionResolver.CheckoutBranchInDependencyRepository(otherRepository, myRepository, myShortName, this);
                }
            }
            finally
            {
                IndentLevel--; 
            }
        }

        protected virtual void HandleFixedVersioning(Dependency dependency, CloneOptions cloneOptions)
        {
            if (!string.IsNullOrEmpty(dependency.Branch) && !string.IsNullOrEmpty(dependency.Commit))
                throw new InvalidOperationException($"Dependency {dependency}: fixed versioning: definition is incoherent: Branch = '{dependency.Branch}' != '' and Commit = '{dependency.Commit}' != '' => you can't have both set!");

            if (!Directory.Exists(dependency.OutputFolder))
            {
                Log($"Dependency {dependency}: fixed versioning: cloning fixed branch '{dependency.Branch}' of '{dependency.Remote}' into '{dependency.OutputFolder}'...");
                cloneOptions.BranchName = dependency.Branch;
                Repository.Clone(dependency.Remote, dependency.OutputFolder, cloneOptions);
            }
            else if (Pull)
            {
                Log($"Dependency {dependency}: fixed versioning: repository '{dependency.Remote}' already cloned in '{dependency.OutputFolder}'");

                try
                {
                    IndentLevel++;

                    using (var otherRepository = new Repository(dependency.OutputFolder))
                    {
                        otherRepository.CheckoutAllRemoteBranches();

                        if (!string.IsNullOrEmpty(dependency.Commit))
                            HandleFixedVersioning_Commit(dependency, otherRepository);
                        else
                            HandleFixedVersioning_Branch(dependency, otherRepository);
                    }
                }
                finally
                {
                    IndentLevel--; 
                }
            }
        }

        protected virtual void HandleFixedVersioning_Commit(Dependency dependency, Repository otherRepository)
        {
            var dependencyCommit = otherRepository.Lookup(dependency.Commit) as Commit;
            if (dependencyCommit == null)
                throw new InvalidOperationException($"Dependency {dependency}: Commit '{dependency.Commit}' is invalid");

            var buildBranch =
                (from branch in otherRepository.Branches
                where branch.FriendlyName == VersionResolver.BuildBranchName
                select branch).SingleOrDefault();

            if (buildBranch == null)
            {
                Log($"Dependency {dependency}: fixed versioning: '{VersionResolver.BuildBranchName}' branch not found => creating it at Commit = '{dependency.Commit}'...");
                otherRepository.CreateBranch(VersionResolver.BuildBranchName, dependency.Commit);
            }
            else
            {
                Log($"Dependency {dependency}: fixed versioning: '{VersionResolver.BuildBranchName}' branch found => checking it out and hard resetting it to Commit = '{dependency.Commit}'...");
                otherRepository.Checkout(buildBranch, new CheckoutOptions() { CheckoutModifiers = CheckoutModifiers.Force });
                otherRepository.Reset(ResetMode.Hard, dependencyCommit);
            }
        }

        protected virtual void HandleFixedVersioning_Branch(Dependency dependency, Repository otherRepository)
        {
            Log($"Dependency {dependency}: fixed versioning: checking current HEAD branch...");
            var headBranch = otherRepository.Head.FriendlyName;
            if (headBranch != dependency.Branch)
            {
                Log($"Dependency {dependency}: fixed versioning: the current HEAD branch '{headBranch}' is different than the dependency branch '{dependency.Branch}' => checkout");
                otherRepository.Checkout(dependency.Branch, new CheckoutOptions() { CheckoutModifiers = CheckoutModifiers.Force });
            }

            // TODO: perform the pull only when necessary

            Log($"Dependency {dependency}: fixed versioning: pulling {dependency.Remote}");
            var options = new PullOptions();
            options.FetchOptions = new FetchOptions()
            {
                CredentialsProvider = new CredentialsHandler((url, usernameFromUrl, types) => dependency.GetCredentials(Authentication))
            };
            otherRepository.Network.Pull(new Signature(dependency.Username, dependency.Email, new DateTimeOffset(DateTime.Now)), options);
        }

        public List<Dependency> MergeDependencies(CompileDependencies rawDependencies, CompileDependencies userDefinedDependencies)
        {
            Log($"{nameof(MergeDependencies)}: raw dependencies count = {rawDependencies.Dependencies.Count}");
            IDictionary<string, Dependency> transformedRawDependencies = rawDependencies.Dependencies.Select(p =>
                new Dependency
                {
                    Branch = p.Branch,
                    Commit = p.Commit,
                    DependencyName = p.DependencyName,
                    TopFolder = p.TopFolder,
                    Remote = p.Remote,
                    Username = rawDependencies.Username,
                    Password = rawDependencies.Password,
                    Email = rawDependencies.Email,
                    LocalFolder = p.LocalFolder
                }).ToDictionary(p => p.DependencyName);

            Log($"{nameof(MergeDependencies)}: user-defined dependencies specified: {userDefinedDependencies.Dependencies != null}");
            var transformedUserDefinedDependencies = userDefinedDependencies.Dependencies.Select(p =>
                new Dependency
                {
                    Branch = p.Branch,
                    Commit = p.Commit,
                    DependencyName = p.DependencyName,
                    TopFolder = p.TopFolder,
                    Remote = string.Format(p.Remote, userDefinedDependencies.Username, userDefinedDependencies.Password),
                    Username = userDefinedDependencies.Username,
                    Password = userDefinedDependencies.Password,
                    Email = userDefinedDependencies.Email,
                    LocalFolder = p.LocalFolder
                }).ToDictionary(p => p.DependencyName);

            Log($"{nameof(MergeDependencies)}: performing...");
            foreach (var p in transformedUserDefinedDependencies)
            {
                transformedRawDependencies[p.Key] = p.Value;
            }

            Log($"{nameof(MergeDependencies)}: dependencies count = {transformedUserDefinedDependencies.Count}");
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
                    Warn($"Unable to read or deserialize '{UserDefinedDependencyFile}': {ex.Message}");
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

    /// <summary>
    /// MSBuild task to run GitClone.
    /// </summary>
    public class Git : BaseGit, ITask
    {
        public Git()
            : base(BuildTool.MSBuild)
        {
        }

        public bool Execute() => Run();
    }

}
