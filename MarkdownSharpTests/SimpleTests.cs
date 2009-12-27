using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace MarkdownSharpTests
{
    [TestFixture]
    public class SimpleTests : BaseTest
    {

        [Test]
        public void Bold()
        {
            var m = new MarkdownSharp.Markdown();
            string s = m.Transform("This is **bold**. This is also __bold__.");
            Assert.AreEqual("<p>This is <strong>bold</strong>. This is also <strong>bold</strong>.</p>\n", s);
        }

        [Test]
        public void Italic()
        {
            var m = new MarkdownSharp.Markdown();
            string s = m.Transform("This is *italic*. This is also _italic_.");
            Assert.AreEqual("<p>This is <em>italic</em>. This is also <em>italic</em>.</p>\n", s);
        }

        [Test]
        public void Link()
        {
            var m = new MarkdownSharp.Markdown();
            string s = m.Transform("This is [a link][1].\n\n  [1]: http://www.example.com");
            Assert.AreEqual("<p>This is <a href=\"http://www.example.com\">a link</a>.</p>\n", s);
        }

        [Test]
        public void LinkBracket()
        {
            var m = new MarkdownSharp.Markdown();
            string s = m.Transform("Have you visited <http://www.example.com> before?");
            Assert.AreEqual("<p>Have you visited <a href=\"http://www.example.com\">http://www.example.com</a> before?</p>\n", s);
        }

        [Test]
        public void LinkBare()
        {
            var m = new MarkdownSharp.Markdown();
            string s = m.Transform("Have you visited http://www.example.com before?");
            Assert.AreEqual("<p>Have you visited <a href=\"http://www.example.com\">http://www.example.com</a> before?</p>\n", s);
        }

        [Test]
        public void LinkAlt()
        {
            var m = new MarkdownSharp.Markdown();
            string s = m.Transform("Have you visited [example](http://www.example.com) before?");
            Assert.AreEqual("<p>Have you visited <a href=\"http://www.example.com\">example</a> before?</p>\n", s);
        }

        [Test]
        public void Image()
        {
            var m = new MarkdownSharp.Markdown();
            string s = m.Transform("An image goes here: ![alt text][1]\n\n  [1]: http://www.google.com/intl/en_ALL/images/logo.gif");
            Assert.AreEqual("<p>An image goes here: <img src=\"http://www.google.com/intl/en%5FALL/images/logo.gif\" alt=\"alt text\" /></p>\n", s);
        }

        [Test]
        public void Blockquote()
        {
            var m = new MarkdownSharp.Markdown();
            string s = m.Transform("Here is a quote\n\n> Sample blockquote\n");
            Assert.AreEqual("<p>Here is a quote</p>\n\n<blockquote>\n  <p>Sample blockquote</p>\n</blockquote>\n", s);
        }

        [Test]
        public void NumberList()
        {
            var m = new MarkdownSharp.Markdown();
            string s = m.Transform("A numbered list:\n\n1. a\n2. b\n3. c\n");
            Assert.AreEqual("<p>A numbered list:</p>\n\n<ol>\n<li>a</li>\n<li>b</li>\n<li>c</li>\n</ol>\n", s);
        }

        [Test]
        public void BulletList()
        {
            var m = new MarkdownSharp.Markdown();
            string s = m.Transform("A bulleted list:\n\n- a\n- b\n- c\n");
            Assert.AreEqual("<p>A bulleted list:</p>\n\n<ul>\n<li>a</li>\n<li>b</li>\n<li>c</li>\n</ul>\n", s);
        }

        [Test]
        public void Header1()
        {
            var m = new MarkdownSharp.Markdown();
            string s = m.Transform("#Header 1\nHeader 1\n========");
            Assert.AreEqual("<h1>Header 1</h1>\n\n<h1>Header 1</h1>\n", s);
        }

        [Test]
        public void Header2()
        {
            var m = new MarkdownSharp.Markdown();
            string s = m.Transform("##Header 2\nHeader 2\n--------");
            Assert.AreEqual("<h2>Header 2</h2>\n\n<h2>Header 2</h2>\n", s);
        }

        [Test]
        public void CodeBlock()
        {
            var m = new MarkdownSharp.Markdown();
            string s = m.Transform("code sample:\n\n    <head>\n    <title>page title</title>\n    </head>\n");
            Assert.AreEqual("<p>code sample:</p>\n\n<pre><code>&lt;head&gt;\n&lt;title&gt;page title&lt;/title&gt;\n&lt;/head&gt;\n</code></pre>\n", s);
        }

        [Test]
        public void CodeSpan()
        {
            var m = new MarkdownSharp.Markdown();
            string s = m.Transform("HTML contains the `<blink>` tag");
            Assert.AreEqual("<p>HTML contains the <code>&lt;blink&gt;</code> tag</p>\n", s);
        }

        [Test]
        public void HtmlPassthrough()
        {
            var m = new MarkdownSharp.Markdown();
            string s = m.Transform("<div>\nHello World!\n</div>\n");
            Assert.AreEqual("<div>\nHello World!\n</div>\n", s);
        }

        [Test]
        public void Escaping()
        {
            var m = new MarkdownSharp.Markdown();
            string s = m.Transform(@"\`foo\`");
            Assert.AreEqual("<p>`foo`</p>\n", s);
        }

        [Test]
        public void HorizontalRule()
        {
            var m = new MarkdownSharp.Markdown();
            string s = m.Transform("* * *\n\n***\n\n*****\n\n- - -\n\n---------------------------------------\n\n");
            Assert.AreEqual("<hr />\n\n<hr />\n\n<hr />\n\n<hr />\n\n<hr />\n", s);
        }

    }
}
