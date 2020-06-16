using System.Configuration;
using MarkdownSharp;
using Xunit;

namespace MarkdownSharpTests
{
    public class ConfigTest
    {
        [Fact]
        public void TestLoadFromConfiguration()
        {
            var settings = ConfigurationManager.AppSettings;
            settings.Set("Markdown.AutoHyperlink", "true");
            settings.Set("Markdown.AutoNewlines", "true");
            settings.Set("Markdown.EmptyElementSuffix", ">");
            settings.Set("Markdown.LinkEmails", "false");
            settings.Set("Markdown.StrictBoldItalic", "true");

            var markdown = new Markdown(true);
            Assert.True(markdown.AutoHyperlink);
            Assert.True(markdown.AutoNewLines);
            Assert.Equal(">", markdown.EmptyElementSuffix);
            Assert.False(markdown.LinkEmails);
            Assert.True(markdown.StrictBoldItalic);
        }

        [Fact]
        public void TestNoLoadFromConfigFile()
        {
            foreach (var markdown in new[] {new Markdown(), new Markdown(false)})
            {
                Assert.False(markdown.AutoHyperlink);
                Assert.False(markdown.AutoNewLines);
                Assert.Equal(" />", markdown.EmptyElementSuffix);
                Assert.True(markdown.LinkEmails);
                Assert.False(markdown.StrictBoldItalic);
            }
        }

        [Fact]
        public void TestAutoHyperlink()
        {
            var markdown = new Markdown();
            Assert.False(markdown.AutoHyperlink);
            Assert.Equal("<p>foo http://example.com bar</p>\n", markdown.Transform("foo http://example.com bar"));
            markdown.AutoHyperlink = true;
            Assert.Equal("<p>foo <a href=\"http://example.com\">http://example.com</a> bar</p>\n", markdown.Transform("foo http://example.com bar"));
        }

        [Fact]
        public void TestAutoNewLines()
        {
            var markdown = new Markdown();
            Assert.False(markdown.AutoNewLines);
            Assert.Equal("<p>Line1\nLine2</p>\n", markdown.Transform("Line1\nLine2"));
            markdown.AutoNewLines = true;
            Assert.Equal("<p>Line1<br />\nLine2</p>\n", markdown.Transform("Line1\nLine2"));
        }

        [Fact]
        public void TestDefaultOptions()
        {
            var markdownWithoutOptions = new Markdown();
            Assert.Equal(" />", markdownWithoutOptions.EmptyElementSuffix);

            var markdownWithOptions = new Markdown(new MarkdownOptions { });
            Assert.Equal(" />", markdownWithOptions.EmptyElementSuffix);

            var options = new MarkdownOptions { };
            Assert.Equal(" />", options.EmptyElementSuffix);
        }

        [Fact]
        public void TestEmptyElementSuffix()
        {
            var markdown = new Markdown();
            Assert.Equal(" />", markdown.EmptyElementSuffix);
            Assert.Equal("<hr />\n", markdown.Transform("* * *"));
            markdown.EmptyElementSuffix = ">";
            Assert.Equal("<hr>\n", markdown.Transform("* * *"));
        }

        [Fact]
        public void TestLinkEmails()
        {
            var markdown = new Markdown();
            Assert.True(markdown.LinkEmails);
            Assert.Equal("<p><a href=\"&#", markdown.Transform("<aa@bb.com>").Substring(0,14));
            markdown.LinkEmails = false;
            Assert.Equal("<p><aa@bb.com></p>\n", markdown.Transform("<aa@bb.com>"));
        }

        [Fact]
        public void TestStrictBoldItalic()
        {
            var markdown = new Markdown();
            Assert.False(markdown.StrictBoldItalic);
            Assert.Equal("<p>before<strong>bold</strong>after before<em>italic</em>after</p>\n", markdown.Transform("before**bold**after before_italic_after"));
            markdown.StrictBoldItalic = true;
            Assert.Equal("<p>before*bold*after before_italic_after</p>\n", markdown.Transform("before*bold*after before_italic_after"));
        }
    }
}
