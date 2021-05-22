# Using Application Insights in a .NET 5 Console Application with Dependency Injection

## Description
Explains how to setup Application Insights for .NET 5 Console Application and Dependency Injection.

Demonstrated with a .NET 5 Console application. Example project can also be found on [GitHub](https://github.com/iSatishYadav/net-core-console-nlog-with-di).

> If you already have a Console Application you can directly skip to [setting up Application Insights](#application-insights).  

### 0. Create a new .NET 5 console project

### 1. Add dependency in csproj manually or using NuGet

Install:

- The package [NLog.Extensions.Logging](https://www.nuget.org/packages/NLog.Extensions.Logging)
- The package [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection) - or use another DI library.
- The package [Microsoft.Extensions.Configuration.Json](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Json) (used in Main method `SetBasePath` + `AddJsonFile`)
- Update the NLog package if possible

e.g.

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="2.1.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="2.1.0" />
    <PackageReference Include="NLog" Version="4.6.5" />
    <PackageReference Include="NLog.Extensions.Logging" Version="1.5.1" />
  </ItemGroup>
```

### 2. Create a nlog.config file. 
Create nlog.config (lowercase all) file in the root of your application project (File Properties: Copy Always)

We use this example:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<!-- XSD manual extracted from package NLog.Schema: https://www.nuget.org/packages/NLog.Schema-->
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd" xsi:schemaLocation="NLog NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true"
      internalLogFile="c:\temp\skylogs\nlog-internal.log"
      internalLogLevel="Info" >

  <!-- the targets to write to -->
  <targets>
    <!-- write logs to file -->
    <target xsi:type="File" name="logfile" fileName="c:\temp\skylogs\skylogs.log"
            layout="${longdate}|${level}|${message} |${all-event-properties} ${exception:format=tostring}" />
    <target xsi:type="Console" name="logconsole"
            layout="${longdate}|${level}|${message} |${all-event-properties} ${exception:format=tostring}" />
  </targets>

  <!-- rules to map from logger name to target -->
  <rules>
    <logger name="*" minlevel="Trace" writeTo="logfile,logconsole" />
  </rules>
</nlog>
```

It is recommended to read the [NLog Tutorial](https://github.com/NLog/NLog/wiki/Tutorial). For more detailed information about config file can be found [here](https://github.com/NLog/NLog/wiki/Configuration-file).

If you like to include other targets or layout renderers, check the [Platform support](https://github.com/NLog/NLog/wiki/platform-support).

Ensure to configure your project-file to copy NLog.config to the output directory:

```xml
 <ItemGroup>
     <None Update="nlog.config" CopyToOutputDirectory="Always" />
 </ItemGroup>
```

### 3. Update your program

#### 3.1 Create your `Person` class

```c#
public class Person
{
      public string Name { get; set; }
      private readonly ILogger<Person> _logger;

      public Person(ILogger<Person> logger)
      {
          _logger = logger;
      }

      public void Talk(string text)
      {
          _logger.LogInformation("Person {name} spoke {text}", Name, text);
      }
}
```

#### 3.2 Setup the Dependency injector (DI) container
```c#
private static IServiceProvider BuildDi(IConfiguration config)
{
   return new ServiceCollection()
           //Add DI Classes here
         .AddTransient<Person>() 
         .AddLogging(loggingBuilder =>
         {
             // configure Logging with NLog
             loggingBuilder.ClearProviders();
             loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
             loggingBuilder.AddNLog(config);
         })
         .BuildServiceProvider();
}
```

#### 3.3 Add required usings
```c#
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
```
#### 3.4 Update your `Main`

First create the DI container, then get your `Person` and start running!

```c#
static void Main(string[] args)
{
    var logger = LogManager.GetCurrentClassLogger();
    try
    {
        var config = new ConfigurationBuilder()
           .SetBasePath(System.IO.Directory.GetCurrentDirectory())
           .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
           .Build();

        var servicesProvider = BuildDi(config);

        using (servicesProvider as IDisposable)
        {
            var person = servicesProvider.GetRequiredService<Person>();
            person.Name = "Sky";
            person.Talk("Hello");

            Console.WriteLine("Press ANY key to exit");
            Console.ReadKey();
        }
    }
    catch (Exception ex)
    {
        // NLog: catch any exception and log it.
        logger.Error(ex, "Stopped program because of exception");
        throw;
    }
    finally
    {
        // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
        LogManager.Shutdown();
    }
}
private static IServiceProvider BuildDi(IConfiguration config)
{
    return new ServiceCollection()
         //Add DI Classes here
       .AddTransient<Person>() // Runner is the custom class
       .AddLogging(loggingBuilder =>
       {
           // configure Logging with NLog
           loggingBuilder.ClearProviders();
           loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
           loggingBuilder.AddNLog(config);
       })
       .BuildServiceProvider();
}
```

### 4 Example output

```
2021-05-22 12:33:20.8486|Info|Person Sky spoke Hello |name=Sky, text=Hello 
```


### Configure NLog Targets for output

Next step, see [Configure NLog with nlog.config](https://github.com/NLog/NLog/wiki/Configuration-file)

## Application Insights

### Add Application Insights Package

Install:

- The package [Microsoft.ApplicationInsights.WorkerService](https://www.nuget.org/packages/Microsoft.ApplicationInsights.WorkerService)

e.g.

```xml
  <ItemGroup>    
    <PackageReference Include="Microsoft.ApplicationInsights.WorkerService" Version="2.17.0" />
  </ItemGroup>
```

> This package says `WorkerService` but can be used in plain Console Application as well.

### Update your `Main` to add Application Insights
- Add following line while creating `ServiceCollection` 

````csharp
.AddApplicationInsightsTelemetryWorkerService("YOUR_INSTRUMENTATION_KEY")
````

- The final code looks like this:

````csharp
private static IServiceProvider BuildDi(IConfiguration config)
{
    return new ServiceCollection()
        //Add DI Classes here
        .AddTransient<Person>() 
        .AddLogging(loggingBuilder =>
        {
            // configure Logging with NLog
            loggingBuilder.ClearProviders();
            loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
            loggingBuilder.AddNLog(config);
        })
        .AddApplicationInsightsTelemetryWorkerService("YOUR_INSTRUMENTATION_KEY")
        .BuildServiceProvider();
}
````

### Use `TelemetryClient` to track events or whatever

- Get the `TelemetryClient` instance:

````csharp
var telemetryClient = servicesProvider.GetRequiredService<TelemetryClient>();
````
- Track events
````
    telemetryClient.TrackEvent($"Person {person.Name} spoke");
    telemetryClient.Flush();
    Thread.Sleep(500);
````
> `Flush()` method actually sends the request to Application Insights but it's not synchronous, so calling `Thread.Sleep(500)` to make sure it completes.

- Final `Main` looks something like this:

````csharp
static void Main(string[] args)
{
    var logger = LogManager.GetCurrentClassLogger();
    try
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(System.IO.Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var servicesProvider = BuildDi(config);

        using (servicesProvider as IDisposable)
        {
            var person = servicesProvider.GetRequiredService<Person>();
            var telemetryClient = servicesProvider.GetRequiredService<TelemetryClient>();
            person.Name = "Sky";
            person.Talk("Hello");
            telemetryClient.TrackEvent($"Person {person.Name} spoke");
            telemetryClient.Flush();
            Thread.Sleep(500);
            Console.WriteLine("Press ANY key to exit");
            Console.ReadKey();
        }
    }
    catch (Exception ex)
    {
        // NLog: catch any exception and log it.
        logger.Error(ex, "Stopped program because of exception");
        throw;
    }
    finally
    {
        // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
        LogManager.Shutdown();
    }
}
````
