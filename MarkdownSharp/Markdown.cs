/*
 * MarkdownSharp
 * -------------
 * a C# Markdown processor
 * 
 * Markdown is a text-to-HTML conversion tool for web writers
 * Copyright (c) 2004 John Gruber
 * http://daringfireball.net/projects/markdown/
 * 
 * Markdown.NET Copyright (c) 2004-2009 Milan Negovan
 * http://www.aspnetresources.com
 * 
 * MarkdownSharp Copyright (c) 2009 Jeff Atwood
 * http://stackoverflow.com
 * http://www.codinghorror.com/blog/
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
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

namespace MarkdownSharp
{
    public class Markdown
    {
        public struct Pair
        {
            public string First;
            public string Second;
        }

        /// <summary>
        /// enter ">" here for HTML output
        /// enter "/>" here for XHTML output
        /// </summary>
        private const string _emptyElementSuffix = ">";
        /// <summary>
        /// when this is true, RETURN becomes a literal newline. 
        /// Beware: this is a major deviation from the Markdown spec!
        /// </summary>
        private const bool _autoNewlines = false;
        /// <summary>
        /// Tabs are automatically converted to spaces as part of the transform 
        /// this variable determines how "wide" those tabs become in spaces
        /// </summary>
        private const int _tabWidth = 4;
        /// <summary>
        /// maximum nested bracket depth supported by the transform
        /// </summary>
        private const int _nestedBracketDepth = 6;
        /// <summary>
        /// when true, email addresses will be auto-linked if present
        /// </summary>
        private const bool _linkEmails = false;

        private const string _markerUL = @"[*+-]";
        private const string _markerOL = @"\d+[.]";

        private static string _nestedBracketsPattern;
        private static string _markerAnyPattern;
                
        private static readonly Hashtable _escapeTable;
        private static readonly Hashtable _backslashEscapeTable;

        private Hashtable _urls;
        private Hashtable _titles;
        private Hashtable _htmlBlocks;

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
            _escapeTable = new Hashtable();

            _escapeTable[@"\"] = @"\".GetHashCode();
            _escapeTable["`"] = "`".GetHashCode();
            _escapeTable["*"] = "*".GetHashCode();
            _escapeTable["_"] = "_".GetHashCode();
            _escapeTable["{"] = "{".GetHashCode();
            _escapeTable["}"] = "}".GetHashCode();
            _escapeTable["["] = "[".GetHashCode();
            _escapeTable["]"] = "]".GetHashCode();
            _escapeTable["("] = "(".GetHashCode();
            _escapeTable[")"] = ")".GetHashCode();
            _escapeTable[">"] = ">".GetHashCode();
            _escapeTable["#"] = "#".GetHashCode();
            _escapeTable["+"] = "+".GetHashCode();
            _escapeTable["-"] = "-".GetHashCode();
            _escapeTable["."] = ".".GetHashCode();
            _escapeTable["!"] = "!".GetHashCode();

            // Create an identical table but for escaped characters.
            _backslashEscapeTable = new Hashtable();

            foreach (string key in _escapeTable.Keys)
                _backslashEscapeTable[@"\" + key] = _escapeTable[key];
        }

        public Markdown()
        {
            _urls = new Hashtable();
            _titles = new Hashtable();
            _htmlBlocks = new Hashtable();
        }

        public static string GetNestedBracketsPattern()
        {
            if (_nestedBracketsPattern == null)
                _nestedBracketsPattern = RepeatString(@"(?>[^\[\]]+|\[", _nestedBracketDepth) + RepeatString(@"\])*", _nestedBracketDepth);
            return _nestedBracketsPattern;
        }

        public static string GetMarkerAnyPattern()
        {
            if (_markerAnyPattern == null)
                _markerAnyPattern = string.Format("(?:{0}|{1})", _markerUL, _markerOL);
            return _markerAnyPattern;
        }

        /// <summary>
        /// Main function. The order in which other subs are called here is
        /// essential. Link and image substitutions need to happen before
        /// EscapeSpecialChars(), so that any *'s or _'s in the <a>
        /// and <img> tags get encoded.
        /// </summary>
        public string Transform(string text)
        {
            // Standardize line endings:
            // DOS to Unix and Mac to Unix
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");

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
						(?:\n+|\Z)", _tabWidth - 1), RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// Strips link definitions from text, stores the URLs and titles in hash references.
        /// </summary>
        /// <remarks>
        /// [id]: url "optional title"
        /// </remarks>
        private string StripLinkDefinitions(string text)
        {
            text = _linkDef.Replace(text, new MatchEvaluator(LinkEvaluator));
            return text;
        }

        private string LinkEvaluator(Match match)
        {
            string linkID = match.Groups[1].Value.ToLower();
            _urls[linkID] = EncodeAmpsAndAngles(match.Groups[2].Value);

            if (match.Groups[3] != null && match.Groups[3].Length > 0)
                _titles[linkID] = match.Groups[3].Value.Replace("\"", "&quot;");

            return "";
        }


        // profiler says this one is expensive
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
				)", _blockTags2), RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

        private static Regex _blocksHr = new Regex(string.Format(@"
                (?:
					(?<=\n\n)		    # Starting after a blank line
					|				    # or
					\A\n?			    # the beginning of the doc
				)
				(						# save in $1
					[ ]{{0, {0}}}
					<(hr)				# start tag = $2
					\b					# word break
					([^<>])*?			#
					/?>					# the matching end tag
					[ \t]*
					(?=\n{{2,}}|\Z)		# followed by a blank line or end of document
				)", _tabWidth - 1), RegexOptions.IgnorePatternWhitespace);

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
				)", _tabWidth - 1), RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// Hashify HTML blocks
        /// </summary>
        private string HashHTMLBlocks(string text)
        {
            /*
             We only want to do this for block-level HTML tags, such as headers,
             lists, and tables. That's because we still want to wrap <p>s around
             "paragraphs" that are wrapped in non-block-level tags, such as anchors,
             phrase emphasis, and spans. The list of tags we're looking for is
             hard-coded.
            */

            /*
             First, look for nested blocks, e.g.:
            <div>
                <div>
                tags for inner block must be indented.
                </div>
            </div>
	        
             The outermost tags must start at the left margin for this to match, and
             the inner nested divs must be indented.
             We need to do this before the next, more liberal match, because the next
             match will start at the first `<div>` and stop at the first `</div>`.
            */
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

        private static Regex _block1 = new Regex(@"^[ ]{0,2}([ ]?\*[ ]?){3,}[ \t]*$", RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        private static Regex _block2 = new Regex(@"^[ ]{0,2}([ ]? -[ ]?){3,}[ \t]*$", RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        private static Regex _block3 = new Regex(@"^[ ]{0,2}([ ]? _[ ]?){3,}[ \t]*$", RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// These are all the transformations that form block-level 
        /// tags like paragraphs, headers, and list items.
        /// </summary>
        private string RunBlockGamut(string text)
        {
            text = DoHeaders(text);

            text = _block1.Replace(text, "<hr" + _emptyElementSuffix + "\n");
            text = _block2.Replace(text, "<hr" + _emptyElementSuffix + "\n");
            text = _block3.Replace(text, "<hr" + _emptyElementSuffix + "\n");

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

            text = EscapeSpecialChars(text);

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
            text = Regex.Replace(text, @" {2,}\n", string.Format("<br{0}\n", _emptyElementSuffix));

            return text;
        }

        private static Regex _htmlTokens = new Regex(
            @"(?s:<!(?:--.*?--\s*)+>)|(?s:<\?.*?\?>)|" + 
            RepeatString(@"(?:<[a-z\/!$](?:[^<>]|", _nestedBracketDepth) + 
            RepeatString(@")*>)", _nestedBracketDepth), 
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

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
        private ArrayList TokenizeHTML(string text)
        {
            // Regular expression derived from the _tokenize() subroutine in 
            // Brad Choate's MTRegex plugin.
            // http://www.bradchoate.com/past/mtregex.php
            int pos = 0;
            ArrayList tokens = new ArrayList();

            foreach (Match m in _htmlTokens.Matches(text))
            {
                string wholeTag = m.Value;
                int tagStart = m.Index;
                Pair token;

                if (pos < tagStart)
                {
                    token = new Pair();
                    token.First = "text";
                    token.Second = text.Substring(pos, tagStart - pos);
                    tokens.Add(token);
                }

                token = new Pair();
                token.First = "tag";
                token.Second = wholeTag;
                tokens.Add(token);

                pos = m.Index + m.Length;
            }

            if (pos < text.Length)
            {
                Pair token = new Pair();
                token.First = "text";
                token.Second = text.Substring(pos, text.Length - pos);
                tokens.Add(token);
            }

            return tokens;
        }


        private string EscapeSpecialChars(string text)
        {
            ArrayList tokens = TokenizeHTML(text);

            // now, rebuild text from the tokens
            var sb = new StringBuilder(text.Length);

            foreach (Pair token in tokens)
            {
                string value = token.Second.ToString();

                if (token.First.Equals("tag"))
                    /*
                        Within tags, encode * and _ so they don't conflict with their use 
                        in Markdown for italics and strong. We're replacing each 
                        such character with its corresponding MD5 checksum value; 
                        this is likely overkill, but it should prevent us from colliding
                        with the escape values by accident.
                    */
                    value = value.Replace("*", _escapeTable["*"].ToString()).Replace("_", _escapeTable["_"].ToString());
                else
                    value = EncodeBackslashEscapes(value);

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
		    )", GetNestedBracketsPattern()), RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

        private static Regex _anchorInline = new Regex(string.Format(@"
                (                          # wrap whole match in $1
		            \[
			            ({0})              # link text = $2
		            \]
		            \(                     # literal paren
			            [ \t]*
			            <?(.*?)>?          # href = $3
			            [ \t]*
			            (                  # $4
			            (['\x22])          # quote char = $5
			            (.*?)              # Title = $6
			            \5                 # matching quote
			            )?                 # title is optional
		            \)
					([\+])?					# is target blank = $7	
        		)", GetNestedBracketsPattern()), RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);


        /// <summary>
        /// Turn Markdown link shortcuts [link text](url "title") or [link text][id] into HTML anchor tags. 
        /// </summary>
        private string DoAnchors(string text)
        {
            // First, handle reference-style links: [link text] [id]
            text = _anchorRef.Replace(text, new MatchEvaluator(AnchorReferenceEvaluator));

            // Next, inline-style links: [link text](url "optional title") or [link text](url "optional title")+ will be added target = "_blank"
            text = _anchorInline.Replace(text, new MatchEvaluator(AnchorInlineEvaluator));

            return text;
        }

        private string AnchorReferenceEvaluator(Match match)
        {
            string wholeMatch = match.Groups[1].Value;
            string linkText = match.Groups[2].Value;
            string linkID = match.Groups[3].Value.ToLower();
            string url = null;
            string res = null;
            string title = null;

            // for shortcut links like [this][].
            if (linkID == "")
                linkID = linkText.ToLower();

            if (_urls[linkID] != null)
            {
                url = _urls[linkID].ToString();
                url = UrlEncoderFixup(url);
                res = "<a href=\"" + url + "\"";

                if (_titles[linkID] != null)
                {
                    title = _titles[linkID].ToString();
                    title = title.Replace("*", _escapeTable["*"].ToString()).Replace("_", _escapeTable["_"].ToString());
                    res += " title=\"" + title + "\"";
                }

                res += ">" + linkText + "</a>";
            }
            else
                res = wholeMatch;

            return res;
        }

        /// <summary>
        /// encodes problem characters in URLS, such as 
        /// ' () [] * _ :
        /// this is to avoid problems with markup later
        /// </summary>
        private string UrlEncoderFixup(string url)
        {
            url = url.Replace("'", "%27");
            url = url.Replace("(", "%28");
            url = url.Replace(")", "%29");
            url = url.Replace("[", "%5B");
            url = url.Replace("]", "%5D");
            url = url.Replace("*", "%2A");
            url = url.Replace("_", "%5F");
            if (url.Length > 7 && url.Substring(7).Contains(":"))
            {
                // replace any colons NOT followed by 2 or more numbers
                url = url.Substring(0, 7) + Regex.Replace(url.Substring(7), @":(?!\d{2,})", "%3A");
            }
            return url;
        }

        private string AnchorInlineEvaluator(Match match)
        {
            string linkText = match.Groups[2].Value;
            string url = match.Groups[3].Value;
            string title = match.Groups[6].Value;
            bool isTargetBlank = match.Groups[7].Value == "+";
            string res = null;

            url = UrlEncoderFixup(url);

            if (isTargetBlank)
                res = string.Format("<a href=\"{0}\" target=\"_blank\"", url);
            else
                res = string.Format("<a href=\"{0}\"", url);

            if (!String.IsNullOrEmpty(title))
            {
                title = title.Replace("\"", "&quot;").Replace("*", _escapeTable["*"].ToString()).Replace("_", _escapeTable["_"].ToString());
                res += string.Format(" title=\"{0}\"", title);
            }

            res += string.Format(">{0}</a>", linkText);
            return res;
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

		            )", RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
        private static Regex _imagesInline = new Regex(@"
                (				# wrap whole match in $1
		        !\[
			        (.*?)		# alt text = $2
		        \]
		        \(			    # literal paren
			        [ \t]*
			        <?(\S+?)>?	# src url = $3
			        [ \t]*
			        (			# $4
			        (['\x22])	# quote char = $5
			        (.*?)		# title = $6
			        \5		    # matching quote
			        [ \t]*
			        )?			# title is optional
		        \)
		        )", RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

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
            string linkID = match.Groups[3].Value.ToLower();
            string url;
            string res;
            string title;

            // for shortcut links like ![this][].
            if (linkID == "")
                linkID = altText.ToLower();

            altText = altText.Replace("\"", "&quot;");

            if (_urls[linkID] != null)
            {
                url = _urls[linkID].ToString();

                url = UrlEncoderFixup(url);

                res = string.Format("<img src=\"{0}\" alt=\"{1}\"", url, altText);

                if (_titles[linkID] != null)
                {
                    title = _titles[linkID].ToString();
                    title = title.Replace("*", _escapeTable["*"].ToString()).Replace("_", _escapeTable["_"].ToString());

                    res += string.Format(" title=\"{0}\"", title);
                }

                res += _emptyElementSuffix;
            }
            else
            {
                // If there's no such link ID, leave intact:
                res = wholeMatch;
            }

            return res;
        }

        private string ImageInlineEvaluator(Match match)
        {
            string altText = match.Groups[2].Value;
            string url = match.Groups[3].Value;
            string title = match.Groups[6].Value;
            string res;

            altText = altText.Replace("\"", "&quot;");
            title = title.Replace("\"", "&quot;");
            url = UrlEncoderFixup(url);

            res = string.Format("<img src=\"{0}\" alt=\"{1}\"", url, altText);

            title = title.Replace("*", _escapeTable["*"].ToString()).Replace("_", _escapeTable["_"].ToString());
            res += string.Format(" title=\"{0}\"", title);

            res += _emptyElementSuffix;
            return res;
        }

        // profiler says this one is expensive
        private static Regex _header1 = new Regex(@"^(.+)[ \t]*\n=+[ \t]*\n+",
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        // profiler says this one is expensive
        private static Regex _header2 = new Regex(@"^(.+)[ \t]*\n-+[ \t]*\n+",
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static Regex _header3 = new Regex(@"
                ^(\#{1,6})	# $1 = string of #'s
			    [ \t]*
			    (.+?)		# $2 = Header text
			    [ \t]*
			    \#*			# optional closing #'s (not counted)
			    \n+",
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

        private string DoHeaders(string text)
        {
            /*
            Setext-style headers:
            
            Header 1
            ========
	          
            Header 2
            --------
            */
            text = _header1.Replace(text, new MatchEvaluator(SetextHeader1Evaluator));
            text = _header2.Replace(text, new MatchEvaluator(SetextHeader2Evaluator));

            /*
             atx-style headers:
                # Header 1
                ## Header 2
                ## Header 2 with closing hashes ##
                ...
                ###### Header 6
            */
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
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

        // profiler says this one is expensive
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
            string result = null;

            // Turn double returns into triple returns, so that we can make a
            // paragraph for the last item in a list, if necessary:
            list = Regex.Replace(list, @"\n{2,}", "\n\n\n");
            result = ProcessListItems(list, GetMarkerAnyPattern());
            result = string.Format("<{0}>\n{1}</{0}>\n", listType, result);

            return result;
        }

        /// <summary>
        /// Process the contents of a single ordered or unordered list, splitting it
        /// into individual list items.
        /// </summary>
        private string ProcessListItems(string list, string marker)
        {
            /*
	            The listLevel global keeps track of when we're inside a list.
	            Each time we enter a list, we increment it; when we leave a list,
	            we decrement. If it's zero, we're not in a list anymore.
	        
	            We do this because when we're not inside a list, we want to treat
	            something like this:
	        
	            	I recommend upgrading to version
	            	8. Oops, now this line is treated
	            	as a sub-list.
	        
	            As a single paragraph, despite the fact that the second line starts
	            with a digit-period-space sequence.
	        
	            Whereas when we're inside a list (or sub-list), that line will be
	            treated as the start of a sub-list. What a kludge, huh? This is
	            an aspect of Markdown's syntax that's hard to parse perfectly
	            without resorting to mind-reading. Perhaps the solution is to
	            change the syntax rules such that sub-lists must start with a
	            starting cardinal number; e.g. "1." or "a.".
            */

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
                                            _tabWidth), RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

        private string DoCodeBlocks(string text)
        {
            // TODO: Should we allow 2 empty lines here or only one?
            text = _codeBlock.Replace(text, new MatchEvaluator(CodeBlockEvaluator));
            return text;
        }

        private string CodeBlockEvaluator(Match match)
        {
            string codeBlock = match.Groups[1].Value;
            codeBlock = EncodeCode(Outdent(codeBlock));

            // Trim leading newlines and trailing whitespace
            codeBlock = Regex.Replace(codeBlock, @"^\n+", "");
            codeBlock = Regex.Replace(codeBlock, @"\s+\z", "");

            return string.Concat("\n\n<pre><code>", codeBlock, "\n</code></pre>\n\n");
        }

        private static Regex _codeSpan = new Regex(@"
                    (`+)		# $1 = Opening run of `
			        (.+?)		# $2 = The code block
			        (?<!`)
			        \1
			        (?!`)", RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

        private string DoCodeSpans(string text)
        {
            /*
                *	Backtick quotes are used for <code></code> spans.
                *	You can use multiple backticks as the delimiters if you want to
                    include literal backticks in the code span. So, this input:

                    Just type ``foo `bar` baz`` at the prompt.
        
                    Will translate to:
        
                      <p>Just type <code>foo `bar` baz</code> at the prompt.</p>
        
                    There's no arbitrary limit to the number of backticks you
                    can use as delimters. If you need three consecutive backticks
                    in your code, use four for delimiters, etc.
        
                *	You can use spaces to get literal backticks at the edges:
        
                      ... type `` `bar` `` ...
        
                    Turns to:
        
                      ... type <code>`bar`</code> ...	        
            */

            text = _codeSpan.Replace(text, new MatchEvaluator(CodeSpanEvaluator));

            return text;
        }

        private string CodeSpanEvaluator(Match match)
        {
            string s = match.Groups[2].Value;
            s = s.Replace(@"^[ \t]*", "").Replace(@"[ \t]*$", "");
            s = EncodeCode(s);

            return string.Concat("<code>", s, "</code>");
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
            code = code.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

            foreach (string key in _escapeTable.Keys)
                code = code.Replace(key, _escapeTable[key].ToString());

            return code;
        }


        // profiler says this one is expensive
        private static Regex _strong = new Regex(@"(\s|^)(\*\*|__)(?=\S)([^\r]*?\S[\*_]*)\2(\W|$)",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);
        // profiler says this one is expensive
        private static Regex _italics = new Regex(@"(\s|^)(\*|_)(?=\S)([^\r\*_]*?\S)\2(\W|$)",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);

        private string DoItalicsAndBold(string text)
        {
            // <strong> must go first:
            text = _strong.Replace(text, "$1<strong>$3</strong>$4");
            // Then <em>:
            text = _italics.Replace(text, "$1<em>$3</em>$4");

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
		    )", RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);

        private string DoBlockQuotes(string text)
        {
            text = _blockquote.Replace(text, new MatchEvaluator(BlockQuoteEvaluator));
            return text;
        }

        private string BlockQuoteEvaluator(Match match)
        {
            string bq = match.Groups[1].Value;

            // Trim one level of quoting - trim whitespace-only lines
            bq = Regex.Replace(bq, @"^[ \t]*>[ \t]?", "", RegexOptions.Multiline);
            bq = Regex.Replace(bq, @"^[ \t]+$", "", RegexOptions.Multiline);

            bq = RunBlockGamut(bq);
            bq = Regex.Replace(bq, @"^", "  ", RegexOptions.Multiline);

            // These leading spaces screw with <pre> content, so we need to fix that:
            bq = Regex.Replace(bq, @"(\s*<pre>.+?</pre>)", new MatchEvaluator(BlockQuoteEvaluator2), RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

            return string.Format("<blockquote>\n{0}\n</blockquote>\n\n", bq);
        }

        private string BlockQuoteEvaluator2(Match match)
        {
            string pre = match.Groups[1].Value;
            pre = Regex.Replace(pre, @"^  ", "", RegexOptions.Multiline);

            return pre;
        }

        private static Regex _newlinesLeading = new Regex(@"^\n+");
        private static Regex _newlinesTrailing = new Regex(@"\n+\z");
        private static Regex _newlinesMultiple = new Regex(@"\n{2,}");
        private static Regex _tabsLeading = new Regex(@"^([ \t]*)", RegexOptions.ExplicitCapture);

        private string FormParagraphs(string text)
        {
            // strip leading and trailing lines
            text = _newlinesLeading.Replace(text, "");
            text = _newlinesTrailing.Replace(text, "");

            string[] paragraphs = _newlinesMultiple.Split(text);

            // Wrap <p> tags.
            for (int i = 0; i < paragraphs.Length; i++)
            {
                // Milan Negovan: I'm adding an additional check for an empty block of code.
                // Otherwise an empty <p></p> is created.
                if (_htmlBlocks[paragraphs[i]] == null && paragraphs[i].Length > 0)
                {
                    string block = paragraphs[i];

                    block = RunSpanGamut(block);
                    block = _tabsLeading.Replace(block, "<p>");
                    block += "</p>";

                    paragraphs[i] = block;
                }
            }

            // Unhashify HTML blocks
            for (int i = 0; i < paragraphs.Length; i++)
            {
                string block = (string)_htmlBlocks[paragraphs[i]];

                if (block != null)
                    paragraphs[i] = block;
            }

            return string.Join("\n\n", paragraphs);
        }

        // profiler says this one is expensive
        private static Regex _autolinkBare = new Regex(@"(^|\s)(https?|ftp)(://[-A-Z0-9+&@#/%?=~_|\[\]\(\)!:,\.;]*[-A-Z0-9+&@#/%=~_|\[\]])($|\W)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private string DoAutoLinks(string text)
        {

            // fixup arbitrary URLs by adding Markdown < > so they get linked as well
            // note that at this point, all other URL in the text are already hyperlinked as <a href=""></a>
            // *except* for the <http://www.foo.com> case
            text = _autolinkBare.Replace(text, @"$1<$2$3>$4");

            if (Regex.IsMatch(text, @"<(https?|ftp)://[^>]+>"))
            {
                string newtext;
                foreach (Match m in Regex.Matches(text, @"<(https?|ftp)://[^>]+>"))
                {
                    // fixup arbitrary URLs -- encode problem characters
                    newtext = UrlEncoderFixup(m.Value);
                    text = text.Replace(m.Value, newtext);
                }
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

            /*
                Input: an email address, e.g. "foo@example.com"
            
                Output: the email address as a mailto link, with each character
                        of the address encoded as either a decimal or hex entity, in
                        the hopes of foiling most address harvesting spam bots. E.g.:
            
                  <a href="&#x6D;&#97;&#105;&#108;&#x74;&#111;:&#102;&#111;&#111;&#64;&#101;
                    x&#x61;&#109;&#x70;&#108;&#x65;&#x2E;&#99;&#111;&#109;">&#102;&#111;&#111;
                    &#64;&#101;x&#x61;&#109;&#x70;&#108;&#x65;&#x2E;&#99;&#111;&#109;</a>
            
                Based by a filter by Matthew Wickline, posted to the BBEdit-Talk
                mailing list: <http://tinyurl.com/yu7ue>
            
             */
            email = "mailto:" + email;

            // leave ':' alone (to spot mailto: later) 
            email = Regex.Replace(email, @"([^\:])", new MatchEvaluator(EncodeEmailEvaluator));

            email = string.Format("<a href=\"{0}\">{0}</a>", email);

            // strip the mailto: from the visible part
            email = Regex.Replace(email, "\">.+?:", "\">");
            return email;
        }

        private string EncodeEmailEvaluator(Match match)
        {
            char c = Convert.ToChar(match.Groups[1].Value);

            Random rnd = new Random();
            int r = rnd.Next(0, 100);

            // Original author note:
            // Roughly 10% raw, 45% hex, 45% dec 
            // '@' *must* be encoded. I insist.
            if (r > 90 && c != '@') return c.ToString();
            if (r < 45) return string.Format("&#x{0:x};", (int)c);

            return string.Format("&#x{0:x};", (int)c);
        }


        private static Regex _amps = new Regex(@"&(?!#?[xX]?([0-9a-fA-F]+|\w+);)", RegexOptions.ExplicitCapture);
        private static Regex _angles = new Regex(@"<(?![A-Za-z/?\$!])", RegexOptions.ExplicitCapture);

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

        private string EncodeBackslashEscapes(string value)
        {
            // Must process escaped backslashes first.
            foreach (string key in _backslashEscapeTable.Keys)
                value = value.Replace(key, _backslashEscapeTable[key].ToString());

            return value;
        }

        /// <summary>
        /// Swap back in all the special characters we've hidden. 
        /// </summary>
        private string UnescapeSpecialChars(string text)
        {
            foreach (string key in _escapeTable.Keys)
                text = text.Replace(_escapeTable[key].ToString(), key);

            return text;
        }

        /// <summary>
        /// Remove one level of line-leading tabs or spaces
        /// </summary>
        private string Outdent(string block)
        {
            return Regex.Replace(block, @"^(\t|[ ]{1," + _tabWidth.ToString() + @"})", "", RegexOptions.Multiline);
        }

        // profiler says this one is expensive
        private static Regex _deTab = new Regex(@"^(.*?)\t", RegexOptions.Multiline | RegexOptions.Compiled);

        private string Detab(string text)
        {
            // Inspired from a post by Bart Lateur: 
            // http://www.nntp.perl.org/group/perl.macperl.anyperl/154
            return _deTab.Replace(text, new MatchEvaluator(TabEvaluator));
        }

        private string TabEvaluator(Match match)
        {
            string leading = match.Groups[1].Value;
            return string.Concat(leading, RepeatString(" ", _tabWidth - leading.Length % _tabWidth));
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