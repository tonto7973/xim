using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Threading;

namespace Xim.Tests.Setup
{
    internal static class Program
    {
        public static void Main()
        {
            bool mutexCreated;
            using (new Mutex(true, "Xim.Tests.Setup:3c9a7a53-0a36-464d-a6eb-69d4340e4b01", out mutexCreated))
            {
                if (!mutexCreated)
                    RewireConsoleOut();
            }

            try
            {
                Run();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(ex.ToString());
                Console.ResetColor();
            }

            if (mutexCreated)
            {
                Console.Write("Press any key to quit ... ");
                Console.ReadKey(true);
            }
            else
            {
                Console.Out.Flush();
            }
        }

        private static void Run()
        {
            if (!IsAdministrator())
            {
                Console.WriteLine("Requesting administrator privileges ... ");
                RunAsAdministrator();
                return;
            }

            Console.Out.WriteLine("Registering Xim Test Certificate ... ");

            TestCertificate.SafeRegister();
        }

        private static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static string ConsoleOutFileName
            => typeof(Program).Assembly.Location + ".out";

        private static void RewireConsoleOut()
        {
            var stream = new FileStream(ConsoleOutFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
            var writer = new StreamWriter(stream)
            {
                AutoFlush = true
            };

            Console.SetOut(writer);
            Console.SetError(writer);
        }

        private static void RunAsAdministrator()
        {
            try
            {
                File.Delete(ConsoleOutFileName);
            }
            catch
            {
                // ignore
            }

            using (var process = new Process())
            {
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.FileName = typeof(Program).Assembly.Location;
                process.StartInfo.Verb = "runas";
                process.StartInfo.UseShellExecute = true;

                process.Start();
                process.WaitForExit();
            }

            using (var reader = new StreamReader(new FileStream(ConsoleOutFileName, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    Console.WriteLine(line);
                }
            }
        }
    }
}
