// Vendored shim replacing the LibLog single-file facade with a thin layer over
// Microsoft.Extensions.Logging.Abstractions. Phase 3 of the modernization.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RawRabbit.Enrichers.RetryLater")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RawRabbit.Operations.Get")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RawRabbit.Operations.MessageSequence")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RawRabbit.Operations.Publish")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RawRabbit.Operations.Request")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RawRabbit.Operations.Respond")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RawRabbit.Operations.StateMachine")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RawRabbit.Operations.Subscribe")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RawRabbit.Operations.Tools")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RawRabbit.Enrichers.GlobalExecutionId")]

namespace RawRabbit.Logging
{
    public interface ILog : ILogger { }

    public static class LogProvider
    {
        public static ILoggerFactory LoggerFactory { get; set; } = NullLoggerFactory.Instance;

        public static ILog For<T>() => new LogWrapper(LoggerFactory.CreateLogger<T>());
    }

    internal sealed class LogWrapper(ILogger inner) : ILog
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
            => inner.BeginScope(state);

        public bool IsEnabled(LogLevel level) => inner.IsEnabled(level);

        public void Log<TState>(LogLevel level, EventId eventId, TState state,
                                Exception exception,
                                Func<TState, Exception, string> formatter)
            => inner.Log(level, eventId, state, exception, formatter);
    }

    public static class LogExtensions
    {
        public static void Info (this ILog l, string m, params object[] a) => l.LogInformation(m, a);
        public static void Debug(this ILog l, string m, params object[] a) => l.LogDebug      (m, a);
        public static void Warn (this ILog l, string m, params object[] a) => l.LogWarning    (m, a);
        public static void Error(this ILog l, string m, params object[] a) => l.LogError      (m, a);
        public static void Trace(this ILog l, string m, params object[] a) => l.LogTrace      (m, a);
        public static void Fatal(this ILog l, string m, params object[] a) => l.LogCritical   (m, a);

        public static void Info (this ILog l, Exception ex, string m, params object[] a) => l.LogInformation(ex, m, a);
        public static void Debug(this ILog l, Exception ex, string m, params object[] a) => l.LogDebug      (ex, m, a);
        public static void Warn (this ILog l, Exception ex, string m, params object[] a) => l.LogWarning    (ex, m, a);
        public static void Error(this ILog l, Exception ex, string m, params object[] a) => l.LogError      (ex, m, a);
        public static void Trace(this ILog l, Exception ex, string m, params object[] a) => l.LogTrace      (ex, m, a);
        public static void Fatal(this ILog l, Exception ex, string m, params object[] a) => l.LogCritical   (ex, m, a);

        public static void InfoException (this ILog l, string m, Exception ex, params object[] a) => l.LogInformation(ex, m, a);
        public static void DebugException(this ILog l, string m, Exception ex, params object[] a) => l.LogDebug      (ex, m, a);
        public static void WarnException (this ILog l, string m, Exception ex, params object[] a) => l.LogWarning    (ex, m, a);
        public static void ErrorException(this ILog l, string m, Exception ex, params object[] a) => l.LogError      (ex, m, a);
        public static void TraceException(this ILog l, string m, Exception ex, params object[] a) => l.LogTrace      (ex, m, a);
        public static void FatalException(this ILog l, string m, Exception ex, params object[] a) => l.LogCritical   (ex, m, a);

        public static bool IsInfoEnabled (this ILog l) => l.IsEnabled(LogLevel.Information);
        public static bool IsDebugEnabled(this ILog l) => l.IsEnabled(LogLevel.Debug);
        public static bool IsWarnEnabled (this ILog l) => l.IsEnabled(LogLevel.Warning);
        public static bool IsErrorEnabled(this ILog l) => l.IsEnabled(LogLevel.Error);
        public static bool IsTraceEnabled(this ILog l) => l.IsEnabled(LogLevel.Trace);
        public static bool IsFatalEnabled(this ILog l) => l.IsEnabled(LogLevel.Critical);
    }
}
