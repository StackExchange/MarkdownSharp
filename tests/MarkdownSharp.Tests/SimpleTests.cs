using MarkdownSharp;
using Xunit;
using Xunit.Abstractions;

namespace MarkdownSharpTests
{
    public class SimpleTests : BaseTest
    {
        private readonly Markdown _markdown = new Markdown();
        public SimpleTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Bold()
        {
            const string input = "This is **bold**. This is also __bold__.";
            const string expected = "<p>This is <strong>bold</strong>. This is also <strong>bold</strong>.</p>\n";

            string actual = _markdown.Transform(input);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Italic()
        {
            const string input = "This is *italic*. This is also _italic_.";
            const string expected = "<p>This is <em>italic</em>. This is also <em>italic</em>.</p>\n";

            string actual = _markdown.Transform(input);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Link()
        {
            const string input = "This is [a link][1].\n\n  [1]: http://www.example.com";
            const string expected = "<p>This is <a href=\"http://www.example.com\">a link</a>.</p>\n";

            string actual = _markdown.Transform(input);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void LinkBracket()
        {
            const string input = "Have you visited <http://www.example.com> before?";
            const string expected = "<p>Have you visited <a href=\"http://www.example.com\">http://www.example.com</a> before?</p>\n";

            string actual = _markdown.Transform(input);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void LinkBare_withoutAutoHyperLink()
        {
            const string input = "Have you visited http://www.example.com before?";
            const string expected = "<p>Have you visited http://www.example.com before?</p>\n";

            string actual = _markdown.Transform(input);

            Assert.Equal(expected, actual);
        }

        /*
        [Fact]
        public void LinkBare_withAutoHyperLink()
        {
            //TODO: implement some way of setting AutoHyperLink programmatically
            //to run this test now, just change the _autoHyperlink constant in Markdown.cs
            string input = "Have you visited http://www.example.com before?";
            string expected = "<p>Have you visited <a href=\"http://www.example.com\">http://www.example.com</a> before?</p>\n";

            string actual = m.Transform(input);

            Assert.Equal(expected, actual);
        }*/

        [Fact]
        public void LinkAlt()
        {
            const string input = "Have you visited [example](http://www.example.com) before?";
            const string expected = "<p>Have you visited <a href=\"http://www.example.com\">example</a> before?</p>\n";

            string actual = _markdown.Transform(input);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Image()
        {
            const string input = "An image goes here: ![alt text][1]\n\n  [1]: http://www.google.com/intl/en_ALL/images/logo.gif";
            const string expected = "<p>An image goes here: <img src=\"http://www.google.com/intl/en_ALL/images/logo.gif\" alt=\"alt text\" /></p>\n";

            string actual = _markdown.Transform(input);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Blockquote()
        {
            const string input = "Here is a quote\n\n> Sample blockquote\n";
            const string expected = "<p>Here is a quote</p>\n\n<blockquote>\n  <p>Sample blockquote</p>\n</blockquote>\n";

            string actual = _markdown.Transform(input);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void NumberList()
        {
            const string input = "A numbered list:\n\n1. a\n2. b\n3. c\n";
            const string expected = "<p>A numbered list:</p>\n\n<ol>\n<li>a</li>\n<li>b</li>\n<li>c</li>\n</ol>\n";

            string actual = _markdown.Transform(input);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void BulletList()
        {
            const string input = "A bulleted list:\n\n- a\n- b\n- c\n";
            const string expected = "<p>A bulleted list:</p>\n\n<ul>\n<li>a</li>\n<li>b</li>\n<li>c</li>\n</ul>\n";

            string actual = _markdown.Transform(input);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Header1()
        {
            const string input = "#Header 1\nHeader 1\n========";
            const string expected = "<h1>Header 1</h1>\n\n<h1>Header 1</h1>\n";

            string actual = _markdown.Transform(input);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Header2()
        {
            const string input = "##Header 2\nHeader 2\n--------";
            const string expected = "<h2>Header 2</h2>\n\n<h2>Header 2</h2>\n";

            string actual = _markdown.Transform(input);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CodeBlock()
        {
            const string input = "code sample:\n\n    <head>\n    <title>page title</title>\n    </head>\n";
            const string expected = "<p>code sample:</p>\n\n<pre><code>&lt;head&gt;\n&lt;title&gt;page title&lt;/title&gt;\n&lt;/head&gt;\n</code></pre>\n";

            string actual = _markdown.Transform(input);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CodeSpan()
        {
            const string input = "HTML contains the `<blink>` tag";
            const string expected = "<p>HTML contains the <code>&lt;blink&gt;</code> tag</p>\n";

            string actual = _markdown.Transform(input);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void HtmlPassthrough()
        {
            const string input = "<div>\nHello World!\n</div>\n";
            const string expected = "<div>\nHello World!\n</div>\n";

            string actual = _markdown.Transform(input);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Escaping()
        {
            const string input = @"\`foo\`";
            const string expected = "<p>`foo`</p>\n";

            string actual = _markdown.Transform(input);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void HorizontalRule()
        {
            const string input = "* * *\n\n***\n\n*****\n\n- - -\n\n---------------------------------------\n\n";
            const string expected = "<hr />\n\n<hr />\n\n<hr />\n\n<hr />\n\n<hr />\n";

            string actual = _markdown.Transform(input);

            Assert.Equal(expected, actual);
        }
    }
}
