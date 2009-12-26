using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace MarkdownSharpTests
{
    class Program
    {
        static void Main(string[] args)
        {

            //RealityCheck();

            //RunBenchmarks();

            log4net.Config.XmlConfigurator.Configure();
            RunTests();

            //AdHocTest();
            //RealityCheck();

            Console.ReadKey();
        }

        /// <summary>
        /// quick and dirty test for one-liner Markdown bug repros
        /// </summary>
        private static void AdHocTest()
        {
            var m = new MarkdownSharp.Markdown();
            string s = m.Transform(@"Backtick: `` \` ``");
            var x = s;
        }

        private static void RealityCheck()
        {
            //var m = new MarkdownSharp.MarkdownOld();
            var m = new MarkdownSharp.Markdown();

            string s = m.Transform(LoadFile("TestFiles/reality-check.txt"));
            string expected = LoadFile("TestFiles/reality-check.html");

            if (s != expected)
                throw new Exception("reality check failed!");
            else
                Console.WriteLine("reality check passed. phew.");

        }

        static void RunBenchmarks()
        {
            Benchmark(LoadFile("BenchmarkFiles/markdown-example-short-1.txt"), 1000);
            Benchmark(LoadFile("BenchmarkFiles/markdown-example-medium-1.txt"), 500);
            Benchmark(LoadFile("BenchmarkFiles/markdown-example-long-2.txt"), 100);
        }

        static string LoadFile(string filename)
        {
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            path = path.Replace(@"\bin\Release", "");
            path = path.Replace(@"\bin\Debug", "");
            path = path.Replace("MarkdownSharpTests.exe", "");
            string file = Path.Combine(path, filename);
            return File.ReadAllText(file);
        }

        static void Benchmark(string text, int iterations)
        {
            var m = new MarkdownSharp.Markdown();

            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < iterations; i++)
                m.Transform(text);
            sw.Stop();

            Console.WriteLine("input string length: " + text.Length);
            Console.Write("performed " + iterations + " iterations in " + sw.ElapsedMilliseconds);
            Console.WriteLine(" (" + Convert.ToDouble(sw.ElapsedMilliseconds) / Convert.ToDouble(iterations) + " ms per iteration)");
        }

        static void RunTests()
        {
            string testAssemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;

            Console.WriteLine("Running tests in {0}\n", testAssemblyLocation);

            Process p = new Process();

            string path = Path.Combine(Path.GetDirectoryName(testAssemblyLocation), @"nunit-console\nunit-console.exe");
            path = path.Replace(@"\bin\Debug", "");
            path = path.Replace(@"\bin\Release", "");
            p.StartInfo.FileName = path;
            p.StartInfo.Arguments = "\"" + testAssemblyLocation + "\" /labels /nologo";

            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;

            p.StartInfo.RedirectStandardOutput = true;
            p.OutputDataReceived += new DataReceivedEventHandler(p_DataReceived);

            p.StartInfo.RedirectStandardError = true;
            p.ErrorDataReceived += new DataReceivedEventHandler(p_DataReceived);

            p.Start();

            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            while (!p.HasExited)
            {
                System.Threading.Thread.Sleep(500);
            }

            Console.WriteLine();
        }

        private static void p_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            Console.WriteLine(e.Data);
        }



    }
}
