using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;

namespace MarkdownSharpTests
{
    class Program
    {
        static void Main(string[] args)
        {

            UnitTests();

            //GenerateTestOutput(@"mdtest-1.1\");

            // quick and dirty "Hello World" type test
            //GenerateTestOutput(@"test-input\");

            //Benchmark();

            //AdHocTest();
            
            Console.ReadKey();
        }

        /// <summary>
        /// quick and dirty test for one-liner Markdown bug repros
        /// </summary>
        private static void AdHocTest()
        {
            var m = new MarkdownSharp.Markdown();

            string input = "    line1\n    \tline2\n    \t\tline3\n    \t\t\tline4\n";
            string output = m.Transform(input);

            Console.WriteLine("input:");
            Console.WriteLine(input);
            Console.WriteLine("output:");
            Console.WriteLine(output);
        }

        /// <summary>
        /// iterates through all the /MDTest1.1 files and generates file-based output 
        /// this is essentially the same as running the unit tests, but with diff-able results
        /// </summary>
        /// <remarks>
        /// three files are present:
        /// test_name.text         -- input (raw markdown)
        /// test_name.html         -- output (expected cooked html output from reference markdown engine)
        /// test_name.actual.html  -- actual output (actual cooked html output from our markdown c# engine)
        /// </remarks>
        static void GenerateTestOutput(string testfolder)
        {

            Console.WriteLine();
            Console.WriteLine(@"Markdown test run on \" + testfolder);
            Console.WriteLine();

            string path = Path.Combine(ExecutingAssemblyPath, testfolder);
            string input;
            string output;
            string expected;
            string filename;

            int ok = 0;
            int err = 0;
            int total = 0;

            var m = new MarkdownSharp.Markdown();

            foreach (var file in Directory.GetFiles(path, "*.text"))
            {
                input = FileContents(file);
                expected = FileContents(file.Replace(".text", ".html"));
                output = m.Transform(input);

                // clear any existing actual results, first
                File.Delete(file.Replace(".text", ".actual.html"));

                total++;

                filename = GetFileName(file);
                Console.Write(String.Format("{0:000} {1,-55}", total, filename.Replace(".text", "")));

                if (output == expected)
                {
                    ok++;
                    Console.WriteLine("OK");
                }
                else
                {
                    err++;
                    Console.WriteLine("Mismatch");
                    File.WriteAllText(file.Replace(".text", ".actual.html"), output);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Tests    : " + (ok + err));
            Console.WriteLine("Passed   : " + ok);
            Console.WriteLine("Mismatch : " + err);

            if (err > 0)
            {
                Console.WriteLine();
                Console.WriteLine("for each mismatch, a *.output.html file was generated in");
                Console.WriteLine(path);
                Console.WriteLine("to troubleshoot mismatches, use a diff tool on *.html and *.output.html");
            }

        }

        /// <summary>
        /// given a full path string return just the filename
        /// </summary>
        private static string GetFileName(string path)
        {
            return Regex.Match(path, @"\\[^\\]+?$").Value.Replace(@"\", "");
        }

        /// <summary>
        /// returns the contents of the specified file as a string  
        /// assumes the files are relative to the root of the project
        /// </summary>
        static string FileContents(string filename)
        {
            string file = Path.Combine(ExecutingAssemblyPath, filename);
            return File.ReadAllText(file);
        }

        /// <summary>
        /// returns the path of the currently executing assembly
        /// </summary>
        static private string ExecutingAssemblyPath
        {
            get
            {
                // very hacky, feel free to improve this
                string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
                path = path.Replace(@"\bin\Release", "");
                path = path.Replace(@"\bin\Debug", "");
                path = path.Replace("MarkdownSharpTests.exe", "");
                return path;
            }
        }


        /// <summary>
        /// executes a standard benchmark on short, medium, and long markdown samples  
        /// use this to verify any impacts of code changes on performance  
        /// please DO NOT MODIFY the input samples or the benchmark itself as this will invalidate previous 
        /// benchmark runs!
        /// </summary>
        static void Benchmark()
        {
            Benchmark(FileContents("benchmark/markdown-example-short-1.txt"), 1000);
            Benchmark(FileContents("benchmark/markdown-example-medium-1.txt"), 500);
            Benchmark(FileContents("benchmark/markdown-example-long-2.txt"), 100);
        }

        /// <summary>
        /// performs a rough benchmark of the Markdown engine using small, medium, and large input samples 
        /// please DO NOT MODIFY the input samples or the benchmark itself as this will invalidate previous 
        /// benchmark runs!
        /// </summary>
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

        /// <summary>
        /// executes nunit-console.exe to run all the tests in this assembly
        /// </summary>
        static void UnitTests()
        {
            log4net.Config.XmlConfigurator.Configure();

            string testAssemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;

            Console.WriteLine("Running tests in {0}\n", testAssemblyLocation);

            var p = new Process();

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
                System.Threading.Thread.Sleep(500);

            Console.WriteLine();
        }

        private static void p_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            Console.WriteLine(e.Data);
        }

    }
}
