using LibGit2Sharp;
using System.Collections.Generic;

namespace MsBuild.GitCloneTask
{
    public class Dependency
    {
        public string DependencyName { get; set; } = string.Empty;

        public string Remote { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string LocalFolder { get; set; } = string.Empty;

        public string TopFolder { get; set; } = string.Empty;

        public string Branch { get; set; } = "master";

        public string Commit { get; set; } = string.Empty;

        public Credentials GetCredentials(string auth) => auth.Equals("Basic") ? new UsernamePasswordCredentials { Username = Username, Password = Password } as Credentials : new DefaultCredentials();

        public string InputSourceReference => UseGit ? Remote : LocalFolder;

        public string OutputFolder => UseGit ? $@".\git\{TopFolder}\{DependencyName}" : $@"{LocalFolder}";

        public bool UseGit => !string.IsNullOrEmpty(Remote);

        public override string ToString() => $@"{DependencyName}";
    }

    public class CompileDependencies
    {
        public string Name { get; set; } = string.Empty;

        public string ShortName { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string AuthenticationType { get; set; } = "Basic";

        public IList<Dependency> Dependencies { get; set; }

        public CompileDependencies()
        {
            Dependencies = new List<Dependency>();
        }

        public CompileDependencies(CompileDependencies from)
        {
            Username = from.Username;
            Password = from.Password;
            Dependencies = new List<Dependency>(from.Dependencies);
        }
    }
}
