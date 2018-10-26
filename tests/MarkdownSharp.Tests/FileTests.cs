using MarkdownSharp;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace MarkdownSharpTests
{
    public class FileTests : BaseTest
    {
        private readonly Markdown _markdown = new Markdown();
        public FileTests(ITestOutputHelper output) : base(output) { }

        public static IEnumerable<object[]> GetMDTestFiles() => GetTests("testfiles.mdtest_1._1");
        public static IEnumerable<object[]> GetMSTestFiles() => GetTests("testfiles.mstest_0._1");
        //public static IEnumerable<object[]> GetPandocFiles() => GetTests("testfiles.pandoc");
        //public static IEnumerable<object[]> GetPhpMarkdownFiles() => GetTests("testfiles.php_markdown");

        /// <summary>
        /// This is the closest thing to a set of Markdown reference tests I could find
        /// See http://six.pairlist.net/pipermail/markdown-discuss/2009-February/001526.html
        /// and http://michelf.com/docs/projets/mdtest-1.1.zip
        /// and http://git.michelf.com/mdtest/
        /// </summary>
        [Theory, MemberData(nameof(GetMDTestFiles))]
        public void MDTest(string inputFile, string expectedFile) => FileTest(inputFile, expectedFile);

        /// <summary>
        /// Our fledgling internal test suite, primarily to exercise MarkdownSharp specific options.
        /// </summary>
        [Theory, MemberData(nameof(GetMSTestFiles))]
        public void MSTest(string inputFile, string expectedFile) => FileTest(inputFile, expectedFile);

        ///// <summary>
        ///// pandoc edge condition tests from 
        ///// https://code.google.com/p/pandoc/wiki/PandocVsMarkdownPl
        ///// </summary>
        //[Theory, MemberData(nameof(GetPandocFiles))]
        //public void Pandoc(string inputFile, string expectedFile) => FileTest(inputFile, expectedFile);

        ///// <summary>
        ///// See http://six.pairlist.net/pipermail/markdown-discuss/2009-February/001526.html
        ///// "another testsuite I made for testing PHP Markdown which should probably apply to any Markdown parser (the PHP Markdown testsuite)"
        ///// </summary>
        ///// <remarks>
        ///// These tests are quite tough, many complex edge conditions.
        ///// </remarks>
        //[Theory, MemberData(nameof(GetPhpMarkdownFiles))]
        //public void PhpMarkdown(string inputFile, string expectedFile) => FileTest(inputFile, expectedFile);

        private void FileTest(string inputFile, string expectedFile)
        {
            Output.WriteLine("Input file: {0}", inputFile);
            Output.WriteLine("Expected file: {0}", expectedFile);
            var input = GetResourceFileContent(inputFile);
            var transformed = _markdown.Transform(input);
            var expected = GetResourceFileContent(expectedFile);

            Output.WriteLine("Transformed:");
            Output.WriteLine(transformed);
            Output.WriteLine("Expected:");
            Output.WriteLine(expected);

            Assert.Equal(RemoveWhitespace(transformed), RemoveWhitespace(expected));
        }

        private static string RemoveWhitespace(string s)
        {
            // Standardize line endings             
            s = s.Replace("\r\n", "\n");    // DOS to Unix
            s = s.Replace("\r", "\n");      // Mac to Unix

            // remove any tabs entirely
            s = s.Replace("\t", "");

            // remove empty newlines
            s = Regex.Replace(s, @"^\n", "", RegexOptions.Multiline);

            // remove leading space at the start of lines
            s = Regex.Replace(s, @"^\s+", "", RegexOptions.Multiline);

            // remove all newlines
            return s.Replace("\n", "");
        }
    }
}
