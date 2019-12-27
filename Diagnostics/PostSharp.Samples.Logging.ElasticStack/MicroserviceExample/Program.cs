﻿//using MicroserviceExample.Formatters;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using PostSharp.Patterns.Diagnostics;
using PostSharp.Patterns.Diagnostics.Backends.Serilog;
using PostSharp.Patterns.Diagnostics.RecordBuilders;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using System;
using Microsoft.Extensions.DependencyInjection;

[assembly: Log]

namespace MicroserviceExample
{
  public class Program
  {


    public static void Main(string[] args)
    {

      using (var logger = new LoggerConfiguration()
          .Enrich.WithProperty("Application", "PostSharp.Samples.Logging.ElasticStack.MicroserviceExample")
          .MinimumLevel.Debug()
          .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
          {
            BatchPostingLimit = 1, // For demo.
            AutoRegisterTemplate = true,
            AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv6,
            EmitEventFailure = EmitEventFailureHandling.ThrowException | EmitEventFailureHandling.WriteToSelfLog,
            FailureCallback = e => Console.WriteLine("Unable to submit event " + e.MessageTemplate),
          })
          .WriteTo.Console(
              outputTemplate:
              "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Indent:l}{Message:l}{NewLine}{Exception}")
          .CreateLogger())
      {


        var backend = new SerilogLoggingBackend(logger);
        backend.Options.IncludeActivityExecutionTime = true;
        backend.Options.IncludeExceptionDetails = true;
        backend.Options.SemanticParametersTreatedSemantically = SemanticParameterKind.All;
        backend.Options.IncludedSpecialProperties = SerilogSpecialProperties.All;
        backend.Options.ContextIdGenerationStrategy = ContextIdGenerationStrategy.Hierarchical;
        LoggingServices.DefaultBackend = backend;

        //LoggingServices.Formatters.Register(typeof(ActionResult<>), typeof(ActionResultFormatter<>));
        //LoggingServices.Formatters.Register(new ActionResultFormatter());
        //LoggingServices.Formatters.Register(new ObjectResultFormatter());

        // Log only warnings by default, except for 10% randomly chosen requests.
        //SampledLoggingActionFilter.Initialize(backend);
        //LoggingServices.DefaultBackend.DefaultVerbosity.SetMinimalLevel(LogLevel.Warning);




        // Execute the web app.
        CreateWebHostBuilder(args).Build().Run();

      }
    }

    public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
            .UseSerilog()
            .UseStartup<Startup>()
            .ConfigureServices(services =>
            {
              // Registers and starts Jaeger (see Shared.JaegerServiceCollectionExtensions)
              services.AddJaeger();

              // Enables OpenTracing instrumentation for ASP.NET Core, CoreFx, EF Core
              services.AddOpenTracing();
            });
  }

}
