### MarkdownSharp

Open source C# implementation of [Markdown](https://daringfireball.net/projects/markdown/) processor, as featured on [Stack Overflow](https://stackoverflow.com/).

This port is based heavily on the original Perl 1.0.1 and Perl 1.0.2b8 implementations of Markdown, with bits and pieces of the apparently much better maintained [PHP Markdown](https://michelf.ca/projects/php-markdown/) folded into it. There are a few Stack Overflow specific modifications (which are all configurable, and all off by default). I'd like to ensure that this version stays within shouting distance of the Markdown "specification", such as it is...

Note: this build is kept somewhat up to date for those using it (and maintaining old input => result expectations), but [CommonMark](https://commonmark.org/) implementations are what any new users of markdown should look at. The spec is much more strict and deterministic across all cases.