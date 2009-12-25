using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using NUnit.Framework;


namespace MarkdownSharpTests 
{
    [TestFixture]
    public class MDTestTests : BaseTest
    {

        const string folder = @"MDTest1.1\";

        private string LoadInput(string filename)
        {
            return LoadFile(folder + @"\" + filename + ".text");
        }

        private string LoadExpected(string filename)
        {
            return LoadFile(folder + @"\" + filename + ".html");
        }

        [Test]
        public void Auto_Links()
        {
            string name = MethodBase.GetCurrentMethod().Name;
            var m = new MarkdownSharp.Markdown();
            Assert.AreEqual(LoadExpected(name), m.Transform(LoadInput(name)));
        }

        [Test]
        public void Amps_and_angle_encoding()
        {
            string name = MethodBase.GetCurrentMethod().Name;
            var m = new MarkdownSharp.Markdown();
            Assert.AreEqual(LoadExpected(name), m.Transform(LoadInput(name)));
        }

        [Test]
        public void Backslash_escapes()
        {
            string name = MethodBase.GetCurrentMethod().Name;
            var m = new MarkdownSharp.Markdown();
            Assert.AreEqual(LoadExpected(name), m.Transform(LoadInput(name)));
        }

        [Test]
        public void Blockquotes_with_code_blocks()
        {
            string name = MethodBase.GetCurrentMethod().Name;
            var m = new MarkdownSharp.Markdown();
            Assert.AreEqual(LoadExpected(name), m.Transform(LoadInput(name)));
        }

        [Test]
        public void Code_Blocks()
        {
            string name = MethodBase.GetCurrentMethod().Name;
            var m = new MarkdownSharp.Markdown();
            Assert.AreEqual(LoadExpected(name), m.Transform(LoadInput(name)));
        }

        [Test]
        public void Code_Spans()
        {
            string name = MethodBase.GetCurrentMethod().Name;
            var m = new MarkdownSharp.Markdown();
            Assert.AreEqual(LoadExpected(name), m.Transform(LoadInput(name)));
        }

        [Test]
        public void Hard_wrapped_paragraphs_with_list_like_lines()
        {
            string name = MethodBase.GetCurrentMethod().Name;
            var m = new MarkdownSharp.Markdown();
            Assert.AreEqual(LoadExpected(name), m.Transform(LoadInput(name)));
        }

        [Test]
        public void Horizontal_rules()
        {
            string name = MethodBase.GetCurrentMethod().Name;
            var m = new MarkdownSharp.Markdown();
            Assert.AreEqual(LoadExpected(name), m.Transform(LoadInput(name)));
        }

        [Test]
        public void Images()
        {
            string name = MethodBase.GetCurrentMethod().Name;
            var m = new MarkdownSharp.Markdown();
            Assert.AreEqual(LoadExpected(name), m.Transform(LoadInput(name)));
        }

        [Test]
        public void Inline_HTML_Advanced()
        {
            string name = MethodBase.GetCurrentMethod().Name;
            var m = new MarkdownSharp.Markdown();
            Assert.AreEqual(LoadExpected(name), m.Transform(LoadInput(name)));
        }

        [Test]
        public void Inline_HTML_comments()
        {
            string name = MethodBase.GetCurrentMethod().Name;
            var m = new MarkdownSharp.Markdown();
            Assert.AreEqual(LoadExpected(name), m.Transform(LoadInput(name)));
        }

        [Test]
        public void Inline_HTML_Simple()
        {
            string name = MethodBase.GetCurrentMethod().Name;
            var m = new MarkdownSharp.Markdown();
            Assert.AreEqual(LoadExpected(name), m.Transform(LoadInput(name)));
        }

        [Test]
        public void Links_inline_style()
        {
            string name = MethodBase.GetCurrentMethod().Name;
            var m = new MarkdownSharp.Markdown();
            Assert.AreEqual(LoadExpected(name), m.Transform(LoadInput(name)));
        }

        [Test]
        public void Links_reference_style()
        {
            string name = MethodBase.GetCurrentMethod().Name;
            var m = new MarkdownSharp.Markdown();
            Assert.AreEqual(LoadExpected(name), m.Transform(LoadInput(name)));
        }

        [Test]
        public void Links_shortcut_references()
        {
            string name = MethodBase.GetCurrentMethod().Name;
            var m = new MarkdownSharp.Markdown();
            Assert.AreEqual(LoadExpected(name), m.Transform(LoadInput(name)));
        }

        [Test]
        public void Literal_quotes_in_titles()
        {
            string name = MethodBase.GetCurrentMethod().Name;
            var m = new MarkdownSharp.Markdown();
            Assert.AreEqual(LoadExpected(name), m.Transform(LoadInput(name)));
        }

        [Test]
        public void Markdown_Documentation_Basics()
        {
            string name = MethodBase.GetCurrentMethod().Name;
            var m = new MarkdownSharp.Markdown();
            Assert.AreEqual(LoadExpected(name), m.Transform(LoadInput(name)));
        }

        [Test]
        public void Markdown_Documentation_Syntax()
        {
            string name = MethodBase.GetCurrentMethod().Name;
            var m = new MarkdownSharp.Markdown();
            Assert.AreEqual(LoadExpected(name), m.Transform(LoadInput(name)));
        }

        [Test]
        public void Nested_blockquotes()
        {
            string name = MethodBase.GetCurrentMethod().Name;
            var m = new MarkdownSharp.Markdown();
            Assert.AreEqual(LoadExpected(name), m.Transform(LoadInput(name)));
        }

        [Test]
        public void Ordered_and_unordered_lists()
        {
            string name = MethodBase.GetCurrentMethod().Name;
            var m = new MarkdownSharp.Markdown();
            Assert.AreEqual(LoadExpected(name), m.Transform(LoadInput(name)));
        }

        [Test]
        public void Strong_and_em_together()
        {
            string name = MethodBase.GetCurrentMethod().Name;
            var m = new MarkdownSharp.Markdown();
            Assert.AreEqual(LoadExpected(name), m.Transform(LoadInput(name)));
        }

        [Test]
        public void Tabs()
        {
            string name = MethodBase.GetCurrentMethod().Name;
            var m = new MarkdownSharp.Markdown();
            Assert.AreEqual(LoadExpected(name), m.Transform(LoadInput(name)));
        }

        [Test]
        public void Tidyness()
        {
            string name = MethodBase.GetCurrentMethod().Name;
            var m = new MarkdownSharp.Markdown();
            Assert.AreEqual(LoadExpected(name), m.Transform(LoadInput(name)));
        }

    }
}
