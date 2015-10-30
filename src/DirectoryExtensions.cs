using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsBuild.GitCloneTask
{
    public static class DirectoryExtensions
    {
        public static void ForceDelete(this DirectoryInfo directory)
        {
            File.SetAttributes(directory.FullName, FileAttributes.Normal);

            var files = directory.GetFiles();
            var dirs = directory.GetDirectories();

            foreach (var file in files)
           {
                File.SetAttributes(file.FullName, FileAttributes.Normal);
                file.Delete();
            }

            foreach (var dir in dirs)
            {
                dir.ForceDelete();
            }

            directory.Delete();
        }
    }
}
