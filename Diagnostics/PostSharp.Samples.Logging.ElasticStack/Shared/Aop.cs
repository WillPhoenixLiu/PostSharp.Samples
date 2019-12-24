using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Tag;
using OpenTracing.Util;
using PostSharp.Aspects;
using PostSharp.Extensibility;
using PostSharp.Serialization;

[assembly: Aop.OpenTracingLoggingAspect(AttributeTargetTypes = "null",
    AttributeTargetTypeAttributes = MulticastAttributes.Public,
    AttributeTargetMemberAttributes = MulticastAttributes.Public)]
namespace Aop
{
  [PSerializable]
  public class OpenTracingLoggingAspect : OnMethodBoundaryAspect
  {
    //接由外部 service 傳入的 Header 資料
    public static AsyncLocal<Dictionary<string, string>> TracerHttpHeaders =
        new AsyncLocal<Dictionary<string, string>>();

    public override void OnEntry(MethodExecutionArgs args)
    {
      if (!GlobalTracer.IsRegistered())
        return;
      var operationName = $"{args.Method.Name}.{args.Method.ReflectedType.Name}";


      var tracer = GlobalTracer.Instance;
      var spanBuilder = tracer.BuildSpan(operationName);
      if (tracer.ActiveSpan != null)
      {
        spanBuilder.AsChildOf(tracer.ActiveSpan);
      }
      else if (TracerHttpHeaders.Value != null)
      {
        // check http
        var parentSpanCtx = tracer.Extract(BuiltinFormats.HttpHeaders, new TextMapExtractAdapter(TracerHttpHeaders.Value));
        spanBuilder.AsChildOf(parentSpanCtx);
      }
      var activeScope = spanBuilder.StartActive(true);
      args.MethodExecutionTag = activeScope;
    }

    public override void OnException(MethodExecutionArgs args)
    {
      if (!GlobalTracer.IsRegistered())
        return;
      //args.FlowBehavior = FlowBehavior.ThrowException;
      var operationName = $"{args.Method.Name}.{args.Method.ReflectedType.Name}";
      var activeScope = args.MethodExecutionTag as IScope;
      Tags.Error.Set(activeScope.Span, true);
      activeScope.Span.Log(new Dictionary<string, object> { ["error"] = args.Exception.ToString() });
    }

    public override void OnExit(MethodExecutionArgs args)
    {
      if (!GlobalTracer.IsRegistered())
        return;
      var operationName = $"{args.Method.Name}.{args.Method.ReflectedType.Name}";
      var activeScope = args.MethodExecutionTag as IScope;
      activeScope.Dispose();
      System.Diagnostics.Debug.WriteLine($"[{operationName}]:OnExit");
    }
  }
}

