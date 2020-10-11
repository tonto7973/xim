using System;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Xim.Tests.Setup.Utils
{
    internal static class CngUtils
    {
        public static string GetContainerPath(string name)
        {
            var commonAppDataDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var searchDirs = new string[] { "Microsoft\\Crypto\\Keys", "Microsoft\\Crypto\\SystemKeys" };
            return searchDirs
                .Select(searchDir => Path.Combine(commonAppDataDir, searchDir))
                .SelectMany(searchPath => Directory.EnumerateFiles(searchPath, name, SearchOption.TopDirectoryOnly))
                .FirstOrDefault();
        }

        public static void AddAcl(string fileName, IdentityReference sid)
        {
            var file = new FileInfo(fileName);
            FileSecurity fs = file.GetAccessControl();
            fs.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.Read, AccessControlType.Allow));

            file.SetAccessControl(fs);
        }
    }
}
