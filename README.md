Octostache is the variable substitution syntax for Octopus Deploy. 

Octopus allows you to [define variables](http://g.octopushq.com/DocumentationVariables), which can then be referenced from deployment steps and in scripts using the following syntax:

```
#{MyVariable}
```

This library contains the code for parsing and evaluating these variable expressions. 

Usage is simple: install **Octostache** from NuGet.org, then create a `VariableDictionary`:

```csharp
var variables = new VariableDictionary();
variables.Set("Server", "Web01");
variables.Set("Port", "10933");
variables.Set("Url", "http://#{Server | ToLower}:#{Port}");

var url = variables.Get("Url");               // http://web01:10933
var raw = variables.GetRaw("Url");            // http://#{Server | ToLower}:#{Port}
var eval = variables.Evaluate("#{Url}/foo");  // http://web01:10933/foo
```

More examples can be found in [UsageFixture](https://github.com/OctopusDeploy/Octostache/blob/master/source/Octostache.Tests/UsageFixture.cs). 

## Contributing
We welcome Pull Requests 🐙❤️🧑‍💻

### Code Cleanup
The first stage of our CI/CD pipeline for Octostache runs a ReSharper code cleanup, to keep everything neat and tidy.

Your PR won't be able to pass without ensuring the code is clean. You can do this locally via the [ReSharper CLI tools](https://www.jetbrains.com/help/rider/ReSharper_Command_Line_Tools.html), which is how we enforce it during our builds.

All the formatting settings are committed to `Octostache.sln.DotSettings`, so as long as you don't override these with an `Octostache.sln.DotSettings.User` file, you should be all good.

To get started with code cleanup the easiest way (via `dotnet tool`), get the CodeCleanup tool installed globally (one-time):
```
dotnet tool install -g JetBrains.ReSharper.GlobalTools
```
then execute the cleanup:
```
jb cleanupcode ./source/Octostache.sln
```

We don't try to enforce this through build scripts or pre-commit hooks, it's up to you to run when you need to. If you use the Rider IDE, it seems to apply another opinion or two when running the code cleanup, and might get different results to the CLI approach; we don't recommend cleaning up this way.
