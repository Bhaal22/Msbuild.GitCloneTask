using System.Security;

namespace MsBuild.GitCloneTask
{
    static class StringExtensions
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
