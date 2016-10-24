using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Octostache.Tests
{
    [TestFixture]
    public class UsageFixture : BaseFixture
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
            string error;
            var result = ParseVariables(variableDefinitions).Evaluate(template, out error);
            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.IsNull(error);
        }

        [Test]
        [TestCase("#{Foo}", "", "#{Foo}")]
        [TestCase("#{Foo}#{Jazz}", "Foo=#{Bar};Bar=Baz", "Baz#{Jazz}")]
        [TestCase("#{each a in Action}#{a.Size}#{/each}", "Action[x].Name=Baz;Action[y].Name=Baz", "#{a.Size}#{a.Size}")
        ]
        public void MissingTokenErrorExamples(string template, string variableDefinitions, string expectedResult)
        {
            string error;
            var result = ParseVariables(variableDefinitions).Evaluate(template, out error);
            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.IsNotNull(error);
        }

        [TestCase("#{Foo", "Foo=Bar;")]
        [TestCase("#{Fo[o}", "Foo=Bar;Fo[o]=Bar")]
        [TestCase("#{each a in Action}", "Action[a]=One")]
        public void ParseErrorExamples(string template, string variableDefinitions)
        {
            string error;
            var result = ParseVariables(variableDefinitions).Evaluate(template, out error);
            Assert.That(result, Is.EqualTo(template));
            Assert.IsNotNull(error);
        }

        [Test]
        [TestCase("#{a/b}", "a/b=Foo", "Foo")]
        [TestCase("#{a~b}", "a~b=Foo", "Foo")]
        [TestCase("#{(abc)}", "(abc)=Foo", "Foo")]
        public void AwkwardCharacters(string template, string variableDefinitions, string expectedResult)
        {
            var result = ParseVariables(variableDefinitions).Evaluate(template);
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase("#{ }", "Foo=Value; =Bar", "#{ }")]
        [TestCase("#{}", "Foo=Value;=Bar", "#{}")]
        public void EmptyValuesAreEchoed(string template, string variableDefinitions, string expectedResult)
        {
            var result = ParseVariables(variableDefinitions).Evaluate(template);
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase("#{Foo}", "Foo=#{Foo}")]
        [TestCase("#{Foo}", "Foo=#{Fox};Fox=#{Fax};Fax=#{Fix};Fix=#{Foo}")]
        public void MaximumRecursionLimitException(string template, string variableDefinitions)
        {
            var ex =
                Assert.Throws<InvalidOperationException>(() => ParseVariables(variableDefinitions).Evaluate(template));
            Assert.That(ex.Message, Does.Contain("appears to have resulted in a self referencing loop"));
        }


        [Test]
        public void AbleToContinueWhenAParsingErrorIsHit()
        {
            const string input =
                @"webpackJsonp([5],{139:function(e,n,o){""use strict"";(function(e){var o=""dev"",t=e?""prod"":o;n.ENV_MODE=t;var s=t===o;n.DEVELOPMENT_MODE=s;var
                  r=""application/json; charset=utf-8"",a=function(e){return""#{""===e.substr(0,2)?void 0:e},i=""#{Web.Root}"";console.log(i);var l=""#{Wsf.Host}""
                  ;console.log(l);var c=""#{Web.Site.Url[A]}"",u=a(c)||""http://localhost.com/apps/api"";n.APP_API_URL=u,n.getHeaders=function(e){return e||(e=new Headers),
                  s&&(e.append(""SSO-LOGONID"",""username""),e.append(""X-Delta-ClientCode"",""UBS_YK""),e.append(""Accept"",""application/json"")),
                  e.append(""Content-Type"",r),e}}).call(n,o(91))},241:function(e,n,o){""use strict"";o(139),o(262)},259:function(e,n,o){""use strict"";o(241)},262:
                  function(e,n,o){""use strict"";var t=o(139),s=function(e){var n=function(e){e.withCredentials=!0;var n=t.getHeaders(new Headers);
                  n.forEach(function(n,o){e.setRequestHeader(o,n)})";

            var result = Evaluate(input, new Dictionary<string, string>
            {
                {"Web.Site.Url[A]", "Subbed.Web.Site.A"},
                {"Web.Root", "Subbed.Web.Root"},
                {"Wsf.Host", "Subbed.Wsf.Host"}
            }, haltOnError: false);

            const string match =
                @"webpackJsonp([5],{139:function(e,n,o){""use strict"";(function(e){var o=""dev"",t=e?""prod"":o;n.ENV_MODE=t;var s=t===o;n.DEVELOPMENT_MODE=s;var
                  r=""application/json; charset=utf-8"",a=function(e){return""#{""===e.substr(0,2)?void 0:e},i=""Subbed.Web.Root"";console.log(i);var l=""Subbed.Wsf.Host""
                  ;console.log(l);var c=""Subbed.Web.Site.A"",u=a(c)||""http://localhost.com/apps/api"";n.APP_API_URL=u,n.getHeaders=function(e){return e||(e=new Headers),
                  s&&(e.append(""SSO-LOGONID"",""username""),e.append(""X-Delta-ClientCode"",""UBS_YK""),e.append(""Accept"",""application/json"")),
                  e.append(""Content-Type"",r),e}}).call(n,o(91))},241:function(e,n,o){""use strict"";o(139),o(262)},259:function(e,n,o){""use strict"";o(241)},262:
                  function(e,n,o){""use strict"";var t=o(139),s=function(e){var n=function(e){e.withCredentials=!0;var n=t.getHeaders(new Headers);
                  n.forEach(function(n,o){e.setRequestHeader(o,n)})";

            Assert.AreEqual(match, result);
        }

        [Test]
        public void AbleToContinueWhenAParsingErrorIsHitAtEndOfString()
        {
            const string input =
                @"i=""#{Web.Root}"";console.log(i);var l=""#{Wsf.Host}""
                  ;a=function(e){return""#{";

            var result = Evaluate(input, new Dictionary<string, string>
            {
                {"Web.Site.Url[A]", "Subbed.Web.Site.A"},
                {"Web.Root", "Subbed.Web.Root"},
                {"Wsf.Host", "Subbed.Wsf.Host"}
            }, haltOnError: false);

            const string match =
                @"i=""Subbed.Web.Root"";console.log(i);var l=""Subbed.Wsf.Host""
                  ;a=function(e){return""#{";

            Assert.AreEqual(match, result);
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
            var result = Evaluate("Hello, #{Location}!", new Dictionary<string, string> {{"Location", "World"}});
            Assert.That(result, Is.EqualTo("Hello, World!"));

            var iterations = 0;

            while (watch.ElapsedMilliseconds < 5000)
            {
                result = Evaluate("Hello, #{Location}!", new Dictionary<string, string> { { "Location", "World" } });
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
                {"Octopus.Action[Package A].Name", "Package A"}
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
            var result = Evaluate(
                "#{each a in Octopus.Action}#{each tr in a.TargetRoles}#{a.Name}#{tr}#{/each}#{/each}",
                new Dictionary<string, string>
                {
                    {"Octopus.Action[Package A].Name", "A"},
                    {"Octopus.Action[Package A].TargetRoles", "a,b"},
                    {"Octopus.Action[Package B].Name", "B"},
                    {"Octopus.Action[Package B].TargetRoles", "c"}
                });

            Assert.AreEqual("AaAbBc", result);
        }

        [Test]
        public void RecursiveIterationIsSupported()
        {
            var result = Evaluate("#{each a in Octopus.Action}#{a.Name}#{/each}",
                new Dictionary<string, string>
                {
                    {"PackageA_Name", "A"},
                    {"PackageB_Name", "B"},
                    {"Octopus.Action[Package A].Name", "#{PackageA_Name}"},
                    {"Octopus.Action[Package B].Name", "#{PackageB_Name}"},
                });

            Assert.AreEqual("AB", result);
        }

        [Test]
        public void ScopedSymbolIndexerInIterationIsSupported()
        {
            var result =
                Evaluate(
                    "#{each action in Octopus.Action}#{if Octopus.Step[#{action.StepName}].Status != \"Skipped\"}#{Octopus.Step[#{action.StepName}].Details}#{/if}#{/each}",
                    new Dictionary<string, string>
                    {
                        {"Octopus.Action[Action 1].StepName", "Step 1"},
                        {"Octopus.Action[Action 2].StepName", "Step 2"},
                        {"Octopus.Step[Step 1].Details", "Step 1 Details"},
                        {"Octopus.Step[Step 2].Details", "Step 2 Details"},
                        {"Octopus.Step[Step 1].Status", "Skipped"},
                        {"Octopus.Step[Step 2].Status", "Running"},
                    });

            Assert.AreEqual("Step 2 Details", result);
        }

        [Test]
        public void IndexWithUnkownVariableDoesntFail()
        {
            var pattern = "#{Location[#{Continent}]}";

            var variables = new VariableDictionary();            
            Assert.AreEqual(pattern, variables.Evaluate(pattern));
            
            variables.Set("Location[Europe]", "Madrid");
            Assert.AreEqual(pattern, variables.Evaluate(pattern));
         
            variables.Set("Continent", "Europe");
            Assert.AreEqual("Madrid", variables.Evaluate(pattern));
        }

        [Test]
        public void UnscopedIndexerInIterationIsSupported()
        {
            var result =
                Evaluate(
                    "#{each action in Octopus.Action}#{if Octopus.Step[#{SomeOtherVariable}].Status == \"Skipped\"}#{Octopus.Step[#{SomeOtherVariable}].Details}#{/if}#{/each}",
                    new Dictionary<string, string>
                    {
                        {"Octopus.Action[Action 1].StepName", "Step 1"},
                        {"Octopus.Action[Action 2].StepName", "Step 2"},
                        {"Octopus.Step[OtherVariableValue].Details", "Octopus"},
                        {"Octopus.Step[OtherVariableValue].Status", "Skipped"},
                        {"SomeOtherVariable", "OtherVariableValue"}
                    });

            Assert.AreEqual("OctopusOctopus", result);
        }

        [Test]
        public void IndexingWithASymbolIsSupported()
        {
            var result = Evaluate("#{Step[#{action.StepName}]}",
                new Dictionary<string, string>
                {
                    {"action.StepName", "Step 1"},
                    {"Step[Step 1]", "Running"},
                });

            Assert.AreEqual("Running", result);
        }

        [Test]
        public void IndexingIsSupportedWithWildcards()
        {
            var result = Evaluate("#{Octopus.Action[*].Name}",
                new Dictionary<string, string>
                {
                    {"Octopus.Action[Package A].Name", "A"},
                    {"Octopus.Action[Package B].Name", "B"}
                });

            Assert.That(result == "A" || result == "B");
        }

        [Test]
        public void IteratorsResolveToTheIndexedExpression()
        {
            var result = Evaluate("#{each a in Octopus.Action}#{a}|#{/each}",
                new Dictionary<string, string>
                {
                    {"Octopus.Action[Package A].Name", "A"},
                    {"Octopus.Action[Package B].Name", "B"},
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
        public void DoubleHashEscapesToken()
        {
            var result = Evaluate("##{foo}", new Dictionary<string, string> {{"foo", "Abc"}});
            Assert.AreEqual("#{foo}", result);
        }

        [Test]
        public void TripleHashResolvesToSinglePlusToken()
        {
            var result = Evaluate("###{foo}", new Dictionary<string, string> {{"foo", "Abc"}});
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
            variables.Set("Protocol", "#{Proto}");

            var url = variables.Get("Url");
            var raw = variables.GetRaw("Url");
            var eval = variables.Evaluate("#{Url}/foo");

            string error;
            variables.Get("Protocol", out error);

            Assert.AreEqual("http://web01:10933", url);
            Assert.AreEqual("http://#{Server | ToLower}:#{Port}", raw);
            Assert.AreEqual("http://web01:10933/foo", eval);
            Assert.IsNotNull(error);
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

            Assert.AreEqual("{" + Environment.NewLine +
                            "  \"Name\": \"Web01\"," + Environment.NewLine +
                            "  \"Port\": \"10933\"" + Environment.NewLine +
                            "}", variables.SaveAsString());
        }
    }
}
