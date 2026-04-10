using System;
using System.Collections.Generic;
using System.Linq;

namespace SaveData1.CrossPlateTesting.Models
{
    /// <summary>
    /// Соединение между узлами — определяет поток выполнения.
    /// </summary>
    public class ConnectionData
    {
        public Guid FromNodeId { get; set; }
        public string FromHandle { get; set; } = "out";  // out, true, false, body, after
        public Guid ToNodeId { get; set; }
        public string ToHandle { get; set; } = "in";
    }

    /// <summary>
    /// Базовый узел скрипта .mavparams для визуального дерева.
    /// </summary>
    public abstract class ScriptNode
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public abstract string NodeType { get; }
        /// <summary>Позиция на канвасе (для свободного размещения).</summary>
        public System.Drawing.PointF Position { get; set; }
    }

    /// <summary>
    /// set PARAM value | toggle
    /// </summary>
    public class SetNode : ScriptNode
    {
        public override string NodeType => "set";
        public string ParamName { get; set; }
        public float? Value { get; set; }
        public bool IsToggle { get; set; }
    }

    /// <summary>
    /// read PARAM
    /// </summary>
    public class ReadNode : ScriptNode
    {
        public override string NodeType => "read";
        public string ParamName { get; set; }
    }

    /// <summary>
    /// if PARAM op value ... else ... endif
    /// </summary>
    public class IfNode : ScriptNode
    {
        public override string NodeType => "if";
        public string ParamName { get; set; }
        public string Operator { get; set; }  // ==, !=, <, >, <=, >=
        public float CompareValue { get; set; }
        public string CompareVarName { get; set; }  // если задано — сравниваем с переменной
        public List<ScriptNode> ThenBranch { get; set; } = new List<ScriptNode>();
        public List<ScriptNode> ElseBranch { get; set; } = new List<ScriptNode>();
    }

    /// <summary>
    /// while PARAM op value ... endwhile — цикл пока условие истинно.
    /// </summary>
    public class WhileNode : ScriptNode
    {
        public override string NodeType => "while";
        public string ParamName { get; set; }  // имя переменной или MAVLink-параметра
        public string Operator { get; set; }  // ==, !=, <, >, <=, >=
        public float CompareValue { get; set; }
        public string CompareVarName { get; set; }  // если задано — сравниваем с переменной
        public List<ScriptNode> Body { get; set; } = new List<ScriptNode>();
    }

    /// <summary>
    /// var NAME = value — объявление переменной скрипта.
    /// </summary>
    public class VarDeclNode : ScriptNode
    {
        public override string NodeType => "var";
        public string VarName { get; set; }
        public float Value { get; set; }
    }

    /// <summary>
    /// mode NUMBER — переключение режима полёта (MAV_CMD_DO_SET_MODE). 0=Stabilize, 5=Loiter, 6=RTL, 9=Land и т.д.
    /// </summary>
    public class SetModeNode : ScriptNode
    {
        public override string NodeType => "mode";
        public int ModeNumber { get; set; }
    }

    /// <summary>
    /// sleep SECONDS — ожидание в секундах (поддержка float, например 1.5).
    /// </summary>
    public class SleepNode : ScriptNode
    {
        public override string NodeType => "sleep";
        public float Seconds { get; set; }
    }

    /// <summary>
    /// sleep_ms MILLISECONDS — ожидание в миллисекундах (совместимость с Mission Planner).
    /// </summary>
    public class SleepMsNode : ScriptNode
    {
        public override string NodeType => "sleep_ms";
        public int Milliseconds { get; set; }
    }

    /// <summary>
    /// arm | disarm — включение/выключение моторов (MAV_CMD_COMPONENT_ARM_DISARM).
    /// </summary>
    public class ArmNode : ScriptNode
    {
        public override string NodeType => "arm";
        public bool Arm { get; set; }  // true = arm, false = disarm
    }

    /// <summary>
    /// sendrc CHANNEL PWM — переопределение RC канала (RC_CHANNELS_OVERRIDE).
    /// </summary>
    public class SendRcNode : ScriptNode
    {
        public override string NodeType => "sendrc";
        public int Channel { get; set; }  // 1-8
        public ushort Pwm { get; set; }   // 1000-2000
    }

    /// <summary>
    /// waitfor "SUBSTRING" TIMEOUT_SEC — ожидание STATUSTEXT с подстрокой.
    /// </summary>
    public class WaitForNode : ScriptNode
    {
        public override string NodeType => "waitfor";
        public string Substring { get; set; }
        public float TimeoutSeconds { get; set; }
    }

    /// <summary>
    /// NAME = expr — присваивание переменной. expr: value | NAME+value | NAME-value | NAME*value | NAME/value | ++ | --
    /// </summary>
    public class VarAssignNode : ScriptNode
    {
        public override string NodeType => "assign";
        public string VarName { get; set; }
        public string Op { get; set; }  // assign, add, sub, mul, div, inc, dec
        public float? Value { get; set; }  // for assign/add/sub/mul/div; not used for inc/dec
        public string SourceVarName { get; set; }  // для x = y + 1 — имя переменной-источника
    }

    /// <summary>
    /// Корневой узел — последовательность операций.
    /// </summary>
    public class RootNode : ScriptNode
    {
        public override string NodeType => "root";
        public List<ScriptNode> Children { get; set; } = new List<ScriptNode>();
        /// <summary>Соединения между узлами. Если не пусто — структура берётся из них, а не из дерева.</summary>
        public List<ConnectionData> Connections { get; set; } = new List<ConnectionData>();
        /// <summary>true = строить дерево из Connections при генерации кода.</summary>
        public bool UseConnectionGraph { get; set; }
    }

    /// <summary>
    /// Парсит .mavparams в дерево узлов и сериализует обратно в код.
    /// </summary>
    public static class ScriptNodeTree
    {
        private static readonly System.Text.RegularExpressions.Regex SetRegex =
            new System.Text.RegularExpressions.Regex(@"^\s*set\s+(\w+)\s+(toggle|[\d\.\-]+)\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        private static readonly System.Text.RegularExpressions.Regex ReadRegex =
            new System.Text.RegularExpressions.Regex(@"^\s*read\s+(\w+)\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        private static readonly System.Text.RegularExpressions.Regex IfRegex =
            new System.Text.RegularExpressions.Regex(@"^\s*if\s+(\w+)\s*(==|!=|<=|>=|<|>)\s*(\w+|[\d\.\-]+)\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        private static readonly System.Text.RegularExpressions.Regex ElseRegex =
            new System.Text.RegularExpressions.Regex(@"^\s*else\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        private static readonly System.Text.RegularExpressions.Regex EndifRegex =
            new System.Text.RegularExpressions.Regex(@"^\s*endif\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        private static readonly System.Text.RegularExpressions.Regex WhileRegex =
            new System.Text.RegularExpressions.Regex(@"^\s*while\s+(\w+)\s*(==|!=|<=|>=|<|>)\s*(\w+|[\d\.\-]+)\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        private static readonly System.Text.RegularExpressions.Regex EndwhileRegex =
            new System.Text.RegularExpressions.Regex(@"^\s*endwhile\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        private static readonly System.Text.RegularExpressions.Regex VarDeclRegex =
            new System.Text.RegularExpressions.Regex(@"^\s*var\s+(\w+)\s*=\s*([\d\.\-]+)\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        private static readonly System.Text.RegularExpressions.Regex VarAssignRegex =
            new System.Text.RegularExpressions.Regex(@"^\s*(\w+)\s*=\s*([\d\.\-]+)\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        private static readonly System.Text.RegularExpressions.Regex VarAssignExprRegex =
            new System.Text.RegularExpressions.Regex(@"^\s*(\w+)\s*=\s*(\w+)\s*(\+|\-|\*|\/)\s*([\d\.\-]+)\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        private static readonly System.Text.RegularExpressions.Regex VarAssignOpRegex =
            new System.Text.RegularExpressions.Regex("^\\s*(\\w+)\\s*(\\+\\=|-=|\\*\\=|\\/\\=)\\s*([\\d\\.\\-]+)\\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        private static readonly System.Text.RegularExpressions.Regex VarIncDecRegex =
            new System.Text.RegularExpressions.Regex("^\\s*(\\w+)\\s*(\\+\\+|\\-\\-)\\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        private static readonly System.Text.RegularExpressions.Regex SleepRegex =
            new System.Text.RegularExpressions.Regex(@"^\s*sleep\s+([\d\.\-]+)\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        private static readonly System.Text.RegularExpressions.Regex ModeRegex =
            new System.Text.RegularExpressions.Regex(@"^\s*mode\s+(\w+)\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        private static readonly System.Text.RegularExpressions.Regex SleepMsRegex =
            new System.Text.RegularExpressions.Regex(@"^\s*sleep_ms\s+(\d+)\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        private static readonly System.Text.RegularExpressions.Regex ArmRegex =
            new System.Text.RegularExpressions.Regex(@"^\s*(arm|disarm)\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        private static readonly System.Text.RegularExpressions.Regex SendRcRegex =
            new System.Text.RegularExpressions.Regex(@"^\s*sendrc\s+(\d+)\s+(\d+)\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        private static readonly System.Text.RegularExpressions.Regex WaitForRegex =
            new System.Text.RegularExpressions.Regex(@"^\s*waitfor\s+""([^""]*)""\s+([\d\.\-]+)\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        private static readonly System.Text.RegularExpressions.Regex LegacyParamEq =
            new System.Text.RegularExpressions.Regex(@"^\s*(\w+)\s*=\s*(.+?)\s*$");
        private static readonly System.Text.RegularExpressions.Regex LegacyParamOnly =
            new System.Text.RegularExpressions.Regex(@"^\s*(\w+)\s*$");

        public static RootNode Parse(string[] lines)
        {
            var root = new RootNode();
            if (lines == null) return root;

            ParseBlock(lines, 0, lines.Length, root.Children, 0, out _);
            return root;
        }

        private static int ParseBlock(string[] lines, int start, int end, List<ScriptNode> target, int minIndent, out int consumed)
        {
            consumed = 0;
            int i = start;
            while (i < end)
            {
                var line = lines[i];
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                {
                    i++;
                    consumed = i - start;
                    continue;
                }

                var lineIndent = GetIndent(line);
                if (lineIndent < minIndent && !string.IsNullOrEmpty(trimmed))
                {
                    consumed = i - start;
                    return i;
                }

                if (EndifRegex.IsMatch(trimmed) || EndwhileRegex.IsMatch(trimmed))
                {
                    consumed = i - start + 1;
                    return i + 1;
                }

                if (ElseRegex.IsMatch(trimmed))
                {
                    consumed = i - start;
                    return i;
                }

                var ifMatch = IfRegex.Match(trimmed);
                if (ifMatch.Success)
                {
                    var condRight = ifMatch.Groups[3].Value;
                    var ifNode = new IfNode
                    {
                        ParamName = ifMatch.Groups[1].Value,
                        Operator = ifMatch.Groups[2].Value,
                        CompareValue = IsFloat(condRight) ? ParseFloat(condRight) : 0f,
                        CompareVarName = IsFloat(condRight) ? null : condRight
                    };
                    int next = i + 1;
                    int childIndent = minIndent + 2;
                    next = ParseBlock(lines, next, end, ifNode.ThenBranch, childIndent, out int c1);
                    if (next < end && ElseRegex.IsMatch(lines[next].Trim()))
                    {
                        next++;
                        next = ParseBlock(lines, next, end, ifNode.ElseBranch, childIndent, out int c2);
                        if (next < end && EndifRegex.IsMatch(lines[next].Trim()))
                            next++;
                    }
                    else if (next < end && EndifRegex.IsMatch(lines[next].Trim()))
                    {
                        next++;
                    }
                    target.Add(ifNode);
                    i = next;
                    consumed = i - start;
                    continue;
                }

                var whileMatch = WhileRegex.Match(trimmed);
                if (whileMatch.Success)
                {
                    var condRight = whileMatch.Groups[3].Value;
                    var whileNode = new WhileNode
                    {
                        ParamName = whileMatch.Groups[1].Value,
                        Operator = whileMatch.Groups[2].Value,
                        CompareValue = IsFloat(condRight) ? ParseFloat(condRight) : 0f,
                        CompareVarName = IsFloat(condRight) ? null : condRight
                    };
                    int next = i + 1;
                    int childIndent = minIndent + 2;
                    next = ParseBlock(lines, next, end, whileNode.Body, childIndent, out int c1);
                    if (next < end && EndwhileRegex.IsMatch(lines[next].Trim()))
                        next++;
                    target.Add(whileNode);
                    i = next;
                    consumed = i - start;
                    continue;
                }

                var varDeclMatch = VarDeclRegex.Match(trimmed);
                if (varDeclMatch.Success)
                {
                    target.Add(new VarDeclNode { VarName = varDeclMatch.Groups[1].Value, Value = ParseFloat(varDeclMatch.Groups[2].Value) });
                    i++;
                    consumed = i - start;
                    continue;
                }

                var varIncDecMatch = VarIncDecRegex.Match(trimmed);
                if (varIncDecMatch.Success)
                {
                    var op = varIncDecMatch.Groups[2].Value;
                    target.Add(new VarAssignNode { VarName = varIncDecMatch.Groups[1].Value, Op = op, Value = null });
                    i++;
                    consumed = i - start;
                    continue;
                }

                var varAssignOpMatch = VarAssignOpRegex.Match(trimmed);
                if (varAssignOpMatch.Success)
                {
                    target.Add(new VarAssignNode
                    {
                        VarName = varAssignOpMatch.Groups[1].Value,
                        Op = varAssignOpMatch.Groups[2].Value,
                        Value = ParseFloat(varAssignOpMatch.Groups[3].Value)
                    });
                    i++;
                    consumed = i - start;
                    continue;
                }

                var varAssignExprMatch = VarAssignExprRegex.Match(trimmed);
                if (varAssignExprMatch.Success)
                {
                    var targetVar = varAssignExprMatch.Groups[1].Value;
                    var sourceVar = varAssignExprMatch.Groups[2].Value;
                    var op = varAssignExprMatch.Groups[3].Value;
                    var rightVal = ParseFloat(varAssignExprMatch.Groups[4].Value);
                    var assignOp = op == "+" ? ("+" + "=") : op == "-" ? ("-" + "=") : op == "*" ? ("*" + "=") : ("/" + "=");
                    target.Add(new VarAssignNode { VarName = targetVar, SourceVarName = sourceVar, Op = assignOp, Value = rightVal });
                    i++;
                    consumed = i - start;
                    continue;
                }

                var varAssignMatch = VarAssignRegex.Match(trimmed);
                if (varAssignMatch.Success)
                {
                    var name = varAssignMatch.Groups[1].Value;
                    var right = varAssignMatch.Groups[2].Value.Trim();
                    if (string.Equals(name, "set", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "read", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "if", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "while", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "var", StringComparison.OrdinalIgnoreCase))
                    {
                        // не переменная, а ключевое слово — пропустить, обработает legacy
                    }
                    else
                    {
                        target.Add(new VarAssignNode { VarName = name, Op = "=", Value = ParseFloat(right) });
                        i++;
                        consumed = i - start;
                        continue;
                    }
                }

                var sleepMatch = SleepRegex.Match(trimmed);
                if (sleepMatch.Success)
                {
                    target.Add(new SleepNode { Seconds = ParseFloat(sleepMatch.Groups[1].Value) });
                    i++;
                    consumed = i - start;
                    continue;
                }

                var modeMatch = ModeRegex.Match(trimmed);
                if (modeMatch.Success)
                {
                    var modeArg = modeMatch.Groups[1].Value;
                    int modeNum;
                    if (int.TryParse(modeArg, out modeNum) && modeNum >= 0 && modeNum <= 17)
                        target.Add(new SetModeNode { ModeNumber = modeNum });
                    else
                        target.Add(new SetModeNode { ModeNumber = ResolveModeName(modeArg) });
                    i++;
                    consumed = i - start;
                    continue;
                }

                var sleepMsMatch = SleepMsRegex.Match(trimmed);
                if (sleepMsMatch.Success)
                {
                    if (int.TryParse(sleepMsMatch.Groups[1].Value, out int ms) && ms >= 0)
                        target.Add(new SleepMsNode { Milliseconds = ms });
                    i++;
                    consumed = i - start;
                    continue;
                }

                var armMatch = ArmRegex.Match(trimmed);
                if (armMatch.Success)
                {
                    var cmd = armMatch.Groups[1].Value.Trim().ToUpperInvariant();
                    target.Add(new ArmNode { Arm = cmd == "ARM" });
                    i++;
                    consumed = i - start;
                    continue;
                }

                var sendRcMatch = SendRcRegex.Match(trimmed);
                if (sendRcMatch.Success)
                {
                    if (int.TryParse(sendRcMatch.Groups[1].Value, out int ch) && int.TryParse(sendRcMatch.Groups[2].Value, out int pwm))
                    {
                        ch = Math.Max(1, Math.Min(8, ch));
                        pwm = Math.Max(1000, Math.Min(2000, pwm));
                        target.Add(new SendRcNode { Channel = ch, Pwm = (ushort)pwm });
                    }
                    i++;
                    consumed = i - start;
                    continue;
                }

                var waitForMatch = WaitForRegex.Match(trimmed);
                if (waitForMatch.Success)
                {
                    var sub = waitForMatch.Groups[1].Value ?? "";
                    var timeout = ParseFloat(waitForMatch.Groups[2].Value);
                    target.Add(new WaitForNode { Substring = sub, TimeoutSeconds = Math.Max(0, timeout) });
                    i++;
                    consumed = i - start;
                    continue;
                }

                var setMatch = SetRegex.Match(trimmed);
                if (setMatch.Success)
                {
                    var param = setMatch.Groups[1].Value;
                    var right = setMatch.Groups[2].Value.Trim();
                    var setNode = new SetNode { ParamName = param };
                    if (string.Equals(right, "toggle", StringComparison.OrdinalIgnoreCase))
                        setNode.IsToggle = true;
                    else if (float.TryParse(right.Replace(',', '.'), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float v))
                        setNode.Value = v;
                    target.Add(setNode);
                    i++;
                    consumed = i - start;
                    continue;
                }

                var readMatch = ReadRegex.Match(trimmed);
                if (readMatch.Success)
                {
                    target.Add(new ReadNode { ParamName = readMatch.Groups[1].Value });
                    i++;
                    consumed = i - start;
                    continue;
                }

                // Legacy: PARAM=value|toggle or PARAM
                var legacyEq = LegacyParamEq.Match(trimmed);
                if (legacyEq.Success)
                {
                    var param = legacyEq.Groups[1].Value;
                    var right = legacyEq.Groups[2].Value.Trim();
                    if (string.Equals(right, "toggle", StringComparison.OrdinalIgnoreCase))
                        target.Add(new SetNode { ParamName = param, IsToggle = true });
                    else if (float.TryParse(right.Replace(',', '.'), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float v))
                        target.Add(new SetNode { ParamName = param, Value = v });
                    else
                        target.Add(new ReadNode { ParamName = param });
                    i++;
                    consumed = i - start;
                    continue;
                }

                var legacyOnly = LegacyParamOnly.Match(trimmed);
                if (legacyOnly.Success && !trimmed.StartsWith("if") && !trimmed.StartsWith("set") && !trimmed.StartsWith("read"))
                {
                    target.Add(new ReadNode { ParamName = legacyOnly.Groups[1].Value });
                    i++;
                    consumed = i - start;
                    continue;
                }

                i++;
                consumed = i - start;
            }
            consumed = i - start;
            return i;
        }

        private static int GetIndent(string line)
        {
            int n = 0;
            foreach (char c in line)
            {
                if (c == ' ' || c == '\t') n++;
                else break;
            }
            return n;
        }

        private static float ParseFloat(string s)
        {
            if (float.TryParse((s ?? "").Replace(',', '.'), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float v))
                return v;
            return 0f;
        }

        /// <summary>
        /// Сериализует дерево обратно в код .mavparams.
        /// </summary>
        public static string ToCode(RootNode root)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Скрипт параметров MAVLink (.mavparams)");
            sb.AppendLine("# var NAME = value | sleep SECONDS | if/while VAR op value");
            sb.AppendLine();
            AppendNodes(sb, root.Children, 0);
            return sb.ToString();
        }

        private static void AppendNodes(System.Text.StringBuilder sb, List<ScriptNode> nodes, int indent)
        {
            var prefix = new string(' ', indent * 2);
            foreach (var n in nodes)
            {
                if (n is SetNode set)
                {
                    var right = set.IsToggle ? "toggle" : set.Value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "0";
                    sb.AppendLine($"{prefix}set {set.ParamName} {right}");
                }
                else if (n is ReadNode read)
                    sb.AppendLine($"{prefix}read {read.ParamName}");
                else if (n is SleepNode sl)
                    sb.AppendLine($"{prefix}sleep {sl.Seconds.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                else if (n is SleepMsNode slm)
                    sb.AppendLine($"{prefix}sleep_ms {slm.Milliseconds}");
                else if (n is ArmNode arm)
                    sb.AppendLine($"{prefix}{(arm.Arm ? "arm" : "disarm")}");
                else if (n is SendRcNode src)
                    sb.AppendLine($"{prefix}sendrc {src.Channel} {src.Pwm}");
                else if (n is WaitForNode wf)
                    sb.AppendLine($"{prefix}waitfor \"{wf.Substring}\" {wf.TimeoutSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                else if (n is SetModeNode sm)
                {
                    var name = GetModeName(sm.ModeNumber);
                    sb.AppendLine(name != null ? $"{prefix}mode {name}" : $"{prefix}mode {sm.ModeNumber}");
                }
                else if (n is IfNode iff)
                {
                    var right = iff.CompareVarName ?? iff.CompareValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    sb.AppendLine($"{prefix}if {iff.ParamName} {iff.Operator} {right}");
                    AppendNodes(sb, iff.ThenBranch, indent + 1);
                    if (iff.ElseBranch.Count > 0)
                    {
                        sb.AppendLine($"{prefix}else");
                        AppendNodes(sb, iff.ElseBranch, indent + 1);
                    }
                    sb.AppendLine($"{prefix}endif");
                }
                else if (n is WhileNode whilew)
                {
                    var right = whilew.CompareVarName ?? whilew.CompareValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    sb.AppendLine($"{prefix}while {whilew.ParamName} {whilew.Operator} {right}");
                    AppendNodes(sb, whilew.Body, indent + 1);
                    sb.AppendLine($"{prefix}endwhile");
                }
                else if (n is VarDeclNode vd)
                    sb.AppendLine($"{prefix}var {vd.VarName} = {vd.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                else if (n is VarAssignNode va)
                {
                    if (va.Op == "++" || va.Op == "--")
                        sb.AppendLine($"{prefix}{va.VarName}{va.Op}");
                    else if (va.SourceVarName != null && va.Value.HasValue)
                    {
                        var opChar = (va.Op == ("+" + "=")) ? "+" : (va.Op == ("-" + "=")) ? "-" : (va.Op == ("*" + "=")) ? "*" : "/";
                        sb.AppendLine($"{prefix}{va.VarName} = {va.SourceVarName} {opChar} {va.Value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                    }
                    else if (va.Value.HasValue)
                        sb.AppendLine($"{prefix}{va.VarName} {va.Op} {va.Value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                    else if (va.Op == "=")
                        sb.AppendLine($"{prefix}{va.VarName} = 0");
                }
            }
        }

        /// <summary>
        /// Собирает все узлы в плоский список (для визуализации).
        /// </summary>
        public static List<ScriptNode> Flatten(RootNode root)
        {
            var list = new List<ScriptNode>();
            FlattenNodes(root.Children, list);
            return list;
        }

        private static void FlattenNodes(List<ScriptNode> nodes, List<ScriptNode> target)
        {
            foreach (var n in nodes)
            {
                target.Add(n);
                if (n is SleepNode || n is SleepMsNode || n is SetModeNode || n is ArmNode || n is SendRcNode || n is WaitForNode)
                    continue;
                if (n is IfNode iff)
                {
                    FlattenNodes(iff.ThenBranch, target);
                    FlattenNodes(iff.ElseBranch, target);
                }
                else if (n is WhileNode whilew)
                    FlattenNodes(whilew.Body, target);
            }
        }

        private static bool IsFloat(string s)
        {
            return float.TryParse((s ?? "").Replace(',', '.'), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out _);
        }

        public static int ResolveModeName(string name)
        {
            var n = (name ?? "").Trim().ToUpperInvariant();
            switch (n)
            {
                case "STABILIZE": return 0;
                case "ACRO": return 1;
                case "ALTHOLD": case "ALT_HOLD": return 2;
                case "AUTO": return 3;
                case "GUIDED": return 4;
                case "LOITER": return 5;
                case "RTL": return 6;
                case "CIRCLE": return 7;
                case "LAND": return 9;
                case "DRIFT": return 11;
                case "SPORT": return 13;
                case "FLIP": return 14;
                case "POSHOLD": case "POS_HOLD": return 16;
                case "BRAKE": return 17;
                default: return int.TryParse(n, out int num) && num >= 0 && num <= 17 ? num : 5;
            }
        }

        private static string GetModeName(int num)
        {
            switch (num)
            {
                case 0: return "STABILIZE";
                case 1: return "ACRO";
                case 2: return "ALTHOLD";
                case 3: return "AUTO";
                case 4: return "GUIDED";
                case 5: return "LOITER";
                case 6: return "RTL";
                case 7: return "CIRCLE";
                case 9: return "LAND";
                case 11: return "DRIFT";
                case 13: return "SPORT";
                case 14: return "FLIP";
                case 16: return "POSHOLD";
                case 17: return "BRAKE";
                default: return null;
            }
        }

        /// <summary>
        /// Строит дерево из графа соединений. Вызывается при UseConnectionGraph=true.
        /// </summary>
        public static void BuildTreeFromConnections(RootNode root)
        {
            var conns = root.Connections ?? new List<ConnectionData>();
            var allNodes = Flatten(root).ToList();  // до очистки дерева
            var nodesById = allNodes.ToDictionary(n => n.Id);

            var outConns = new Dictionary<Guid, List<(string handle, Guid toId)>>();
            foreach (var c in conns)
            {
                if (!outConns.ContainsKey(c.FromNodeId))
                    outConns[c.FromNodeId] = new List<(string, Guid)>();
                outConns[c.FromNodeId].Add((c.FromHandle ?? "out", c.ToNodeId));
            }

            var inConns = new Dictionary<Guid, int>();
            foreach (var c in conns)
            {
                if (!inConns.ContainsKey(c.ToNodeId)) inConns[c.ToNodeId] = 0;
                inConns[c.ToNodeId]++;
            }

            foreach (var iff in allNodes.OfType<IfNode>())
            {
                iff.ThenBranch.Clear();
                iff.ElseBranch.Clear();
            }
            foreach (var w in allNodes.OfType<WhileNode>())
                w.Body.Clear();
            root.Children.Clear();

            var entryIds = allNodes.Where(n => !(n is RootNode) && (!inConns.ContainsKey(n.Id) || inConns[n.Id] == 0)).Select(n => n.Id).ToList();
            if (entryIds.Count == 0 && conns.Count > 0)
                entryIds.Add(conns[0].ToNodeId);

            var visited = new HashSet<Guid>();
            foreach (var entryId in entryIds)
            {
                if (!nodesById.TryGetValue(entryId, out var entryNode)) continue;
                BuildTreeRecursive(entryNode, root.Children, nodesById, outConns, visited);
            }
        }

        private static void BuildTreeRecursive(ScriptNode node, List<ScriptNode> target,
            Dictionary<Guid, ScriptNode> nodesById,
            Dictionary<Guid, List<(string handle, Guid toId)>> outConns,
            HashSet<Guid> visited)
        {
            if (visited.Contains(node.Id)) return;
            visited.Add(node.Id);

            if (node is IfNode iff)
            {
                target.Add(iff);
                var outs = outConns.ContainsKey(iff.Id) ? outConns[iff.Id] : null;
                if (outs != null)
                {
                    foreach (var (handle, toId) in outs)
                    {
                        if (handle == "true" && nodesById.TryGetValue(toId, out var tn))
                            BuildTreeRecursive(tn, iff.ThenBranch, nodesById, outConns, visited);
                        else if (handle == "false" && nodesById.TryGetValue(toId, out var en))
                            BuildTreeRecursive(en, iff.ElseBranch, nodesById, outConns, visited);
                    }
                    var outNext = outs.FirstOrDefault(o => o.handle == "out");
                    if (outNext.toId != Guid.Empty && nodesById.TryGetValue(outNext.toId, out var nextNode))
                        BuildTreeRecursive(nextNode, target, nodesById, outConns, visited);
                }
            }
            else if (node is WhileNode whilew)
            {
                target.Add(whilew);
                var outs = outConns.ContainsKey(whilew.Id) ? outConns[whilew.Id] : null;
                if (outs != null)
                {
                    foreach (var (handle, toId) in outs)
                    {
                        if (handle == "body" && nodesById.TryGetValue(toId, out var bn))
                            BuildTreeRecursive(bn, whilew.Body, nodesById, outConns, visited);
                    }
                    var outAfter = outs.FirstOrDefault(o => o.handle == "after" || o.handle == "out");
                    if (outAfter.toId != Guid.Empty && nodesById.TryGetValue(outAfter.toId, out var afterNode))
                        BuildTreeRecursive(afterNode, target, nodesById, outConns, visited);
                }
            }
            else
            {
                target.Add(node);
                if (outConns.TryGetValue(node.Id, out var outs) && outs.Count > 0)
                {
                    var first = outs.FirstOrDefault(o => o.handle == "out");
                    if (first.toId != Guid.Empty && nodesById.TryGetValue(first.toId, out var nextNode))
                        BuildTreeRecursive(nextNode, target, nodesById, outConns, visited);
                }
            }
        }

        /// <summary>
        /// Заполняет Connections из текущего дерева (для начальной синхронизации).
        /// </summary>
        public static void BuildConnectionsFromTree(RootNode root)
        {
            root.Connections.Clear();
            BuildConnectionsRecursive(root.Children, null, "out", root.Connections);
        }

        private static void BuildConnectionsRecursive(List<ScriptNode> nodes, ScriptNode prevNode, string prevHandle,
            List<ConnectionData> conns)
        {
            foreach (var n in nodes)
            {
                if (prevNode != null)
                    conns.Add(new ConnectionData { FromNodeId = prevNode.Id, FromHandle = prevHandle, ToNodeId = n.Id, ToHandle = "in" });

                if (n is IfNode iff)
                {
                    if (iff.ThenBranch.Count > 0)
                        BuildConnectionsRecursive(iff.ThenBranch, iff, "true", conns);
                    if (iff.ElseBranch.Count > 0)
                        BuildConnectionsRecursive(iff.ElseBranch, iff, "false", conns);
                    prevNode = iff;
                    prevHandle = "out";
                }
                else if (n is WhileNode whilew)
                {
                    if (whilew.Body.Count > 0)
                        BuildConnectionsRecursive(whilew.Body, whilew, "body", conns);
                    prevNode = whilew;
                    prevHandle = "after";
                }
                else
                {
                    prevNode = n;
                    prevHandle = "out";
                }
            }
        }
    }
}
