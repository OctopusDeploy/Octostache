using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;

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
        [TestCase("#{Foo Bar Jazz}", "Foo Bar Jazz=Bar", "Bar")]
        [TestCase("#{Foo|ToUpper}", "Foo=#{Bar | ToLower};Bar=Baz", "BAZ")]
        [TestCase("#{Foo | ToUpper}", "Foo=baz", "BAZ")]
        [TestCase("##{Foo}", "foo=Bar", "#{Foo}")]
        public void BasicExamples(string template, string variableDefinitions, string expectedResult)
        {
            var result = ParseVariables(variableDefinitions).Evaluate(template);
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase("#{a/b}", "a/b=Foo", "Foo")]
        [TestCase("#{a~b}", "a~b=Foo", "Foo")]
        public void AwkwardCharacters(string template, string variableDefinitions, string expectedResult)
        {
            var result = ParseVariables(variableDefinitions).Evaluate(template);
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase("#{ }", "Foo=Value; =Bar", "#{ }")]
        [TestCase("#{}","Foo=Value;=Bar", "#{}")]
        public void EmptyValuesAreEchoed(string template, string variableDefinitions, string expectedResult)
        {
            var result = ParseVariables(variableDefinitions).Evaluate(template);
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        public void Required()
        {
            var variables = new VariableDictionary();
            variables.Set("FirstName", "Paul");
            Assert.That(variables.Get("FirstName"), Is.EqualTo("Paul"));
            Assert.Throws<ArgumentOutOfRangeException>(() => variables.Require("LastName"));
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
            Assert.That(iterations, Is.GreaterThan(10000));
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
        public void RecursiveIterationIsSupported()
        {
            var result = Evaluate("#{each a in Octopus.Action}#{a.Name}#{/each}",
                new Dictionary<string, string>
                {
                    { "PackageA_Name", "A" },
                    { "PackageB_Name", "B" },
                    { "Octopus.Action[Package A].Name", "#{PackageA_Name}" },
                    { "Octopus.Action[Package B].Name", "#{PackageB_Name}" },
                });

            Assert.AreEqual("AB", result);
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

        [Test]
        public void DocumentationIntroduction()
        {
            var variables = new VariableDictionary();
            variables.Set("Server", "Web01");
            variables.Set("Port", "10933");
            variables.Set("Url", "http://#{Server | ToLower}:#{Port}");

            var url = variables.Get("Url");
            var raw = variables.GetRaw("Url");
            var eval = variables.Evaluate("#{Url}/foo");

            Assert.AreEqual("http://web01:10933", url);
            Assert.AreEqual("http://#{Server | ToLower}:#{Port}", raw);
            Assert.AreEqual("http://web01:10933/foo", eval);
        }

        [Test]
        public void ShouldSupportRoundTripping()
        {
            var temp = Path.GetTempFileName();

            var parent = new VariableDictionary();
            parent.Set("Name", "Web01");
            parent.Set("Port", "10933");
            parent.Set("Hello world", "This is a \"string\"!@#$");

            parent.Save(temp);

            var child = new VariableDictionary(temp);
            Assert.That(child["Name"], Is.EqualTo("Web01"));
            Assert.That(child["Port"], Is.EqualTo("10933"));
            Assert.That(child["Hello world"], Is.EqualTo("This is a \"string\"!@#$"));

            // Since this variable dictionary was loaded from disk, setting variables
            // will automatically persist the change
            child["SomeVariable"] = "Hello";

            Assert.That(parent["SomeVariable"], Is.Null);

            // If one process calls another (using the same variables file), the parent process should 
            // reload its variables once the child finishes.
            parent.Reload();

            Assert.That(parent["SomeVariable"], Is.EqualTo("Hello"));

            File.Delete(temp);
        }

        [Test]
        public void ShouldSaveAsString()
        {
            var variables = new VariableDictionary();
            variables.Set("Name", "Web01");
            variables.Set("Port", "10933");

            Assert.AreEqual("{\r\n  \"Name\": \"Web01\",\r\n  \"Port\": \"10933\"\r\n}", variables.SaveAsString());
        }

        static string Evaluate(string template, IDictionary<string, string> variables)
        {
            var dictionary = new VariableDictionary();
            foreach (var pair in variables)
            {
                dictionary[pair.Key] = pair.Value;
            }
            return dictionary.Evaluate(template);
        }

        private static VariableDictionary ParseVariables(string variableDefinitions)
        {
            var variables = new VariableDictionary();

            var items = variableDefinitions.Split(';');
            foreach (var item in items)
            {
                var pair = item.Split('=');
                var key = pair.First();
                var value = pair.Last();
                variables[key] = value;
            }

            return variables;
        }
    }
}
