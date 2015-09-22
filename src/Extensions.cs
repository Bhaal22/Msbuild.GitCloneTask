using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace MsBuild.GitCloneTask
{
    static class Extensions
    {
        public static SecureString Secure(this string clearPassword)
        {
            SecureString secure = new SecureString();

            foreach (var c in clearPassword)
            {
                secure.AppendChar(c);
            }
            return secure;
        }
    }
}
