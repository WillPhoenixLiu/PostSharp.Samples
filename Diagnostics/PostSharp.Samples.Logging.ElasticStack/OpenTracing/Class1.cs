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
namespace OpenTracing
{


[assembly: AopTest.MyMI(AttributeTargetTypes = "null",
    AttributeTargetTypeAttributes = MulticastAttributes.Public,
    AttributeTargetMemberAttributes = MulticastAttributes.Public)]

    /*针对整个方法体进行包围调用添加日志和截取异常(类似于spring中的环绕通知)
        * 
        * PostSharp中的MethodInterceptionAspect类是针对整个方法体的截取，
        * 继承于它的特性可以对整个方法体进行控制和日志截取、异步操作等!
        * 这个类里面有一个主要的函数可以重载以实现包围整个方法体截取的作用，
        * 它是OnInvoke(MethodInterceptionArgs args)。意义如下：
        * 在它的内部可以通过base.OnInvoke(args)来调用我们加特性声明的方法执行流程,
        * 通过这个方法我们可以在方法开始调用前做操作，调用之后做操作。
        */
    [Serializable]
  public class MyMIAttribute : MethodInterceptionAspect
  {
    public override void OnInvoke(MethodInterceptionArgs args)
    {
      Arguments arguments = args.Arguments;
      StringBuilder sb = new StringBuilder();
      ParameterInfo[] parameters = args.Method.GetParameters();
      for (int i = 0; arguments != null && i < arguments.Count; i++)
      {
        //进入的参数的值        
        sb.Append(parameters[i].Name + "=" + arguments[i] + "");
      }
      try
      {
        Console.WriteLine("进入{0}方法，参数是:{1}", args.Method.DeclaringType + "." + args.Method.Name, sb.ToString());
        base.OnInvoke(args);
        Console.WriteLine("退出{0}方法，返回结果是:{1}", args.Method.DeclaringType + "." + args.Method.Name, args.ReturnValue);
      }
      catch (Exception ex)
      {
        Console.WriteLine(string.Format("出现异常，此方法异常信息是：{0}", ex.ToString()));
      }
    }
  }

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

