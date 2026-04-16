using System;
using System.Text;

namespace SaveData1.Helpers
{
    /// <summary>Сообщения об ошибках для UI: EF часто прячет причину во внутреннем исключении (например SqlException).</summary>
    public static class ExceptionDisplay
    {
        public static string MessageWithInners(Exception ex, int maxDepth = 6)
        {
            if (ex == null) return "";
            var sb = new StringBuilder(ex.Message.TrimEnd());
            int depth = 0;
            for (var inner = ex.InnerException; inner != null && depth < maxDepth; inner = inner.InnerException, depth++)
            {
                sb.AppendLine().Append("→ ").Append(inner.Message.TrimEnd());
            }
            return sb.ToString();
        }
    }
}
