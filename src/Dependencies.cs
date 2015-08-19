using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsBuild.GitCloneTask
{
    public class Dependency
    {
        public string Name { get; set; }
        public string Remote { get; set; }

        public string TopFolder { get; set; }

        public string Branch { get; set; } = "master";

        public string Commit { get; set; }

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
