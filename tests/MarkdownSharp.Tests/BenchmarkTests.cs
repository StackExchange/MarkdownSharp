using MarkdownSharp;
using System;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace MarkdownSharpTests
{
    public class BenchmarkTests : BaseTest
    {
        private readonly Markdown _markdown = new Markdown();
        public BenchmarkTests(ITestOutputHelper output) : base(output) { }

        /// <summary>
        /// performs a rough benchmark of the Markdown engine using small, medium, and large input samples 
        /// please DO NOT MODIFY the input samples or the benchmark itself as this will invalidate previous 
        /// benchmark runs!
        /// </summary>
        /// <remarks>TODO: BenchmarkDotNet instead</remarks>
        [Theory]
        [InlineData("markdown-example-short-1.md", 4000)]
        [InlineData("markdown-example-medium-1.md", 1000)]
        [InlineData("markdown-example-long-2.md", 100)]
        [InlineData("markdown-readme.md", 1)]
        [InlineData("markdown-readme.8.md", 1)]
        [InlineData("markdown-readme.32.md", 1)]
        public void BenchmarkFile(string inputFile, int iterations)
        {
            var text = GetResourceFileContent(_assembly.GetName().Name + ".benchmarks." + inputFile);

            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < iterations; i++)
            {
                _markdown.Transform(text);
            }
            sw.Stop();

            Console.WriteLine("input string length: " + text.Length);
            Console.Write(iterations + " iteration" + (iterations == 1 ? "" : "s") + " in " + sw.ElapsedMilliseconds + " ms");
            if (iterations == 1)
                Console.WriteLine();
            else
                Console.WriteLine(" (" + (Convert.ToDouble(sw.ElapsedMilliseconds) / Convert.ToDouble(iterations)) + " ms per iteration)");
        }
    }
}
