using System;
using System.Reflection;
using Jaeger;
using Jaeger.Metrics;
using Jaeger.Reporters;
using Jaeger.Samplers;
using Jaeger.Senders;
using Microsoft.Extensions.Logging;
using OpenTracing;
using OpenTracing.Contrib.NetCore.CoreFx;
using OpenTracing.Util;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class JaegerServiceCollectionExtensions
    {
        private static readonly Uri _jaegerUri = new Uri("http://47.96.102.100:14268/api/traces");

        public static IServiceCollection AddJaeger(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddSingleton<ITracer>(serviceProvider =>
            {
                string serviceName = Assembly.GetEntryAssembly().GetName().Name;

                ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

                ISampler sampler = new ConstSampler(sample: true);


                //This will log to a default localhost installation of Jaeger.

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

                return tracer;
            });

            // Prevent endless loops when OpenTracing is tracking HTTP requests to Jaeger.
            services.Configure<HttpHandlerDiagnosticOptions>(options =>
            {
                options.IgnorePatterns.Add(request => _jaegerUri.IsBaseOf(request.RequestUri));
            });

            return services;
        }
    }
}
