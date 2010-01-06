/*
 * MarkdownSharp
 * -------------
 * a C# Markdown processor
 * 
 * Markdown is a text-to-HTML conversion tool for web writers
 * Copyright (c) 2004 John Gruber
 * http://daringfireball.net/projects/markdown/
 * 
 * Markdown.NET
 * Copyright (c) 2004-2009 Milan Negovan
 * http://www.aspnetresources.com
 * http://aspnetresources.com/blog/markdown_announced.aspx
 * 
 * MarkdownSharp
 * Copyright (c) 2009 Jeff Atwood
 * http://stackoverflow.com
 * http://www.codinghorror.com/blog/
 * http://code.google.com/p/markdownsharp/
 * 
 * History: Milan ported the Markdown processor to C#. He granted license to me so I can open source it
 * and let the community contribute to and improve MarkdownSharp.
 * 
 */

#region Copyright and license

/*

Copyright (c) 2009 Jeff Atwood

http://www.opensource.org/licenses/mit-license.php
  
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

Copyright (c) 2003-2004 John Gruber
<http://daringfireball.net/>   
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are
met:

* Redistributions of source code must retain the above copyright notice,
  this list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright
  notice, this list of conditions and the following disclaimer in the
  documentation and/or other materials provided with the distribution.

* Neither the name "Markdown" nor the names of its contributors may
  be used to endorse or promote products derived from this software
  without specific prior written permission.

This software is provided by the copyright holders and contributors "as
is" and any express or implied warranties, including, but not limited
to, the implied warranties of merchantability and fitness for a
particular purpose are disclaimed. In no event shall the copyright owner
or contributors be liable for any direct, indirect, incidental, special,
exemplary, or consequential damages (including, but not limited to,
procurement of substitute goods or services; loss of use, data, or
profits; or business interruption) however caused and on any theory of
liability, whether in contract, strict liability, or tort (including
negligence or otherwise) arising in any way out of the use of this
software, even if advised of the possibility of such damage.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MarkdownSharp
{
    public class Markdown
    {

        /// <summary>
        /// use ">" for HTML output, or " />" for XHTML output
        /// </summary>
        private const string _emptyElementSuffix = " />";
        /// <summary>
        /// Tabs are automatically converted to spaces as part of the transform  
        /// this variable determines how "wide" those tabs become in spaces
        /// </summary>
        private const int _tabWidth = 4;
        /// <summary>
        /// maximum nested depth of [] and () supported by the transform
        /// </summary>
        private const int _nestDepth = 6;



        /// <summary>
        /// when false, email addresses will never be auto-linked  
        /// WARNING: this is a significant deviation from the markdown spec
        /// </summary>
        private const bool _linkEmails = true;
        /// <summary>
        /// when true, bold and italic require non-word characters on either side  
        /// WARNING: this is a significant deviation from the markdown spec
        /// </summary>
        private const bool _strictBoldItalic = false;
        /// <summary>
        /// when true, RETURN becomes a literal newline  
        /// WARNING: this is a significant deviation from the markdown spec
        /// </summary>
        private const bool _autoNewlines = false;
        /// <summary>
        /// when true, (most) bare plain URLs are auto-hyperlinked  
        /// WARNING: this is a significant deviation from the markdown spec
        /// </summary>
        private const bool _autoHyperlink = false;
        /// <summary>
        /// when true, problematic URL characters like [, ], (, and so forth will be encoded 
        /// WARNING: this is a significant deviation from the markdown spec
        /// </summary>
        private const bool _encodeProblemUrlCharacters = false;


        private enum TokenType { Text, Tag }

        private struct Token
        {
            public Token(TokenType type, string value)
            {
                this.Type = type;
                this.Value = value;
            }
            public TokenType Type;
            public string Value;
        }

        private const string _markerUL = @"[*+-]";
        private const string _markerOL = @"\d+[.]";

        private static string _nestedBracketsPattern;
        private static string _nestedParensPattern;
        private static string _markerAnyPattern;
                
        private static readonly Dictionary<string, string> _escapeTable;
        private static readonly Dictionary<string, string> _backslashEscapeTable;

        private readonly Dictionary<string, string> _urls = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _titles = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _htmlBlocks = new Dictionary<string, string>();

        private int _listLevel = 0;

        /// <summary>
        /// Static constructor
        /// </summary>
        /// <remarks>
        /// In the static constuctor we'll initialize what stays the same across all transforms.
        /// </remarks>
        static Markdown()
        {
            // Table of hash values for escaped characters:
            _escapeTable = new Dictionary<string, string>();
            // Table of hash value for backslash escaped characters:
            _backslashEscapeTable = new Dictionary<string, string>();
            
            foreach (char c in @"\`*_{}[]()>#+-.!")
            {
                string key = c.ToString();
                string hash = key.GetHashCode().ToString();
                _escapeTable.Add(key, hash);
                _backslashEscapeTable.Add(@"\" + key, hash);
            }
                            
        }

        /// <summary>
        /// current version of MarkdownSharp  
        /// see http://code.google.com/p/markdownsharp/ for latest or to contribute
        /// </summary>
        public string Version
        {
            get { return "1.007"; }
        }

        /// <summary>
        /// Reusable pattern to match balanced [brackets]. See Friedl's 
        /// "Mastering Regular Expressions", 2nd Ed., pp. 328-331.
        /// </summary>
        private static string GetNestedBracketsPattern()
        {
            // in other words [this] and [this[also]] and [this[also[too]]]
            // up to _nestDepth
            if (_nestedBracketsPattern == null)
                _nestedBracketsPattern = 
                    RepeatString(@"
                    (?>              # Atomic matching
                       [^\[\]]+      # Anything other than brackets
                     |
                       \[
                           ", _nestDepth) + RepeatString(
                    @" \]
                    )*"
                    , _nestDepth);
            return _nestedBracketsPattern;
        }

        /// <summary>
        /// Reusable pattern to match balanced (parens). See Friedl's 
        /// "Mastering Regular Expressions", 2nd Ed., pp. 328-331.
        /// </summary>
        private static string GetNestedParensPattern()
        {
            // in other words (this) and (this(also)) and (this(also(too)))
            // up to _nestDepth
            if (_nestedParensPattern == null)
                _nestedParensPattern =
                    RepeatString(@"
                    (?>              # Atomic matching
                       [^()\s]+      # Anything other than parens or whitespace
                     |
                       \(
                           ", _nestDepth) + RepeatString(
                    @" \)
                    )*"
                    , _nestDepth);
            return _nestedParensPattern;
        }

        private static string GetMarkerAnyPattern()
        {
            if (_markerAnyPattern == null)
                _markerAnyPattern = string.Format("(?:{0}|{1})", _markerUL, _markerOL);
            return _markerAnyPattern;
        }

        private static string GetBoldPattern()
        {
            if (_strictBoldItalic)
                return @"([\W_]|^) (\*\*|__) (?=\S) ([^\r]*?\S[\*_]*) \2 ([\W_]|$)";
            else
                return @"(\*\*|__) (?=\S) (.+?[*_]*) (?<=\S) \1";
        }
        private static string GetBoldReplace()
        {
            if (_strictBoldItalic)
                return "$1<strong>$3</strong>$4";
            else
                return "<strong>$2</strong>";
        }

        private static string GetItalicPattern()
        {
            if (_strictBoldItalic)
                return @"([\W_]|^) (\*|_) (?=\S) ([^\r\*_]*?\S) \2 ([\W_]|$)";
            else
                return @"(\*|_) (?=\S) (.+?) (?<=\S) \1";
        }
        private static string GetItalicReplace()
        {
            if (_strictBoldItalic)
                return "$1<em>$3</em>$4";
            else
                return "<em>$2</em>";
        }


        /// <summary>
        /// Main function. The order in which other subs are called here is
        /// essential. Link and image substitutions need to happen before
        /// EscapeSpecialChars(), so that any *'s or _'s in the &lt;a&gt;
        /// and &lt;img&gt; tags get encoded.
        /// </summary>
        public string Transform(string text)
        {
            if (text == null) return "";

            Setup();

            // Standardize line endings
            text = text.Replace("\r\n", "\n");    // DOS to Unix
            text = text.Replace("\r", "\n");      // Mac to Unix

            // Make sure $text ends with a couple of newlines:
            text += "\n\n";

            // Convert all tabs to spaces.
            text = Detab(text);

            // Strip any lines consisting only of spaces and tabs.
            // This makes subsequent regexen easier to write, because we can
            // match consecutive blank lines with /\n+/ instead of something
            // contorted like /[ \t]*\n+/ .
            text = Regex.Replace(text, @"^[ \t]+$", "", RegexOptions.Multiline);

            // Turn block-level HTML blocks into hash entries
            text = HashHTMLBlocks(text);

            // Strip link definitions, store in hashes.
            text = StripLinkDefinitions(text);

            text = RunBlockGamut(text);

            text = UnescapeSpecialChars(text);

            return text + "\n";
        }


        private void Setup()
        {
            // Clear the global hashes. If we don't clear these, you get conflicts
            // from other articles when generating a page which contains more than
            // one article (e.g. an index page that shows the N most recent
            // articles):
            _urls.Clear();
            _titles.Clear();
            _htmlBlocks.Clear();
        }

        private static Regex _linkDef = new Regex(string.Format(@"
                        ^[ ]{{0,{0}}}\[(.+)\]:	# id = $1
                          [ \t]*
                          \n?				# maybe *one* newline
                          [ \t]*
                        <?(\S+?)>?			# url = $2
                          [ \t]*
                          \n?				# maybe one newline
                          [ \t]*
                        (?:
                            (?<=\s)			# lookbehind for whitespace
                            [\x22(]
                            (.+?)			# title = $3
                            [\x22)]
                            [ \t]*
                        )?	# title is optional
                        (?:\n+|\Z)", _tabWidth - 1), RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        /// <summary>
        /// Strips link definitions from text, stores the URLs and titles in hash references.
        /// </summary>
        /// <remarks>
        /// [id]: url "optional title"
        /// </remarks>
        private string StripLinkDefinitions(string text)
        {
            return _linkDef.Replace(text, new MatchEvaluator(LinkEvaluator));
        }

        private string LinkEvaluator(Match match)
        {
            string linkID = match.Groups[1].Value.ToLowerInvariant();
            _urls[linkID] = EncodeAmpsAndAngles(match.Groups[2].Value);

            if (match.Groups[3] != null && match.Groups[3].Length > 0)
                _titles[linkID] = match.Groups[3].Value.Replace("\"", "&quot;");

            return "";
        }


        private static string _blockTags1 = "p|div|h[1-6]|blockquote|pre|table|dl|ol|ul|script|noscript|form|fieldset|iframe|math|ins|del";
        private static Regex _blocksNested = new Regex(string.Format(@"
                (						# save in $1
                    ^					# start of line  (with /m)
                    <({0})	            # start tag = $2
                    \b					# word break
                    (.*\n)*?			# any number of lines, minimally matching
                    </\2>				# the matching end tag
                    [ \t]*				# trailing spaces/tabs
                    (?=\n+|\Z)	        # followed by a newline or end of document
                )", _blockTags1), RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static string _blockTags2 = "p|div|h[1-6]|blockquote|pre|table|dl|ol|ul|script|noscript|form|fieldset|iframe|math";
        private static Regex _blocksNestedLiberal = new Regex(string.Format(@"
               (						# save in $1
                    ^					# start of line  (with /m)
                    <({0})	            # start tag = $2
                    \b					# word break
                    (.*\n)*?			# any number of lines, minimally matching
                    .*</\2>				# the matching end tag
                    [ \t]*				# trailing spaces/tabs
                    (?=\n+|\Z)	        # followed by a newline or end of document
                )", _blockTags2), RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static Regex _blocksHr = new Regex(string.Format(@"
                (?:
                    (?<=\n\n)		    # Starting after a blank line
                    |				    # or
                    \A\n?			    # the beginning of the doc
                )
                (						# save in $1
                    [ ]{{0,{0}}}
                    <(hr)				# start tag = $2
                    \b					# word break
                    ([^<>])*?			#
                    /?>					# the matching end tag
                    [ \t]*
                    (?=\n{{2,}}|\Z)		# followed by a blank line or end of document
                )", _tabWidth - 1), RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static Regex _blocksHtmlComments = new Regex(string.Format(@"
                (?:
                    (?<=\n\n)		# Starting after a blank line
                    |				# or
                    \A\n?			# the beginning of the doc
                )
                (						# save in $1
                    [ ]{{0,{0}}}
                    (?s:
                        <!
                        (--.*?--\s*)+
                        >
                    )
                    [ \t]*
                    (?=\n{{2,}}|\Z)		# followed by a blank line or end of document
                )", _tabWidth - 1), RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        /// <summary>
        /// Hashify HTML blocks:
        /// We only want to do this for block-level HTML tags, such as headers,
        /// lists, and tables. That's because we still want to wrap &lt;p&gt;s around
        /// "paragraphs" that are wrapped in non-block-level tags, such as anchors,
        /// phrase emphasis, and spans. The list of tags we're looking for is
        /// hard-coded.        
        /// </summary>
        private string HashHTMLBlocks(string text)
        {
            //
            // First, look for nested blocks, e.g.:
            // <div>
            //    <div>
            //    tags for inner block must be indented.
            //    </div>
            // </div>
            
            // The outermost tags must start at the left margin for this to match, and
            // the inner nested divs must be indented.
            // We need to do this before the next, more liberal match, because the next
            // match will start at the first `<div>` and stop at the first `</div>`.
            text = _blocksNested.Replace(text, new MatchEvaluator(HtmlEvaluator));

            // Now match more liberally, simply from `\n<tag>` to `</tag>\n`
            text = _blocksNestedLiberal.Replace(text, new MatchEvaluator(HtmlEvaluator));

            // Special case just for <hr />. It was easier to make a special case than
            // to make the other regex more complicated.
            text = _blocksHr.Replace(text, new MatchEvaluator(HtmlEvaluator));

            // Special case for standalone HTML comments:
            text = _blocksHtmlComments.Replace(text, new MatchEvaluator(HtmlEvaluator));

            return text;
        }

        private string HtmlEvaluator(Match match)
        {
            string text = match.Groups[1].Value;
            string key = text.GetHashCode().ToString();
            _htmlBlocks[key] = text;

            // # String that will replace the block
            return string.Concat("\n\n", key, "\n\n");
        }

        private static Regex _horizontalRules = new Regex(@"
            ^[ ]{0,3}	        # Leading space
                ([-*_])		    # $1: First marker
                (?>			    # Repeated marker group
                    [ ]{0,2}	# Zero, one, or two spaces.
                    \1			# Marker character
                ){2,}		    # Group repeated at least twice
                [ ]*		    # Trailing spaces
                $			    # End of line.
            ", RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        /// <summary>
        /// These are all the transformations that form block-level 
        /// tags like paragraphs, headers, and list items.
        /// </summary>
        private string RunBlockGamut(string text)
        {
            text = DoHeaders(text);

            text = _horizontalRules.Replace(text, "<hr" + _emptyElementSuffix + "\n");

            text = DoLists(text);
            text = DoCodeBlocks(text);
            text = DoBlockQuotes(text);

            // We already ran HashHTMLBlocks() before, in Markdown(), but that
            // was to escape raw HTML in the original Markdown source. This time,
            // we're escaping the markup we've just created, so that we don't wrap
            // <p> tags around block-level tags.
            text = HashHTMLBlocks(text);

            text = FormParagraphs(text);

            return text;
        }


        /// <summary>
        /// These are all the transformations that occur *within* block-level 
        /// tags like paragraphs, headers, and list items.
        /// </summary>
        private string RunSpanGamut(string text)
        {
            text = DoCodeSpans(text);

            text = EscapeSpecialCharsWithinTagAttributes(text);
            text = EncodeBackslashEscapes(text);

            // Process anchor and image tags. Images must come first,
            // because ![foo][f] looks like an anchor.
            text = DoImages(text);
            text = DoAnchors(text);

            // Make links out of things like `<http://example.com/>`
            // Must come after DoAnchors(), because you can use < and >
            // delimiters in inline links like [this](<url>).
            text = DoAutoLinks(text);

            // Fix unencoded ampersands and <'s:
            text = EncodeAmpsAndAngles(text);

            text = DoItalicsAndBold(text);

            // do hard breaks
            if (_autoNewlines)
                text = Regex.Replace(text, @"\n", string.Format("<br{0}\n", _emptyElementSuffix));
            else
                text = Regex.Replace(text, @" {2,}\n", string.Format("<br{0}\n", _emptyElementSuffix));

            return text;
        }

        private static Regex _htmlTokens = new Regex(
            @"(?s:<!(?:--.*?--\s*)+>)|(?s:<\?.*?\?>)|" + 
            RepeatString(@"(?:<[a-z\/!$](?:[^<>]|", _nestDepth) + 
            RepeatString(@")*>)", _nestDepth), 
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text">String containing HTML markup.</param>
        /// <returns>An array of the tokens comprising the input string. Each token is 
        /// either a tag (possibly with nested, tags contained therein, such 
        /// as &lt;a href="&lt;MTFoo&gt;"&gt;, or a run of text between tags. Each element of the 
        /// array is a two-element array; the first is either 'tag' or 'text'; the second is 
        /// the actual value.
        /// </returns>
        private List<Token> TokenizeHTML(string text)
        {
            // Regular expression derived from the _tokenize() subroutine in 
            // Brad Choate's MTRegex plugin.
            // http://www.bradchoate.com/past/mtregex.php
            int pos = 0;
            var tokens = new List<Token>();

            foreach (Match m in _htmlTokens.Matches(text))
            {
                string wholeTag = m.Value;
                int tagStart = m.Index;

                if (pos < tagStart)
                {
                    tokens.Add(new Token(TokenType.Text, text.Substring(pos, tagStart - pos)));
                }

                tokens.Add(new Token(TokenType.Tag, wholeTag));

                pos = m.Index + m.Length;
            }

            if (pos < text.Length)
            {
                tokens.Add(new Token(TokenType.Text, text.Substring(pos, text.Length - pos)));
            }

            return tokens;
        }


        /// <summary>
        /// Within tags -- meaning between &lt; and &gt; -- encode [\ ` * _] so they 
        /// don't conflict with their use in Markdown for code, italics and strong. 
        /// We're replacing each such character with its corresponding hash 
        /// value; this is likely overkill, but it should prevent us from colliding 
        /// with the escape values by accident.
        /// </summary>
        private string EscapeSpecialCharsWithinTagAttributes(string text)
        {
            var tokens = TokenizeHTML(text);

            // now, rebuild text from the tokens
            var sb = new StringBuilder(text.Length);

            foreach (var token in tokens)
            {
                string value = token.Value;

                if (token.Type == TokenType.Tag)
                {
                    value = value.Replace(@"\", _escapeTable[@"\"]);
                    value = Regex.Replace(value, "(?<=.)</?code>(?=.)", _escapeTable[@"`"]);
                    value = value.Replace("*", _escapeTable["*"]);
                    value = value.Replace("_", _escapeTable["_"]);
                }                    

                sb.Append(value);
            }

            return sb.ToString();
        }


        private static Regex _anchorRef = new Regex(string.Format(@"
            (                               # wrap whole match in $1
                \[
                    ({0})                   # link text = $2
                \]

                [ ]?                        # one optional space
                (?:\n[ ]*)?                 # one optional newline followed by spaces

                \[
                    (.*?)                   # id = $3
                \]
            )", GetNestedBracketsPattern()), RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static Regex _anchorInline = new Regex(string.Format(@"
                (                          # wrap whole match in $1
                    \[
                        ({0})              # link text = $2
                    \]
                    \(                     # literal paren
                        [ \t]*
                        ({1})              # href = $3
                        [ \t]*
                        (                  # $4
                        (['\x22])          # quote char = $5
                        (.*?)              # Title = $6
                        \5                 # matching quote
                        [ \t]*             # ignore any spaces/tabs between closing quote and )
                        )?                 # title is optional
                    \)
                )", GetNestedBracketsPattern(), GetNestedParensPattern()),
                  RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static Regex _anchorRefShortcut = new Regex(@"
            (					# wrap whole match in $1
              \[
                 ([^\[\]]+)		# link text = $2; can't contain '[' or ']'
              \]
            )", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        /// <summary>
        /// Turn Markdown link shortcuts [link text](url "title") or [link text][id] into HTML anchor tags. 
        /// </summary>
        private string DoAnchors(string text)
        {
            // First, handle reference-style links: [link text] [id]
            text = _anchorRef.Replace(text, new MatchEvaluator(AnchorRefEvaluator));

            // Next, inline-style links: [link text](url "optional title") or [link text](url "optional title")
            text = _anchorInline.Replace(text, new MatchEvaluator(AnchorInlineEvaluator));

            //	Last, handle reference-style shortcuts: [link text]
            //  These must come last in case you've also got [link test][1]
            //  or [link test](/foo)
            text = _anchorRefShortcut.Replace(text, new MatchEvaluator(AnchorRefShortcutEvaluator));
            return text;
        }

        private string AnchorRefEvaluator(Match match)
        {
            string wholeMatch = match.Groups[1].Value;
            string linkText = match.Groups[2].Value;
            string linkID = match.Groups[3].Value.ToLowerInvariant();

            string result;

            // for shortcut links like [this][].
            if (linkID == "")
                linkID = linkText.ToLowerInvariant();

            if (_urls.ContainsKey(linkID))
            {
                string url = _urls[linkID];

                url = EscapeBoldItalic(url);
                url = EncodeProblemUrlChars(url);
                result = "<a href=\"" + url + "\"";

                if (_titles.ContainsKey(linkID))
                {
                    string title = _titles[linkID];
                    title = title.Replace("*", _escapeTable["*"]);
                    title = title.Replace("_", _escapeTable["_"]);
                    result += " title=\"" + title + "\"";
                }

                result += ">" + linkText + "</a>";
            }
            else
                result = wholeMatch;

            return result;
        }

        private string AnchorRefShortcutEvaluator(Match match)
        {
            string wholeMatch = match.Groups[1].Value;
            string linkText = match.Groups[2].Value;
            string linkID = Regex.Replace(linkText.ToLowerInvariant(), @"[ ]*\n[ ]*", " ");  // lower case and remove newlines / extra spaces

            string result;

            if (_urls.ContainsKey(linkID))
            {
                string url = _urls[linkID];

                url = EscapeBoldItalic(url);
                url = EncodeProblemUrlChars(url);
                result = "<a href=\"" + url + "\"";

                if (_titles.ContainsKey(linkID))
                {
                    string title = _titles[linkID];
                    title = title.Replace("*", _escapeTable["*"]);
                    title = title.Replace("_", _escapeTable["_"]);
                    result += " title=\"" + title + "\"";
                }

                result += ">" + linkText + "</a>";
            }
            else
                result = wholeMatch;

            return result;
        }

        /// <summary>
        /// escapes Bold [ * ] and Italic [ _ ] characters
        /// </summary>
        private string EscapeBoldItalic(string s)
        {
            s = s.Replace("*", _escapeTable["*"]);
            s = s.Replace("_", _escapeTable["_"]);
            return s;
        }


        /// <summary>
        /// encodes problem characters in URLs, such as 
        /// * _  and optionally ' () []  :
        /// this is to avoid problems with markup later
        /// </summary>
        private string EncodeProblemUrlChars(string url)
        {
            if (_encodeProblemUrlCharacters)
            {
                url = url.Replace("*", "%2A");
                url = url.Replace("_", "%5F");
                url = url.Replace("'", "%27");
                url = url.Replace("(", "%28");
                url = url.Replace(")", "%29");
                url = url.Replace("[", "%5B");
                url = url.Replace("]", "%5D");
                if (url.Length > 7 && url.Substring(7).Contains(":"))
                {
                    // replace any colons in the body of the URL that are NOT followed by 2 or more numbers
                    url = url.Substring(0, 7) + Regex.Replace(url.Substring(7), @":(?!\d{2,})", "%3A");
                }
            }

            return url;
        }

        private string AnchorInlineEvaluator(Match match)
        {
            string linkText = match.Groups[2].Value;
            string url = match.Groups[3].Value;
            string title = match.Groups[6].Value;
            string result;
           
            url = EscapeBoldItalic(url);
            if (url.StartsWith("<") && url.EndsWith(">")) 
                url = url.Substring(1, url.Length - 2); // remove <>'s surrounding URL, if present
            url = EncodeProblemUrlChars(url);

            result = string.Format("<a href=\"{0}\"", url);

            if (!String.IsNullOrEmpty(title))
            {
                title = title.Replace("\"", "&quot;");
                title = title.Replace("*", _escapeTable["*"]);
                title = title.Replace("_", _escapeTable["_"]);
                result += string.Format(" title=\"{0}\"", title);
            }

            result += string.Format(">{0}</a>", linkText);
            return result;
        }

        private static Regex _imagesRef = new Regex(@"
                    (               # wrap whole match in $1
                    !\[
                        (.*?)	    # alt text = $2
                    \]

                    [ ]?            # one optional space
                    (?:\n[ ]*)?		# one optional newline followed by spaces

                    \[
                        (.*?)       # id = $3
                    \]

                    )", RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);

        private static Regex _imagesInline = new Regex(String.Format(@"
              (                 # wrap whole match in $1
                !\[
                    (.*?)		# alt text = $2
                \]
                \s?             # one optional whitespace character
                \(			    # literal paren
                    [ \t]*
                    ({0})   # href = $3
                    [ \t]*
                    (			# $4
                    (['\x22])	# quote char = $5
                    (.*?)		# title = $6
                    \5		    # matching quote
                    [ \t]*
                    )?			# title is optional
                \)
              )", GetNestedParensPattern()),
                  RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Turn Markdown image shortcuts into <img> tags. 
        /// </summary>
        private string DoImages(string text)
        {
            // First, handle reference-style labeled images: ![alt text][id]
            text = _imagesRef.Replace(text, new MatchEvaluator(ImageReferenceEvaluator));

            // Next, handle inline images:  ![alt text](url "optional title")
            // Don't forget: encode * and _
            text = _imagesInline.Replace(text, new MatchEvaluator(ImageInlineEvaluator));

            return text;
        }

        private string ImageReferenceEvaluator(Match match)
        {
            string wholeMatch = match.Groups[1].Value;
            string altText = match.Groups[2].Value;
            string linkID = match.Groups[3].Value.ToLowerInvariant();
            string result;

            // for shortcut links like ![this][].
            if (linkID == "")
                linkID = altText.ToLowerInvariant();

            altText = altText.Replace("\"", "&quot;");

            if (_urls.ContainsKey(linkID))
            {
                string url = _urls[linkID];
                url = EscapeBoldItalic(url);
                url = EncodeProblemUrlChars(url);
                result = string.Format("<img src=\"{0}\" alt=\"{1}\"", url, altText);

                if (_titles.ContainsKey(linkID))
                {
                    string title = _titles[linkID];
                    title = title.Replace("*", _escapeTable["*"]);
                    title = title.Replace("_", _escapeTable["_"]);

                    result += string.Format(" title=\"{0}\"", title);
                }

                result += _emptyElementSuffix;
            }
            else
            {
                // If there's no such link ID, leave intact:
                result = wholeMatch;
            }

            return result;
        }

        private string ImageInlineEvaluator(Match match)
        {
            string alt = match.Groups[2].Value;
            string url = match.Groups[3].Value;
            string title = match.Groups[6].Value;
            string result;
            
            alt = alt.Replace("\"", "&quot;");
            title = title.Replace("\"", "&quot;");

            url = EscapeBoldItalic(url);
            if (url.StartsWith("<") && url.EndsWith(">"))
                url = url.Substring(1, url.Length - 2);    // Remove <>'s surrounding URL, if present

            url = EncodeProblemUrlChars(url);

            result = string.Format("<img src=\"{0}\" alt=\"{1}\"", url, alt);

            if (!String.IsNullOrEmpty(title))
            {
                title = title.Replace("*", _escapeTable["*"]);
                title = title.Replace("_", _escapeTable["_"]);
                result += string.Format(" title=\"{0}\"", title);
            }

            result += _emptyElementSuffix;

            return result;
        }

        private static Regex _header1 = new Regex(@"^(.+?)[ \t]*\n=+[ \t]*\n+",
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static Regex _header2 = new Regex(@"^(.+?)[ \t]*\n-+[ \t]*\n+",
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static Regex _header3 = new Regex(@"
                ^(\#{1,6})	# $1 = string of #'s
                [ \t]*
                (.+?)		# $2 = Header text
                [ \t]*
                \#*			# optional closing #'s (not counted)
                \n+",
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private string DoHeaders(string text)
        {
            // Setext-style headers:
            //
            // Header 1
            // ========
            //  
            // Header 2
            // --------
            //
            text = _header1.Replace(text, new MatchEvaluator(SetextHeader1Evaluator));
            text = _header2.Replace(text, new MatchEvaluator(SetextHeader2Evaluator));

            // atx-style headers:
            //    # Header 1
            //    ## Header 2
            //    ## Header 2 with closing hashes ##
            //    ...
            //    ###### Header 6
            text = _header3.Replace(text, new MatchEvaluator(AtxHeaderEvaluator));

            return text;
        }

        private string SetextHeader1Evaluator(Match match)
        {
            string header = match.Groups[1].Value;
            return string.Concat("<h1>", RunSpanGamut(header), "</h1>\n\n");
        }

        private string SetextHeader2Evaluator(Match match)
        {
            string header = match.Groups[1].Value;
            return string.Concat("<h2>", RunSpanGamut(header), "</h2>\n\n");
        }

        private string AtxHeaderEvaluator(Match match)
        {
            string headerSig = match.Groups[1].Value;
            string headerText = match.Groups[2].Value;

            return string.Concat("<h", headerSig.Length, ">", RunSpanGamut(headerText), "</h", headerSig.Length, ">\n\n");
        }

        private static string _wholeList = string.Format(@"
            (                               # $1 = whole list
              (                             # $2
                [ ]{{0,{1}}}
                ({0})                       # $3 = first list item marker
                [ \t]+
              )
              (?s:.+?)
              (                             # $4
                  \z
                |
                  \n{{2,}}
                  (?=\S)
                  (?!                       # Negative lookahead for another list item marker
                    [ \t]*
                    {0}[ \t]+
                  )
              )
            )", GetMarkerAnyPattern(), _tabWidth - 1);

        private static Regex _listNested = new Regex(@"^" + _wholeList,
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static Regex _listTopLevel = new Regex(@"(?:(?<=\n\n)|\A\n?)" + _wholeList,
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private string DoLists(string text)
        {
            // Re-usable pattern to match any entirel ul or ol list:

            // We use a different prefix before nested lists than top-level lists.
            // See extended comment in _ProcessListItems().
            if (_listLevel > 0)
                text = _listNested.Replace(text, new MatchEvaluator(ListEvaluator));
            else
                text = _listTopLevel.Replace(text, new MatchEvaluator(ListEvaluator));

            return text;
        }

        private string ListEvaluator(Match match)
        {
            string list = match.Groups[1].Value;
            string listType = Regex.IsMatch(match.Groups[3].Value, _markerUL) ? "ul" : "ol";
            string result;

            // Turn double returns into triple returns, so that we can make a
            // paragraph for the last item in a list, if necessary:
            list = Regex.Replace(list, @"\n{2,}", "\n\n\n");
            result = ProcessListItems(list, GetMarkerAnyPattern());

            // from Markdown 1.0.2b8 -- not doing this for now
            //
            // Trim any trailing whitespace, to put the closing `</$list_type>`
            // up on the preceding line, to get it past the current stupid
            // HTML block parser. This is a hack to work around the terrible
            // hack that is the HTML block parser.
            //
            //result = Regex.Replace(output, @"\s+$", "");
            //result = string.Format("<{0}>{1}</{0}>\n", listType, result);

            result = string.Format("<{0}>\n{1}</{0}>\n", listType, result);

            return result;
        }

        /// <summary>
        /// Process the contents of a single ordered or unordered list, splitting it
        /// into individual list items.
        /// </summary>
        private string ProcessListItems(string list, string marker)
        {
            // The listLevel global keeps track of when we're inside a list.
            // Each time we enter a list, we increment it; when we leave a list,
            // we decrement. If it's zero, we're not in a list anymore.

            // We do this because when we're not inside a list, we want to treat
            // something like this:

            //    I recommend upgrading to version
            //    8. Oops, now this line is treated
            //    as a sub-list.

            // As a single paragraph, despite the fact that the second line starts
            // with a digit-period-space sequence.

            // Whereas when we're inside a list (or sub-list), that line will be
            // treated as the start of a sub-list. What a kludge, huh? This is
            // an aspect of Markdown's syntax that's hard to parse perfectly
            // without resorting to mind-reading. Perhaps the solution is to
            // change the syntax rules such that sub-lists must start with a
            // starting cardinal number; e.g. "1." or "a.".

            _listLevel++;

            // Trim trailing blank lines:
            list = Regex.Replace(list, @"\n{2,}\z", "\n");

            string pattern = string.Format(
              @"(\n)?                      # leading line = $1
                (^[ \t]*)                  # leading whitespace = $2
                ({0}) [ \t]+               # list marker = $3
                ((?s:.+?)                  # list item text = $4
                (\n{{1,2}}))      
                (?= \n* (\z | \2 ({0}) [ \t]+))", marker);

            list = Regex.Replace(list, pattern, new MatchEvaluator(ListEvaluator2),
                                  RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);
            _listLevel--;
            return list;
        }

        private string ListEvaluator2(Match match)
        {
            string item = match.Groups[4].Value;
            string leadingLine = match.Groups[1].Value;

            if ((leadingLine != null && leadingLine != "") || Regex.IsMatch(item, @"\n{2,}"))
                item = RunBlockGamut(Outdent(item));
            else
            {
                // Recursion for sub-lists:
                item = DoLists(Outdent(item));
                item = item.TrimEnd('\n');
                item = RunSpanGamut(item);
            }

            return string.Format("<li>{0}</li>\n", item);
        }


        private static Regex _codeBlock = new Regex(string.Format(@"
                    (?:\n\n|\A)
                    (	                     # $1 = the code block -- one or more lines, starting with a space/tab
                    (?:
                        (?:[ ]{{{0}}} | \t)  # Lines must start with a tab or a tab-width of spaces
                        .*\n+
                    )+
                    )
                    ((?=^[ ]{{0,{0}}}\S)|\Z) # Lookahead for non-space at line-start, or end of doc",
                                            _tabWidth), RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private string DoCodeBlocks(string text)
        {
            text = _codeBlock.Replace(text, new MatchEvaluator(CodeBlockEvaluator));
            return text;
        }

        private string CodeBlockEvaluator(Match match)
        {
            string codeBlock = match.Groups[1].Value;

            codeBlock = EncodeCode(Outdent(codeBlock));
            codeBlock = Detab(codeBlock);
            codeBlock = _newlinesLeadingTrailing.Replace(codeBlock, "");

            return string.Concat("\n\n<pre><code>", codeBlock, "\n</code></pre>\n\n");
        }

        private static Regex _codeSpan = new Regex(@"
                    (?<!\\)		# Character before opening ` can't be a backslash
                    (`+)		# $1 = Opening run of `
                    (.+?)		# $2 = The code block
                    (?<!`)
                    \1
                    (?!`)", RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);

        private string DoCodeSpans(string text)
        {
            //
            //    *	Backtick quotes are used for <code></code> spans.
            //
            //    *	You can use multiple backticks as the delimiters if you want to
            //        include literal backticks in the code span. So, this input:
            //
            //        Just type ``foo `bar` baz`` at the prompt.
            //
            //        Will translate to:
            //
            //          <p>Just type <code>foo `bar` baz</code> at the prompt.</p>
            //
            //        There's no arbitrary limit to the number of backticks you
            //        can use as delimters. If you need three consecutive backticks
            //        in your code, use four for delimiters, etc.
            //
            //    *	You can use spaces to get literal backticks at the edges:
            //
            //          ... type `` `bar` `` ...
            //
            //        Turns to:
            //
            //          ... type <code>`bar`</code> ...	        
            //

            return _codeSpan.Replace(text, new MatchEvaluator(CodeSpanEvaluator));
        }

        private string CodeSpanEvaluator(Match match)
        {
            string span = match.Groups[2].Value;
            span = Regex.Replace(span, @"^[ \t]*", ""); // leading whitespace
            span = Regex.Replace(span, @"[ \t]*$", ""); // trailing whitespace
            span = EncodeCode(span);

            return string.Concat("<code>", span, "</code>");
        }


        /// <summary>
        /// Encode/escape certain characters inside Markdown code runs.
        /// </summary>
        /// <remarks>
        /// The point is that in code, these characters are literals, and lose their 
        /// special Markdown meanings.
        /// </remarks>
        private string EncodeCode(string code)
        {
            // Encode all ampersands; HTML entities are not
            // entities within a Markdown code span.
            code = code.Replace("&", "&amp;");

            // Do the angle bracket song and dance
            code = code.Replace("<", "&lt;");
            code = code.Replace(">", "&gt;");

            // Now, escape characters that are magic in Markdown
            code = code.Replace("*", _escapeTable["*"]);
            code = code.Replace("_", _escapeTable["_"]);
            code = code.Replace("{", _escapeTable["{"]);
            code = code.Replace("}", _escapeTable["}"]);
            code = code.Replace("[", _escapeTable["["]);
            code = code.Replace("]", _escapeTable["]"]);
            code = code.Replace(@"\", _escapeTable[@"\"]);

            return code;
        }


        private static Regex _strong = new Regex(GetBoldPattern(),
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);
        private static Regex _italics = new Regex(GetItalicPattern(),
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);

        private string DoItalicsAndBold(string text)
        {
            // <strong> must go first:
            text = _strong.Replace(text, GetBoldReplace());
            // Then <em>:
            text = _italics.Replace(text, GetItalicReplace());

            return text;
        }

        private static Regex _blockquote = new Regex(@"
            (                           # Wrap whole match in $1
                (
                ^[ \t]*>[ \t]?			# '>' at the start of a line
                    .+\n				# rest of the first line
                (.+\n)*					# subsequent consecutive lines
                \n*						# blanks
                )+
            )", RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Compiled);

        private string DoBlockQuotes(string text)
        {
            return _blockquote.Replace(text, new MatchEvaluator(BlockQuoteEvaluator));
        }

        private string BlockQuoteEvaluator(Match match)
        {
            string bq = match.Groups[1].Value;

            bq = Regex.Replace(bq, @"^[ \t]*>[ \t]?", "", RegexOptions.Multiline);   // trim one level of quoting
            bq = Regex.Replace(bq, @"^[ \t]+$", "", RegexOptions.Multiline);         // trim whitespace-only lines
            bq = RunBlockGamut(bq);                                                  // recurse

            bq = Regex.Replace(bq, @"^", "  ", RegexOptions.Multiline);

            // These leading spaces screw with <pre> content, so we need to fix that:
            bq = Regex.Replace(bq, @"(\s*<pre>.+?</pre>)", new MatchEvaluator(BlockQuoteEvaluator2), RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

            return string.Format("<blockquote>\n{0}\n</blockquote>\n\n", bq);
        }

        private string BlockQuoteEvaluator2(Match match)
        {
            return Regex.Replace(match.Groups[1].Value, @"^  ", "", RegexOptions.Multiline);
        }

        private static Regex _newlinesLeadingTrailing = new Regex(@"^\n+|\n+\z", RegexOptions.Compiled);
        private static Regex _newlinesMultiple = new Regex(@"\n{2,}", RegexOptions.Compiled);
        private static Regex _tabsLeading = new Regex(@"^([ \t]*)", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        private string FormParagraphs(string text)
        {
            text = _newlinesLeadingTrailing.Replace(text, "");

            string[] grafs = _newlinesMultiple.Split(text);

            // Wrap <p> tags.
            for (int i = 0; i < grafs.Length; i++)
            {
                if (!_htmlBlocks.ContainsKey(grafs[i]))
                {
                    string block = grafs[i];

                    block = RunSpanGamut(block);
                    block = _tabsLeading.Replace(block, "<p>");
                    block += "</p>";

                    grafs[i] = block;
                }
            }

            // Unhashify HTML blocks
            for (int i = 0; i < grafs.Length; i++)
            {
                if (_htmlBlocks.ContainsKey(grafs[i]))
                    grafs[i] = _htmlBlocks[grafs[i]];
            }

            return string.Join("\n\n", grafs);
        }


        private static Regex _autolinkBare = new Regex(@"(^|\s)(https?|ftp)(://[-A-Z0-9+&@#/%?=~_|\[\]\(\)!:,\.;]*[-A-Z0-9+&@#/%=~_|\[\]])($|\W)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private string DoAutoLinks(string text)
        {

            if (_autoHyperlink)
            {
                // fixup arbitrary URLs by adding Markdown < > so they get linked as well
                // note that at this point, all other URL in the text are already hyperlinked as <a href=""></a>
                // *except* for the <http://www.foo.com> case
                text = _autolinkBare.Replace(text, @"$1<$2$3>$4");
            }

            // Hyperlinks: <http://foo.com>
            text = Regex.Replace(text, "<((https?|ftp):[^'\">\\s]+)>", new MatchEvaluator(HyperlinkEvaluator));

            if (_linkEmails)
            {
                // Email addresses: <address@domain.foo>
                string pattern =
                    @"<
                      (?:mailto:)?
                      (
                        [-.\w]+
                        \@
                        [-a-z0-9]+(\.[-a-z0-9]+)*\.[a-z]+
                      )
                      >";
                text = Regex.Replace(text, pattern, new MatchEvaluator(EmailEvaluator), RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            }

            return text;
        }

        private string HyperlinkEvaluator(Match match)
        {
            string link = match.Groups[1].Value;
            return string.Format("<a href=\"{0}\">{0}</a>", link);
        }

        private string EmailEvaluator(Match match)
        {
            string email = UnescapeSpecialChars(match.Groups[1].Value);

            //
            //    Input: an email address, e.g. "foo@example.com"
            //
            //    Output: the email address as a mailto link, with each character
            //            of the address encoded as either a decimal or hex entity, in
            //            the hopes of foiling most address harvesting spam bots. E.g.:
            //
            //      <a href="&#x6D;&#97;&#105;&#108;&#x74;&#111;:&#102;&#111;&#111;&#64;&#101;
            //        x&#x61;&#109;&#x70;&#108;&#x65;&#x2E;&#99;&#111;&#109;">&#102;&#111;&#111;
            //        &#64;&#101;x&#x61;&#109;&#x70;&#108;&#x65;&#x2E;&#99;&#111;&#109;</a>
            //
            //    Based by a filter by Matthew Wickline, posted to the BBEdit-Talk
            //    mailing list: <http://tinyurl.com/yu7ue>
            //
            email = "mailto:" + email;

            // leave ':' alone (to spot mailto: later) 
            email = EncodeEmailAddress(email);

            email = string.Format("<a href=\"{0}\">{0}</a>", email);

            // strip the mailto: from the visible part
            email = Regex.Replace(email, "\">.+?:", "\">");
            return email;
        }

        /// <summary>
        /// encodes email address randomly  
        /// roughly 10% raw, 45% hex, 45% dec 
        /// note that @ is always encoded and : never is
        /// </summary>
        private string EncodeEmailAddress(string addr)
        {
            var sb = new StringBuilder(addr.Length * 5);
            var rand = new Random();
            int r;
            foreach (char c in addr)
            {
                r = rand.Next(1, 100);
                if ((r > 90 || c == ':') && c != '@')
                    sb.Append(c);                         // m
                else if (r < 45)
                    sb.AppendFormat("&#x{0:x};", (int)c); // &#x6D
                else
                    sb.AppendFormat("&#{0};", (int)c);    // &#109
            }
            return sb.ToString();
        }


        private static Regex _amps = new Regex(@"&(?!#?[xX]?([0-9a-fA-F]+|\w+);)", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
        private static Regex _angles = new Regex(@"<(?![A-Za-z/?\$!])", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        /// <summary>
        /// Smart processing for ampersands and angle brackets that need to be encoded.
        /// </summary>
        private string EncodeAmpsAndAngles(string text)
        {
            // Ampersand-encoding based entirely on Nat Irons's Amputator MT plugin:
            // http://bumppo.net/projects/amputator/
            text = _amps.Replace(text, "&amp;");

            // Encode naked <'s
            text = _angles.Replace(text, "&lt;");

            return text;
        }

        /// <summary>
        /// process any escaped characters such as \`, \*, \[ etc
        /// </summary>
        private string EncodeBackslashEscapes(string text)
        {
            // Must process escaped backslashes first.
            foreach (var pair in _backslashEscapeTable)
                text = text.Replace(pair.Key, pair.Value);
            return text;
        }

        /// <summary>
        /// swap back in all the special characters we've hidden
        /// </summary>
        private string UnescapeSpecialChars(string text)
        {
            foreach (var pair in _escapeTable)
                text = text.Replace(pair.Value, pair.Key);
            return text;
        }

        private static Regex _outDent = new Regex(@"^(\t|[ ]{1," + _tabWidth + @"})", RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>
        /// Remove one level of line-leading tabs or spaces
        /// </summary>
        private string Outdent(string block)
        {
            return _outDent.Replace(block, "");
        }

        private static Regex _deTab = new Regex(@"^(.*?)(\t+)", RegexOptions.Multiline | RegexOptions.Compiled);

        private string Detab(string text)
        {
            // Inspired from a post by Bart Lateur: 
            // http://www.nntp.perl.org/group/perl.macperl.anyperl/154
            //
            // without a beginning of line anchor, the above has HIDEOUS performance
            // so I added a line anchor and we count the # of tabs beyond that.
            return _deTab.Replace(text, new MatchEvaluator(TabEvaluator));
        }

        private string TabEvaluator(Match match)
        {
            string leading = match.Groups[1].Value;
            int tabCount = match.Groups[2].Value.Length;
            return String.Concat(leading, new String(' ', (_tabWidth - leading.Length % _tabWidth) + ((tabCount - 1) * _tabWidth)));
        }

        /// <summary>
        /// this is to emulate what's evailable in PHP
        /// </summary>
        private static string RepeatString(string text, int count)
        {
            var sb = new StringBuilder(text.Length * count);

            for (int i = 0; i < count; i++)
                sb.Append(text);

            return sb.ToString();
        }

    }
}