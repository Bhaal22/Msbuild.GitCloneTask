using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsBuild.GitCloneTask
{
    internal static class Authenticator
    {
        public static Credentials GetAuthenticator(string auth)
        {
            return new DefaultCredentials();
        }
    }
}
