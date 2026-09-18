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

## Filters

The full list of filters is in the [variable filters documentation](https://octopus.com/docs/projects/variables/variable-filters). A couple of behaviours worth calling out here:

### Truncate

`Truncate <length>` shortens a value and appends an ellipsis. The suffix is configurable via an optional second option, so pass an empty string to truncate without any suffix:

```
#{MyVariable | Truncate 7}            // Octopus...
#{MyVariable | Truncate 7 ""}         // Octopus
#{MyVariable | Truncate 7 " (more)"}  // Octopus (more)
```

The suffix is only appended when the value is actually longer than `<length>`.

### Substring

`Substring <length>` and `Substring <startIndex> <length>` both clamp the length to what remains in the value, so asking for more characters than are available returns the rest of the string rather than failing:

```
#{MyVariable | Substring 100}    // Octopus Deploy
#{MyVariable | Substring 8 100}  // Deploy
```

A `<startIndex>` past the end of the value is still an error, and leaves the expression unevaluated.

### Comparison filters

`LessThan`, `LessThanOrEqual`, `GreaterThan` and `GreaterThanOrEqual` compare a value to a single argument and evaluate to `true` or `false`:

```
#{DaysUntilExpiration | LessThan 31}              // true, when DaysUntilExpiration is 20
#{DaysUntilExpiration | GreaterThanOrEqual 31}    // false, when DaysUntilExpiration is 20
```

Because they are filters, they work anywhere a variable expression does — including as a run condition on its own — and they compose with conditionals and other filters:

```
#{if DaysUntilExpiration | LessThan 31}Expiring soon#{/if}
#{Total | Trim | GreaterThan 100}
```

Both sides are parsed as numbers using the invariant culture, so `.` is always the decimal separator and thousands separators are not accepted. If either side is not a number the expression is left unevaluated, which is not truthy.

## Contributing
🐙 We welcome Pull Requests ❤️🧑‍💻

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
