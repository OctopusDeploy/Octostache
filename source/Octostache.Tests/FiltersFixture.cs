using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Octostache.Tests
{
    [TestFixture]
    public class FiltersFixture : BaseFixture
    {

        [Test]
        public void UnmatchedSubstitutionsAreEchoedEvenWithFiltering()
        {
            var result = Evaluate("#{foo | ToUpper}", new Dictionary<string, string>());
            Assert.AreEqual("#{foo | ToUpper}", result);
        }

        [Test]
        public void UnknownFiltersAreEchoed()
        {
            var result = Evaluate("#{Foo | ToBazooka}", new Dictionary<string, string> { { "Foo", "Abc" } });
            Assert.AreEqual("#{Foo | ToBazooka}", result);
        }

        [Test]
        public void FiltersAreApplied()
        {
            var result = Evaluate("#{Foo | ToUpper}", new Dictionary<string, string> { { "Foo", "Abc" } });
            Assert.AreEqual("ABC", result);
        }

        [Test]
        public void HtmlIsEscaped()
        {
            var result = Evaluate("#{Foo | HtmlEscape}", new Dictionary<string, string> { { "Foo", "A&'bc" } });
            Assert.AreEqual("A&amp;&apos;bc", result);
        }

        [Test]
        public void XmlIsEscaped()
        {
            var result = Evaluate("#{Foo | XmlEscape}", new Dictionary<string, string> { { "Foo", "A&'bc" } });
            Assert.AreEqual("A&amp;&apos;bc", result);
        }

        [Test]
        public void JsonIsEscaped()
        {
            var result = Evaluate("#{Foo | JsonEscape}", new Dictionary<string, string> { { "Foo", "A&\"bc" } });
            Assert.AreEqual("A&\\\"bc", result);
        }

        [Test]
        public void MarkdownIsProcessed()
        {
            var result = Evaluate("#{Foo | Markdown}", new Dictionary<string, string> { { "Foo", "_yeah!_" } });
            Assert.AreEqual("<p><em>yeah!</em></p>\n", result);
        }

        [Test]
        public void DateIsFormatted()
        {
            var dict = new Dictionary<string, string> {{"Foo", "22/05/2030 09:05:00"}};

            var result = Evaluate("#{Foo | FormatDate HH dd-MMM-yyyy}", dict);
            Assert.AreEqual("09 22-May-2030", result);
        }

        [Test]
        public void FiltersAreAppliedInOrder()
        {
            var result = Evaluate("#{Foo|ToUpper|ToLower}", new Dictionary<string, string> { { "Foo", "Abc" } });
            Assert.AreEqual("abc", result);
        }
    }
}
