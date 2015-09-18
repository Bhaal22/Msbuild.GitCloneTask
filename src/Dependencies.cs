using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsBuild.GitCloneTask
{
    public class Dependency
    {
        public string Name { get; set; } = string.Empty;
        public string Remote { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public string LocalFolder { get; set; } = string.Empty;

        public string TopFolder { get; set; } = string.Empty;

        public string Branch { get; set; } = "master";

        public string Commit { get; set; } = string.Empty;


        public string InputSourceReference
        {
            get
            {
                if (UseGit)
                    return Remote;

                return LocalFolder;
            }
        }

        public string OutputFolder
        {
            get
            {
                if (UseGit)
                    return string.Format(@".\git\{0}\{1}", TopFolder, Name);

                return string.Format(@"{0}", LocalFolder);
            }
        }

        public bool UseGit
        {
            get
            {
                return !string.IsNullOrEmpty(Remote);
            }
        }

    }

    public class CompileDependencies
    {
        public string Username { get; set; }

        public string Password { get; set; }
        public IList<Dependency> Dependencies { get; set; }

        public CompileDependencies()
        {
            Username = string.Empty;
            Password = string.Empty;

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
