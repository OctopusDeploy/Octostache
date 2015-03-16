using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Octostache.Templates;

namespace Octostache.Tests
{
    [TestFixture]
    public class UsageFixture
    {
        [Test]
        public void HowToUseTheDictionary()
        {
            var variables = new VariableDictionary();
            variables.Set("Foo", "Bar");
            variables.Set("IsFamous", "True");
            variables.Set("FriendCount", "99");
            variables.Set("InstallPath", "C:\\#{Directory}");
            variables.Set("Directory", "MyDirectory");

            Assert.That(variables.Get("InstallPath"), Is.EqualTo("C:\\MyDirectory"));
            Assert.That(variables.GetRaw("InstallPath"), Is.EqualTo("C:\\#{Directory}"));
            Assert.That(variables.GetFlag("IsFamous"), Is.EqualTo(true));
            Assert.That(variables.GetFlag("IsInfamous"), Is.EqualTo(false));
            Assert.That(variables.GetFlag("IsInfamous", true), Is.EqualTo(true));
            Assert.That(variables.GetInt32("FriendCount"), Is.EqualTo(99));
            Assert.That(variables.GetInt32("FollowerCount"), Is.EqualTo(null));
        }

        [Test]
        [TestCase("#{Foo}", "", "#{Foo}")]
        [TestCase("#{Foo}", "Foo=Bar", "Bar")]
        [TestCase("#{Foo}", "foo=Bar", "Bar")]
        [TestCase("#{Foo}", "Foo=#{Bar};Bar=Baz", "Baz")]
        [TestCase("#{Foo}", "Foo=#{Bar | ToLower};Bar=Baz", "baz")]
        [TestCase("#{Foo|ToUpper}", "Foo=#{Bar | ToLower};Bar=Baz", "BAZ")]
        [TestCase("##{Foo}", "foo=Bar", "#{Foo}")]
        public void BasicExamples(string template, string variableDefinitions, string expectedResult)
        {
            var result = ParseVariables(variableDefinitions).Evaluate(template);
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        public void Performance()
        {
            var watch = Stopwatch.StartNew();
            var result = Evaluate("Hello, #{Location}!", new Dictionary<string, string> { { "Location", "World" } });
            Assert.That(result, Is.EqualTo("Hello, World!"));

            var iterations = 0;

            while (watch.ElapsedMilliseconds < 5000)
            {
                result = Evaluate("Hello, #{Location}!", new Dictionary<string, string> {{"Location", "World"}});
                Assert.That(result, Is.EqualTo("Hello, World!"));
                iterations++;
            }

            Console.WriteLine(iterations);
            Assert.That(iterations, Is.GreaterThan(100000));
        }

        [Test]
        public void NamedIndexersAreSupported()
        {
            var result = Evaluate("#{Octopus.Action[Package A].Name}", new Dictionary<string, string>
            {
                { "Octopus.Action[Package A].Name", "Package A" }
            });

            Assert.AreEqual("Package A", result);
        }

        [Test]
        public void IterationOverAnEmptyCollectionIsFine()
        {
            var result = Evaluate("Ok#{each nothing in missing}#{nothing}#{/each}", new Dictionary<string, string>());

            Assert.AreEqual("Ok", result);
        }

        [Test]
        public void NestedIterationIsSupported()
        {
            var result = Evaluate("#{each a in Octopus.Action}#{each tr in a.TargetRoles}#{a.Name}#{tr}#{/each}#{/each}",
                new Dictionary<string, string>
                {
                    { "Octopus.Action[Package A].Name", "A" },
                    { "Octopus.Action[Package A].TargetRoles", "a,b" },
                    { "Octopus.Action[Package B].Name", "B" },
                    { "Octopus.Action[Package B].TargetRoles", "c" }
                });

            Assert.AreEqual("AaAbBc", result);
        }

        [Test]
        public void IndexingIsSupportedWithWildcards()
        {
            var result = Evaluate("#{Octopus.Action[*].Name}",
                new Dictionary<string, string>
                {
                    { "Octopus.Action[Package A].Name", "A" },
                    { "Octopus.Action[Package B].Name", "B" }
                });

            Assert.That(result == "A" || result == "B");
        }

        [Test]
        public void IteratorsResolveToTheIndexedExpression()
        {
            var result = Evaluate("#{each a in Octopus.Action}#{a}|#{/each}",
                new Dictionary<string, string>
                {
                    { "Octopus.Action[Package A].Name", "A" },
                    { "Octopus.Action[Package B].Name", "B" },
                });

            Assert.AreEqual("Package A|Package B|", result);
        }

        [Test]
        public void UnmatchedSubstitutionsAreEchoed()
        {
            var result = Evaluate("#{foo}", new Dictionary<string, string>());
            Assert.AreEqual("#{foo}", result);
        }

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
        public void FiltersAreAppliedInOrder()
        {
            var result = Evaluate("#{Foo|ToUpper|ToLower}", new Dictionary<string, string> { { "Foo", "Abc" } });
            Assert.AreEqual("abc", result);
        }

        [Test]
        public void DoubleHashEscapesToken()
        {
            var result = Evaluate("##{foo}", new Dictionary<string, string> { { "foo", "Abc" } });
            Assert.AreEqual("#{foo}", result);
        }

        [Test]
        public void TripleHashResolvesToSinglePlusToken()
        {
            var result = Evaluate("###{foo}", new Dictionary<string, string> { { "foo", "Abc" } });
            Assert.AreEqual("#Abc", result);
        }

        [Test]
        public void StandaloneHashesAreText()
        {
            var result = Evaluate("# ## ###", new Dictionary<string, string>());
            Assert.AreEqual("# ## ###", result);
        }

        [Test]
        public void EvenBlockHashesAreText()
        {
            var result = Evaluate("###### hello", new Dictionary<string, string>());
            Assert.AreEqual("###### hello", result);
        }

        [Test]
        public void OddBlockHashesAreText()
        {
            var result = Evaluate("####### world", new Dictionary<string, string>());
            Assert.AreEqual("####### world", result);
        }

        static string Evaluate(string template, IDictionary<string, string> variables)
        {
            var dictionary = new VariableDictionary(variables);
            return dictionary.Evaluate(template);
        }

        private static VariableDictionary ParseVariables(string variableDefinitions)
        {
            var variables = new Dictionary<string, string>();

            var items = variableDefinitions.Split(';');
            foreach (var item in items)
            {
                var pair = item.Split('=');
                var key = pair.First();
                var value = pair.Last();
                variables[key] = value;
            }

            return new VariableDictionary(variables);
        }
    }
}
