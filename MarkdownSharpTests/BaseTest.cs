using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.IO;

using NUnit.Framework;

namespace MarkdownSharpTests
{
    public class BaseTest
    {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(BaseTest).FullName);

        static BaseTest()
        {
            log4net.Config.XmlConfigurator.Configure();
            log.Debug("Logging configured");
        }

        [TestFixtureSetUp]
        public void SetUp()
        {
            log.InfoFormat("{0} - Tests starting", GetType().Name);
        }

        [TestFixtureTearDown, DebuggerStepThrough]
        public void TearDown()
        {
            log.InfoFormat("{0} - Tests complete", GetType().Name);
        }

        public string FileContents(string filename)
        {
            string path;
            //string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            throw new Exception("you must edit the path variable in FileContents() before running tests");
            //path = @"c:\svn3\MarkdownSharpTests\";
            string file = Path.Combine(path, filename);
            return File.ReadAllText(file);
        }

    }
}
