[![Build status](https://ci.appveyor.com/api/projects/status/g4975pdrvf4cubr0?svg=true)](https://ci.appveyor.com/project/OctopusDeploy/octostache)

Octostache is the variable substitution syntax for Octopus Deploy. 

Octopus allows you to [define variables](http://docs.octopusdeploy.com/display/OD/Variables), which can then be referenced from deployment steps and in scripts using the following syntax:

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

