using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Octostache.Templates;
using Xunit;

namespace Octostache.Tests
{
    public class UsageFixture : BaseFixture
    {
        [Fact]
        public void HowToUseTheDictionary()
        {
            var variables = new VariableDictionary();
            variables.Set("Foo", "Bar");
            variables.Set("IsFamous", "True");
            variables.Set("FriendCount", "99");
            variables.Set("InstallPath", "C:\\#{Directory}");
            variables.Set("Directory", "MyDirectory");

            variables.Get("InstallPath").Should().Be("C:\\MyDirectory");
            variables.GetRaw("InstallPath").Should().Be("C:\\#{Directory}");
            variables.GetFlag("IsFamous").Should().Be(true);
            variables.GetFlag("IsInfamous").Should().Be(false);
            variables.GetFlag("IsInfamous", true).Should().Be(true);
            variables.GetInt32("FriendCount").Should().Be(99);
            variables.GetInt32("FollowerCount").Should().Be(null);
        }

        [Fact]
        public void UseDictionaryWithCollectionInitializer()
        {
            var variables = new VariableDictionary
            {
                {"Foo", "Bar"},
                {"IsFamous", "True"},
                {"FriendCount", "99"},
                {"InstallPath", "C:\\#{Directory}"},
                {"Directory", "MyDirectory"}
            };


            variables.Get("InstallPath").Should().Be("C:\\MyDirectory");
            variables.GetRaw("InstallPath").Should().Be("C:\\#{Directory}");
            variables.GetFlag("IsFamous").Should().Be(true);
            variables.GetFlag("IsInfamous").Should().Be(false);
            variables.GetFlag("IsInfamous", true).Should().Be(true);
            variables.GetInt32("FriendCount").Should().Be(99);
            variables.GetInt32("FollowerCount").Should().Be(null);
        }

        [Theory]
        [InlineData("#{Foo}", "Foo=Bar", "Bar")]
        [InlineData("#{Foo}", "foo=Bar", "Bar")]
        [InlineData("#{Foo}", "Foo=#{Bar};Bar=Baz", "Baz")]
        [InlineData("#{Foo}", "Foo=#{Bar | ToBase64};Bar=Baz", "QmF6")]
        [InlineData("#{Foo}", "Foo=#{Bar | ToBase64 unicode};Bar=Baz", "QgBhAHoA")]
        [InlineData("#{Foo}", "Foo=#{Bar | FromBase64};Bar=QmF6", "Baz")]
        [InlineData("#{Foo}", "Foo=#{Bar | FromBase64 unicode};Bar=QgBhAHoA", "Baz")]
        [InlineData("#{Foo}", "Foo=#{Bar | ToLower};Bar=Baz", "baz")]
        [InlineData("#{Foo Bar Jazz}", "Foo Bar Jazz=Bar", "Bar")]
        [InlineData("#{Foo|ToUpper}", "Foo=#{Bar | ToLower};Bar=Baz", "BAZ")]
        [InlineData("#{Foo | ToUpper}", "Foo=baz", "BAZ")]
        [InlineData("##{Foo}", "foo=Bar", "#{Foo}")]

        public void BasicExamples(string template, string variableDefinitions, string expectedResult)
        {
            string error;
            var result = ParseVariables(variableDefinitions).Evaluate(template, out error);
            result.Should().Be(expectedResult);
            error.Should().BeNull();
        }

        [Theory]
        [InlineData("#{Foo}", "", "#{Foo}")]
        [InlineData("#{Foo}#{Jazz}", "Foo=#{Bar};Bar=Baz", "Baz#{Jazz}")]
        [InlineData("#{each a in Action}#{a.Size}#{/each}", "Action[x].Name=Baz;Action[y].Name=Baz", "#{a.Size}#{a.Size}")
        ]
        public void MissingTokenErrorExamples(string template, string variableDefinitions, string expectedResult)
        {
            string error;
            var result = ParseVariables(variableDefinitions).Evaluate(template, out error);
            result.Should().Be(expectedResult);
            error.Should().NotBeNull();
        }

        [Theory]
        [InlineData("#{Foo", "Foo=Bar;")]
        [InlineData("#{Fo[o}", "Foo=Bar;Fo[o]=Bar")]
        [InlineData("#{each a in Action}", "Action[a]=One")]
        public void ParseErrorExamples(string template, string variableDefinitions)
        {
            string error;
            var result = ParseVariables(variableDefinitions).Evaluate(template, out error);
            result.Should().Be(template);
            error.Should().NotBeNull();
        }

        [Theory]
        [InlineData("#{a/b}", "a/b=Foo", "Foo")]
        [InlineData("#{a~b}", "a~b=Foo", "Foo")]
        [InlineData("#{(abc)}", "(abc)=Foo", "Foo")]
        public void AwkwardCharacters(string template, string variableDefinitions, string expectedResult)
        {
            var result = ParseVariables(variableDefinitions).Evaluate(template);
            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("#{ }", "Foo=Value; =Bar", "#{ }")]
        [InlineData("#{}", "Foo=Value;=Bar", "#{}")]
        public void EmptyValuesAreEchoed(string template, string variableDefinitions, string expectedResult)
        {
            var result = ParseVariables(variableDefinitions).Evaluate(template);
            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("#{Foo}", "Foo=#{Foo}", "Foo -> Foo")]
        [InlineData("#{Foo}", "Foo=#{Fox};Fox=#{Fax};Fax=#{Fix};Fix=#{Foo}", "Foo -> Fox -> Fax -> Fix -> Foo")]
        public void MaximumRecursionLimitException(string template, string variableDefinitions, string expectedChain)
        {
            var ex =
                Assert.Throws<RecursiveDefinitionException>(() => ParseVariables(variableDefinitions).Evaluate(template));
            ex.Message.Should().Be($"An attempt to parse the variable symbol \"Foo\" appears to have resulted in a self referencing loop ({expectedChain}). Ensure that recursive loops do not exist in the variable values.");
        }

        [Fact]
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

            result.Should().Be(match);
        }

        [Fact]
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

            result.Should().Be(match);
        }


        [Fact]
        public void Required()
        {
            var variables = new VariableDictionary();
            variables.Set("FirstName", "Paul");
            variables.Get("FirstName").Should().Be("Paul");
            Assert.Throws<ArgumentOutOfRangeException>(() => variables.Require("LastName"));
        }

        [Fact]
        public void Performance()
        {
            var watch = Stopwatch.StartNew();
            var result = Evaluate("Hello, #{Location}!", new Dictionary<string, string> { { "Location", "World" } });
            result.Should().Be("Hello, World!");

            var iterations = 0;

            while (watch.ElapsedMilliseconds < 5000)
            {
                result = Evaluate("Hello, #{Location}!", new Dictionary<string, string> { { "Location", "World" } });
                result.Should().Be("Hello, World!");
                iterations++;
            }

            Console.WriteLine(iterations);
            iterations.Should().BeGreaterThan(10000);
        }

        [Fact]
        public void NamedIndexersAreSupported()
        {
            var result = Evaluate("#{Octopus.Action[Package A].Name}", new Dictionary<string, string>
            {
                { "Octopus.Action[Package A].Name", "MyPackage" }
            });

            result.Should().Be("MyPackage");
        }

        [Fact]
        public void VariableIndexersAreSupported()
        {
            var result = Evaluate("#{Octopus.Action[#{Package}].Name}", new Dictionary<string, string>
            {
                { "Package", "Package A" },
                { "Octopus.Action[Package A].Name", "MyPackage" },
            });

            result.Should().Be("MyPackage");
        }

        [Fact]
        public void MissingVariableIndexersFailToEvaluateGracefully()
        {
            var result = Evaluate("#{Octopus.Action[#{Package}].Name}", new Dictionary<string, string>
            {
                { "Octopus.Action[Package A].Name", "MyPackage" },
            });

            result.Should().Be("#{Octopus.Action[#{Package}].Name}");
        }

        [Fact]
        public void MissingIndexedVariableWhenTheIndexerIsAValidVariableAndTheVariableNameIsTheSameAsTheIndexedVariableName()
        {
            var result = Evaluate("#{MY_VAR}", new Dictionary<string, string>
            {
                { "MY_VAR", "#{MY_VAR[#{foo}]}" },
                { "foo", "bar" },
            });
            result.Should().Be("#{MY_VAR[#{foo}]}");
        }


        [Fact]
        public void IterationOverAnEmptyCollectionIsFine()
        {
            var result = Evaluate("Ok#{each nothing in missing}#{nothing}#{/each}", new Dictionary<string, string>());

            result.Should().Be("Ok");
        }

        [Fact]
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

            result.Should().Be("AaAbBc");
        }

        [Fact]
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

            result.Should().Be("AB");
        }

        [Fact]
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

            result.Should().Be("Step 2 Details");
        }

        [Fact]
        public void IndexWithUnkownVariableDoesntFail()
        {
            var pattern = "#{Location[#{Continent}]}";

            var variables = new VariableDictionary();
            variables.Evaluate(pattern).Should().Be(pattern);

            variables.Set("Location[Europe]", "Madrid");
            variables.Evaluate(pattern).Should().Be(pattern);

            variables.Set("Continent", "Europe");
            variables.Evaluate(pattern).Should().Be("Madrid");
        }

        [Fact]
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

            result.Should().Be("OctopusOctopus");
        }

        [Fact]
        public void IndexingWithASymbolIsSupported()
        {
            var result = Evaluate("#{Step[#{action.StepName}]}",
                new Dictionary<string, string>
                {
                    {"action.StepName", "Step 1"},
                    {"Step[Step 1]", "Running"},
                });

            result.Should().Be("Running");
        }

        [Fact]
        public void IndexingIsSupportedWithWildcards()
        {
            var result = Evaluate("#{Octopus.Action[*].Name}",
                new Dictionary<string, string>
                {
                    {"Octopus.Action[Package A].Name", "A"},
                    {"Octopus.Action[Package B].Name", "B"}
                });

            result.Should().BeOneOf("A", "B");
        }

        [Fact]
        public void IteratorsResolveToTheIndexedExpression()
        {
            var result = Evaluate("#{each a in Octopus.Action}#{a}|#{/each}",
                new Dictionary<string, string>
                {
                    {"Octopus.Action[Package A].Name", "A"},
                    {"Octopus.Action[Package B].Name", "B"},
                });

            result.Should().Be("Package A|Package B|");
        }

        [Fact]
        public void UnmatchedSubstitutionsAreEchoed()
        {
            var result = Evaluate("#{foo}", new Dictionary<string, string>());
            result.Should().Be("#{foo}");
        }


        [Fact]
        public void DoubleHashEscapesToken()
        {
            var result = Evaluate("##{foo}", new Dictionary<string, string> { { "foo", "Abc" } });
            result.Should().Be("#{foo}");
        }

        [Fact]
        public void TripleHashResolvesToSinglePlusToken()
        {
            var result = Evaluate("###{foo}", new Dictionary<string, string> { { "foo", "Abc" } });
            result.Should().Be("#Abc");
        }

        [Fact]
        public void StandaloneHashesAreText()
        {
            var result = Evaluate("# ## ###", new Dictionary<string, string>());
            result.Should().Be("# ## ###");
        }

        [Fact]
        public void EvenBlockHashesAreText()
        {
            var result = Evaluate("###### hello", new Dictionary<string, string>());
            result.Should().Be("###### hello");
        }

        [Fact]
        public void OddBlockHashesAreText()
        {
            var result = Evaluate("####### world", new Dictionary<string, string>());
            result.Should().Be("####### world");
        }

        [Fact]
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

            url.Should().Be("http://web01:10933");
            raw.Should().Be("http://#{Server | ToLower}:#{Port}");
            eval.Should().Be("http://web01:10933/foo");
            error.Should().NotBeNull();
        }

        [Fact]
        public void ShouldSupportRoundTripping()
        {
            var temp = Path.GetTempFileName();

            var parent = new VariableDictionary();
            parent.Set("Name", "Web01");
            parent.Set("Port", "10933");
            parent.Set("Hello world", "This is a \"string\"!@#$");

            parent.Save(temp);

            var child = new VariableDictionary(temp);
            child["Name"].Should().Be("Web01");
            child["Port"].Should().Be("10933");
            child["Hello world"].Should().Be("This is a \"string\"!@#$");

            // Since this variable dictionary was loaded from disk, setting variables
            // will automatically persist the change
            child["SomeVariable"] = "Hello";

            parent["SomeVariable"].Should().BeNull();

            // If one process calls another (using the same variables file), the parent process should
            // reload its variables once the child finishes.
            parent.Reload();

            parent["SomeVariable"].Should().Be("Hello");

            File.Delete(temp);
        }

        [Fact]
        public void ShouldSaveAsString()
        {
            var variables = new VariableDictionary();
            variables.Set("Name", "Web01");
            variables.Set("Port", "10933");

            variables.SaveAsString().Should().Be("{" + Environment.NewLine +
                            "  \"Name\": \"Web01\"," + Environment.NewLine +
                            "  \"Port\": \"10933\"" + Environment.NewLine +
                            "}");
        }

        [Theory]
        [InlineData("{Sizes: {Small: \"#{Test.Sizes.Large.Price}\", Large: \"15\"}}\", Desc: \"Monkey\", Value: 12}", "#{Test.Sizes.Small.Price}", "#{Test.Sizes.Small.Price}", "Direct inner JSON")]
        [InlineData("#{Test.Something}", "#{Test}", "#{Test.Something}", "Missing replacement")]

        public void VariablesThatResolveToUnresolvableReturnError(string variable, string pattern, string expectedResult, string testName)
        {
            var variables = new VariableDictionary
            {
                ["Test"] = variable
            };

            string err;
            variables.Evaluate(pattern, out err).Should().Be(expectedResult);
            err.Should().Be($"The following tokens were unable to be evaluated: '{expectedResult}'");
        }

        [Fact]
        public void ShouldEvaluateTrueToTrue()
        {
            var result = EvaluateTruthy("#{truthy}",
               new Dictionary<string, string>
               {
                    {"truthy", "true"}
               });

            result.Should().BeTrue();
        }

        [Fact]
        public void ShouldEvaluateFalseToFalse()
        {
            var result = EvaluateTruthy("#{falsey}",
               new Dictionary<string, string>
               {
                    {"falsey", "false"}
               });

            result.Should().BeFalse();
        }

        [Fact]
        public void ShouldEvaluateMissingToFalse()
        {
            var result = EvaluateTruthy("#{missing}",
               new Dictionary<string, string>
               {
                    {"truthy", "true"}
               });

            result.Should().BeFalse();
        }

        [Fact]
        public void ShouldEvaluateExistsToTrue()
        {
            var result = EvaluateTruthy("#{exists}",
               new Dictionary<string, string>
               {
                    {"exists", "exists"}
               });

            result.Should().BeTrue();
        }

        [Fact]
        public void CanGetIndexes()
        {
            var variableDictionary = new VariableDictionary
            {
                ["Octopus.Action[Package A].Name"] = "A",
                ["Octopus.Action[Package B].Name"] = "B",
                ["Octopus.Action[].Name"] = "C",
                ["PackageBName"] = "#{Octopus.Action[Package B].Name}",
            };

            var presentIndexes = variableDictionary.GetIndexes("Octopus.Action");

            presentIndexes.Should().HaveCount(3);
            presentIndexes.Should().Contain("Package A");
            presentIndexes.Should().Contain("Package B");
            presentIndexes.Should().Contain("");

            var absentIndexes = variableDictionary.GetIndexes("Foo.Bar");

            absentIndexes.Should().HaveCount(0);
        }

        [Fact]
        public void EncryptText()
        {
            var certificate = MakeCert();
            if (certificate != null)
            {
                var variableDictionary = new VariableDictionary
                {
                    //TODO: Verify this is how Octopus certificate variables create certificate base64 strings. #{CertificateVariable.Certificate}
                    ["CertificateBase64String"] = Convert.ToBase64String(certificate.RawData),
                    ["Data"] = "Test",
                    ["EncryptedData"] = "#{Data | RSAEncrypt #{CertificateBase64String}}"
                };

                var encryptedString = variableDictionary.Evaluate("#{EncryptedData}");
                encryptedString.Should().NotBeNullOrWhiteSpace();
            }
        }

        static X509Certificate2 MakeCert()
        {
#if NETCOREAPP
            var rsa = RSA.Create();
            var req = new CertificateRequest("cn=test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
#else
            //TODO: could potentially manually create certificate and include it as an embedded resource to use here.
            return null;
#endif

        }
    }
}
