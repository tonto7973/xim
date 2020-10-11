using System;

namespace Xim.Tests.Setup
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                TestCertificate.Prepare(args);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(ex.ToString());
                Console.ResetColor();
            }
        }
    }
}
