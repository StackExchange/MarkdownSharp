using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit.Abstractions;

namespace MarkdownSharpTests
{
    public class BaseTest
    {
        protected static readonly Assembly _assembly = typeof(BaseTest).Assembly;
        protected ITestOutputHelper Output { get; }

        public BaseTest(ITestOutputHelper output) => Output = output;

        protected static string GetResourceFileContent(string filename)
        {
            using (Stream stream = _assembly.GetManifestResourceStream(filename))
            {
                if (stream == null)
                    return null;

                using (StreamReader streamReader = new StreamReader(stream))
                    return streamReader.ReadToEnd();
            }
        }

        public static IEnumerable<object[]> GetTests(string folder)
        {
            string namespacePrefix = _assembly.GetName().Name + '.' + folder;
            foreach (var resourceName in _assembly.GetManifestResourceNames())
            {
                if (resourceName.StartsWith(namespacePrefix) && resourceName.EndsWith(".html"))
                {
                    yield return new[] { Path.ChangeExtension(resourceName, "text"), resourceName };
                }
            }
        }
    }
}