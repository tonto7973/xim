using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace Xim.Tests.Setup.Utils
{
    internal static class AdminUtils
    {
        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static void RunAsAdministrator<TApp>(string arguments)
        {
            var consoleOutFileName = typeof(TApp).Assembly.Location + ".out";
            try
            {
                File.Delete(consoleOutFileName);
            }
            catch
            {
                // ignore
            }

            using var process = new Process();

            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.FileName = Regex.Replace(typeof(TApp).Assembly.Location, "\\.dll$", ".exe", RegexOptions.IgnoreCase);
            process.StartInfo.Verb = "runas";
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.Arguments = arguments;

            process.Start();
            process.WaitForExit();

            using var reader = new StreamReader(new FileStream(consoleOutFileName, FileMode.Open, FileAccess.Read, FileShare.Read));
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                Console.WriteLine(line);
            }
        }

        public static void RewireConsoleOut<TApp>()
        {
            var consoleOutFileName = typeof(TApp).Assembly.Location + ".out";

            var stream = new FileStream(consoleOutFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
            var writer = new StreamWriter(stream)
            {
                AutoFlush = true
            };

            Console.SetOut(writer);
            Console.SetError(writer);
        }
    }
}
