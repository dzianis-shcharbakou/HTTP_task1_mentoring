using HTTP_core;
using System;
using System.IO;

namespace HTTP_Fundamentals
{
    class Program
    {
        private static string SaveTo = Path.Combine(Environment.CurrentDirectory,@"Temp\");

        private static string Url = "https://www.google.com/";

        private static int DeepLevel = 2;


        static void Main(string[] args)
        {
            HttpCreator creator = new HttpCreator(SaveTo, Url)
                .SetDeepLevel(DeepLevel)
                .SetDomainRestriction(DomainRestrictionEnum.OnlyCurrentDomain)
                .SetVerboseMode(true)
                .SetDownloadResourceRestriction(new System.Collections.Generic.List<string>() { ".gif", ".png",  ".jpeg", ".jpg", ".pdf" });

            creator.Progress += ConsoleLog;

            creator.AnalysContent(Url);
        }

        public static void ConsoleLog(object sender, string message)
        {
            Console.WriteLine(message);
        }
    }
}
