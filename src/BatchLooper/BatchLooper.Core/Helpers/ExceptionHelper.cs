using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;

namespace BatchLooper.Core.Helpers
{
    public static class ExceptionHelper
    {
        /// <summary>
        /// {0} => Idented level.
        /// {1} => Error Source.
        /// {2} => Error Message.
        /// {3} => Error Target.
        /// {4} => Error Line Number.
        /// </summary>
        private const string MskError = "{0}[{1}]: {2} | {3} ({4})";

        private static ConcurrentQueue<string> Messages { get; set; } = [];

        public static string GetAggregatedMessages(this Exception exception, bool detailed = true, bool idented = true, bool clearHistory = true) =>
            string.Join(Environment.NewLine, exception.GetListMessages(detailed, idented, clearHistory));

        public static IEnumerable<string> GetListMessages(this Exception parentEX, bool detailed = true, bool idented = true, bool clearHistory = true)
        {
            if (clearHistory && !Messages.IsEmpty)
                Messages.Clear();

            parentEX.GetMessage(detailed, idented);
            return Messages;
        }

        private static void GetMessage(this Exception? child, bool detailed, bool idented, string? identLeval = null)
        {
            if (child == null)
                return;

            Messages ??= [];

            var message = child.Message.EndsWith(".", StringComparison.OrdinalIgnoreCase) ? child.Message : child.Message + ".";
            var errTarget = child?.TargetSite?.Name ?? string.Empty;
            var errSource = child?.Source ?? string.Empty;

            var removeIdent = !idented
                || (detailed && idented
                    && !string.IsNullOrEmpty(errSource) && !string.IsNullOrEmpty(errTarget)
                    && !Messages.IsEmpty && Messages.TryPeek(out var tmpMsg)
                    && !(tmpMsg ?? string.Empty).Contains($"[{errSource}]:")
                    && !(tmpMsg ?? string.Empty).Contains($"| {errTarget} ("));

            if (idented)
                identLeval += " ";
            else if (removeIdent)
                identLeval = string.Empty;

            if (!detailed)
            {
                var msg = !Messages.IsEmpty ? $"{identLeval}Inner error: {message}" : message;
                Messages!.Enqueue(msg);
                child?.InnerException.GetMessage(detailed, idented, identLeval);
                return;
            }

            var errLine = new StackTrace(child!, true)?.GetFrame(0)?.GetFileLineNumber().ToString(CultureInfo.InvariantCulture) ?? "?";
            var formatedMsg = string.Format(MskError, identLeval, errSource, message, errTarget, errLine);

            //(child as DbEntityValidationException)?.GetMessage(identLeval);
            (child as AggregateException)?.GetMessage(detailed, idented, identLeval);

            Messages.Enqueue(formatedMsg);

            child?.InnerException.GetMessage(detailed, idented, identLeval);
        }

        //private static void GetMessage(this DbEntityValidationException? ex, string? identLeval = null)
        //{
        //    if (ex is null)
        //        return;

        //    foreach (var erro in ex.EntityValidationErrors)
        //    {
        //        string msgErro = string.Format(provider: CultureInfo.InvariantCulture, format: "{0} Entidade do tipo: {1} no estado: {2} tem os seguintes erros de validação:",
        //            identLeval, erro.Entry.Entity.GetType().FullName, erro.Entry.State);

        //        Messages.Add(msgErro);
        //        foreach (var err in erro.ValidationErrors)
        //        {
        //            msgErro = string.Format(provider: CultureInfo.InvariantCulture, format: "{0}  -> A propriedade: {1}, com o valor: {2}, tem o seguinte erro: {3}",
        //                identLeval, err.PropertyName, erro.Entry.CurrentValues.GetValue<object>(err.PropertyName),
        //                (err.ErrorMessage.EndsWith(".", StringComparison.OrdinalIgnoreCase)) ? err.ErrorMessage : err.ErrorMessage + ".");

        //            Messages.Add(msgErro);
        //        }
        //    }
        //}

        private static void GetMessage(this AggregateException? ex, bool detailed, bool idented, string? identLeval = null)
        {
            if (ex is null)
                return;

            ex.InnerExceptions
                .ToList()
                .ForEach(x => x.GetMessage(detailed, idented, identLeval));
        }
    }
}
