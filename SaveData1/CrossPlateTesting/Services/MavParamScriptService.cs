using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SaveData1.CrossPlateTesting.Services
{
    /// <summary>
    /// Условие для операций: param op value (==, !=, &lt;, &gt;, &lt;=, &gt;=)
    /// </summary>
    public class MavParamCondition
    {
        public string ParamName { get; set; }
        public string Operator { get; set; }  // ==, !=, <, >, <=, >=
        public float CompareValue { get; set; }
    }

    /// <summary>
    /// Операция над параметром MAVLink из скрипта .mavparams
    /// </summary>
    public class MavParamOperation
    {
        public string ParamName { get; set; }
        public string Action { get; set; }  // "read", "toggle", "set"
        public float? Value { get; set; }
        /// <summary>Условия для выполнения (AND). Пустой список = выполнять всегда.</summary>
        public List<MavParamCondition> Conditions { get; set; } = new List<MavParamCondition>();
        /// <summary>Уровень вложенности (для отладки)</summary>
        public int NestLevel { get; set; }
    }

    /// <summary>
    /// Парсит файлы .mavparams — скрипты с операциями над параметрами MAVLink.
    /// Формат:
    ///   set PARAM value | toggle
    ///   read PARAM
    ///   if PARAM op value ... else ... endif (op: ==, !=, &lt;, &gt;, &lt;=, &gt;=)
    ///   Поддержка вложенных условий.
    /// </summary>
    public static class MavParamScriptService
    {
        private static readonly Regex SetRegex = new Regex(@"^\s*set\s+(\w+)\s+(toggle|[\d\.\-]+)\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex ReadRegex = new Regex(@"^\s*read\s+(\w+)\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex IfRegex = new Regex(@"^\s*if\s+(\w+)\s*(==|!=|<=|>=|<|>)\s*([\d\.\-]+)\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex ElseRegex = new Regex(@"^\s*else\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex EndifRegex = new Regex(@"^\s*endif\s*$", RegexOptions.IgnoreCase);

        // Legacy format: PARAM_NAME=action or PARAM_NAME=value
        private static readonly Regex LegacyParamEq = new Regex(@"^\s*(\w+)\s*=\s*(.+?)\s*$");
        private static readonly Regex LegacyParamOnly = new Regex(@"^\s*(\w+)\s*$");

        /// <summary>
        /// Читает и парсит файл скрипта параметров (новый и legacy формат).
        /// </summary>
        /// <param name="filePath">Путь к .mavparams файлу</param>
        /// <returns>Список операций или null при ошибке</returns>
        public static List<MavParamOperation> Parse(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return null;

            var lines = File.ReadAllLines(filePath);
            return ParseLines(lines);
        }

        /// <summary>
        /// Парсит строки скрипта (для тестирования и ScriptGeneratorForm).
        /// </summary>
        public static List<MavParamOperation> ParseLines(string[] lines)
        {
            if (lines == null) return null;

            var result = new List<MavParamOperation>();
            var stack = new Stack<ConditionContext>();
            var pendingElse = new Stack<bool>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                // endif
                if (EndifRegex.IsMatch(trimmed))
                {
                    if (stack.Count > 0)
                    {
                        stack.Pop();
                        if (pendingElse.Count > 0) pendingElse.Pop();
                    }
                    continue;
                }

                // else
                if (ElseRegex.IsMatch(trimmed))
                {
                    if (stack.Count > 0)
                    {
                        var ctx = stack.Peek();
                        ctx.InElseBranch = true;
                        if (pendingElse.Count > 0)
                        {
                            var v = pendingElse.Pop();
                            pendingElse.Push(!v);
                        }
                    }
                    continue;
                }

                // if PARAM op value
                var ifMatch = IfRegex.Match(trimmed);
                if (ifMatch.Success)
                {
                    var cond = new MavParamCondition
                    {
                        ParamName = ifMatch.Groups[1].Value,
                        Operator = ifMatch.Groups[2].Value,
                        CompareValue = ParseFloat(ifMatch.Groups[3].Value)
                    };
                    stack.Push(new ConditionContext { Condition = cond, InElseBranch = false });
                    pendingElse.Push(false);
                    continue;
                }

                // set PARAM value | toggle
                var setMatch = SetRegex.Match(trimmed);
                if (setMatch.Success)
                {
                    var paramName = setMatch.Groups[1].Value;
                    var right = setMatch.Groups[2].Value.Trim();
                    var op = new MavParamOperation { ParamName = paramName, NestLevel = stack.Count };
                    if (string.Equals(right, "toggle", StringComparison.OrdinalIgnoreCase))
                    {
                        op.Action = "toggle";
                    }
                    else if (float.TryParse(right.Replace(',', '.'), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float val))
                    {
                        op.Action = "set";
                        op.Value = val;
                    }
                    else
                    {
                        op.Action = "read";
                    }
                    ApplyCondition(op, stack);
                    result.Add(op);
                    continue;
                }

                // read PARAM
                var readMatch = ReadRegex.Match(trimmed);
                if (readMatch.Success)
                {
                    var op = new MavParamOperation
                    {
                        ParamName = readMatch.Groups[1].Value,
                        Action = "read",
                        NestLevel = stack.Count
                    };
                    ApplyCondition(op, stack);
                    result.Add(op);
                    continue;
                }

                // Legacy: PARAM_NAME=action or PARAM_NAME=value or PARAM_NAME
                var legacyEq = LegacyParamEq.Match(trimmed);
                if (legacyEq.Success)
                {
                    var paramName = legacyEq.Groups[1].Value;
                    var right = legacyEq.Groups[2].Value.Trim();
                    var op = new MavParamOperation { ParamName = paramName, NestLevel = stack.Count };
                    if (string.Equals(right, "toggle", StringComparison.OrdinalIgnoreCase))
                        op.Action = "toggle";
                    else if (float.TryParse(right.Replace(',', '.'), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float val))
                    {
                        op.Action = "set";
                        op.Value = val;
                    }
                    else
                        op.Action = "read";
                    ApplyCondition(op, stack);
                    result.Add(op);
                    continue;
                }

                var legacyOnly = LegacyParamOnly.Match(trimmed);
                if (legacyOnly.Success && !trimmed.StartsWith("if") && !trimmed.StartsWith("set") && !trimmed.StartsWith("read"))
                {
                    var op = new MavParamOperation
                    {
                        ParamName = legacyOnly.Groups[1].Value,
                        Action = "read",
                        NestLevel = stack.Count
                    };
                    ApplyCondition(op, stack);
                    result.Add(op);
                }
            }

            return result.Count > 0 ? result : null;
        }

        private static void ApplyCondition(MavParamOperation op, Stack<ConditionContext> stack)
        {
            foreach (var ctx in stack.Reverse())
            {
                op.Conditions.Add(ctx.InElseBranch ? InvertCondition(ctx.Condition) : ctx.Condition);
            }
        }

        private static MavParamCondition InvertCondition(MavParamCondition c)
        {
            if (c == null) return null;
            var inv = new MavParamCondition { ParamName = c.ParamName, CompareValue = c.CompareValue };
            switch (c.Operator)
            {
                case "==": inv.Operator = "!="; break;
                case "!=": inv.Operator = "=="; break;
                case "<": inv.Operator = ">="; break;
                case ">": inv.Operator = "<="; break;
                case "<=": inv.Operator = ">"; break;
                case ">=": inv.Operator = "<"; break;
                default: inv.Operator = "!="; break;
            }
            return inv;
        }

        private static float ParseFloat(string s)
        {
            if (float.TryParse((s ?? "").Replace(',', '.'), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float v))
                return v;
            return 0f;
        }

        private class ConditionContext
        {
            public MavParamCondition Condition { get; set; }
            public bool InElseBranch { get; set; }
        }

        /// <summary>
        /// Проверяет, выполняется ли одно условие при текущем значении параметра.
        /// </summary>
        public static bool EvaluateCondition(MavParamCondition cond, float? paramValue)
        {
            if (cond == null) return true;
            if (!paramValue.HasValue) return false;

            float a = paramValue.Value;
            float b = cond.CompareValue;
            switch (cond.Operator)
            {
                case "==": return Math.Abs(a - b) < 0.0001f;
                case "!=": return Math.Abs(a - b) >= 0.0001f;
                case "<": return a < b;
                case ">": return a > b;
                case "<=": return a <= b + 0.0001f;
                case ">=": return a >= b - 0.0001f;
                default: return false;
            }
        }

        /// <summary>
        /// Проверяет, выполняются ли все условия операции (нужны закэшированные значения параметров).
        /// </summary>
        public static bool EvaluateConditions(List<MavParamOperation> ops, int opIndex, Func<string, float?> getParamValue)
        {
            var op = ops[opIndex];
            if (op.Conditions == null || op.Conditions.Count == 0) return true;
            foreach (var c in op.Conditions)
            {
                var val = getParamValue(c.ParamName);
                if (!EvaluateCondition(c, val)) return false;
            }
            return true;
        }

        public static bool IsMavParamScript(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   path.EndsWith(".mavparams", StringComparison.OrdinalIgnoreCase);
        }
    }
}
