using PostSharp.Patterns.Diagnostics;
using PostSharp.Patterns.Diagnostics.Backends.Serilog;
using PostSharp.Patterns.Diagnostics.RecordBuilders;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using System;
using System.Threading.Tasks;
using static PostSharp.Patterns.Diagnostics.FormattedMessageBuilder;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Jaeger.Samplers;
using Jaeger.Senders;
using Jaeger.Reporters;
using Jaeger;
using OpenTracing;
using OpenTracing.Util;

[assembly: Log]

namespace ClientExample
{
  public static class Program
  {
    private static readonly LogSource logSource = LogSource.Get();

    private static async Task Main()
    {
      var serviceProvider = new ServiceCollection()
        .AddLogging()
        .AddOpenTracing()
            .BuildServiceProvider();
      serviceProvider
    .GetService<ILoggerFactory>();

      string serviceName = Assembly.GetEntryAssembly().GetName().Name;

      ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

      ISampler sampler = new ConstSampler(sample: true);


      //This will log to a default localhost installation of Jaeger.
      Uri _jaegerUri = new Uri("http://47.96.102.100:14268/api/traces");
      var sender = new HttpSender(_jaegerUri.AbsoluteUri);

      var reporter = new RemoteReporter.Builder()
              .WithLoggerFactory(loggerFactory) // optional, defaults to no logging
                                                //.WithMaxQueueSize(...)            // optional, defaults to 100
                                                //.WithFlushInterval(...)           // optional, defaults to TimeSpan.FromSeconds(1)
              .WithSender(sender)                  // optional, defaults to UdpSender("localhost", 6831, 0)
              .Build();


      ITracer tracer = new Tracer.Builder(serviceName)
          .WithSampler(sampler)
          .WithReporter(reporter)
          .Build();

      GlobalTracer.Register(tracer);

      using (var logger = new LoggerConfiguration()
          .Enrich.WithProperty("Application", "PostSharp.Samples.Logging.ElasticStack.ClientExample")
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


        using (logSource.Debug.OpenActivity(Formatted("Running the client"), 
          new OpenActivityOptions {  Properties = new[] { new LoggingProperty("User", "Gaius Julius Caesar") {IsBaggage = true} } }))
        {
          await QueueProcessor.ProcessQueue(".\\My\\Queue");
        }
      }

      Console.WriteLine("Done!");
    }
  }
}