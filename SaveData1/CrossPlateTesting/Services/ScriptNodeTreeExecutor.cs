using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SaveData1.CrossPlateTesting.Models;

namespace SaveData1.CrossPlateTesting.Services
{
    /// <summary>
    /// Выполняет дерево скрипта .mavparams (set, read, toggle, if, while).
    /// </summary>
    public static class ScriptNodeTreeExecutor
    {
        private const int MaxLoopIterations = 1000;

        public static async Task<bool> RunFromFileAsync(string filePath, string host, int port,
            Action<string> log, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return false;
            var lines = File.ReadAllLines(filePath);
            var root = ScriptNodeTree.Parse(lines);
            return await RunAsync(root, host, port, log, ct);
        }

        public static async Task<bool> RunAsync(RootNode root, string host, int port,
            Action<string> log, CancellationToken ct = default)
        {
            if (root == null) return false;
            var paramCache = new Dictionary<string, float?>(StringComparer.OrdinalIgnoreCase);
            var varStore = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            return await RunNodesAsync(root.Children, host, port, paramCache, varStore, log, ct);
        }

        private static async Task<bool> RunNodesAsync(List<ScriptNode> nodes, string host, int port,
            Dictionary<string, float?> paramCache, Dictionary<string, float> varStore, Action<string> log, CancellationToken ct)
        {
            foreach (var node in nodes)
            {
                if (ct.IsCancellationRequested) return false;

                if (node is VarDeclNode vd)
                {
                    varStore[vd.VarName] = vd.Value;
                    log($"[VAR] {vd.VarName} = {vd.Value}");
                }
                else if (node is VarAssignNode va)
                {
                    float current = 0;
                    if (va.SourceVarName != null && varStore.TryGetValue(va.SourceVarName, out var sv))
                        current = sv;
                    else if (varStore.TryGetValue(va.VarName, out var cv))
                        current = cv;
                    float newVal = current;
                    switch (va.Op)
                    {
                        case "=": newVal = va.Value ?? 0; break;
                        case "\u002B=": newVal = current + (va.Value ?? 0); break;
                        case "-=": newVal = current - (va.Value ?? 0); break;
                        case "*=": newVal = current * (va.Value ?? 1); break;
                        case "/=": newVal = (va.Value ?? 1) != 0 ? current / (va.Value ?? 1) : current; break;
                        case "\u002B\u002B": newVal = current + 1; break;
                        case "--": newVal = current - 1; break;
                    }
                    varStore[va.VarName] = newVal;
                    log($"[VAR] {va.VarName} = {newVal}");
                }
                else if (node is SetNode set)
                {
                    if (set.IsToggle)
                    {
                        var result = await MavLinkService.ToggleParameterAsync(host, port, set.ParamName, DelaySettings.MavLink_ParamReadTimeoutMs, log, ct);
                        if (result.HasValue)
                        {
                            paramCache[set.ParamName] = result.Value ? 1f : 0f;
                            log($"[OK] {set.ParamName} = {(result.Value ? "true" : "false")}");
                        }
                        else
                        {
                            paramCache.Remove(set.ParamName);
                            log($"[ПРЕДУПРЕЖДЕНИЕ] Toggle {set.ParamName} не подтверждён дроном.");
                        }
                    }
                    else if (set.Value.HasValue)
                    {
                        bool ok = await MavLinkService.SetParameterAsync(host, port, set.ParamName, set.Value.Value, 1, 1, DelaySettings.MavLink_ParamReadTimeoutMs, log, 9, ct);
                        if (ok)
                        {
                            paramCache[set.ParamName] = set.Value.Value;
                            log($"[OK] {set.ParamName} = {set.Value.Value}");
                        }
                        else
                        {
                            paramCache.Remove(set.ParamName);
                            log($"[ПРЕДУПРЕЖДЕНИЕ] Set {set.ParamName} не подтверждён дроном.");
                        }
                    }
                }
                else if (node is ReadNode read)
                {
                    var value = await MavLinkService.ReadParameterAsync(host, port, read.ParamName, DelaySettings.MavLink_ParamReadTimeoutMs, log, ct);
                    if (value.HasValue)
                    {
                        paramCache[read.ParamName] = value.Value;
                        log($"[OK] {read.ParamName} = {value.Value}");
                    }
                }
                else if (node is SleepNode sl)
                {
                    var ms = (int)(Math.Max(0, sl.Seconds) * 1000);
                    log($"[SLEEP] {sl.Seconds} сек");
                    try { await Task.Delay(ms, ct); }
                    catch (OperationCanceledException) { return false; }
                }
                else if (node is SetModeNode sm)
                {
                    var ok = await MavLinkService.SetModeAsync(host, port, sm.ModeNumber, 1, 1, log, ct);
                    if (ok) log($"[OK] Режим -> {sm.ModeNumber}");
                }
                else if (node is SleepMsNode slm)
                {
                    var ms = Math.Max(0, slm.Milliseconds);
                    log($"[SLEEP_MS] {ms} мс");
                    try { await Task.Delay(ms, ct); }
                    catch (OperationCanceledException) { return false; }
                }
                else if (node is ArmNode arm)
                {
                    var ok = arm.Arm
                        ? await MavLinkService.ArmAsync(host, port, false, log, ct)
                        : await MavLinkService.DisarmAsync(host, port, false, log, ct);
                    if (ok) log($"[OK] {(arm.Arm ? "Arm" : "Disarm")}");
                    else log($"[ПРЕДУПРЕЖДЕНИЕ] {(arm.Arm ? "Arm" : "Disarm")} не выполнен.");
                }
                else if (node is SendRcNode src)
                {
                    var ok = await MavLinkService.SendRcOverrideAsync(host, port, src.Channel, src.Pwm, 1, 1, log, ct);
                    if (ok) log($"[OK] RC ch{src.Channel} = {src.Pwm}");
                    else log($"[ПРЕДУПРЕЖДЕНИЕ] RC ch{src.Channel} = {src.Pwm} не отправлен.");
                }
                else if (node is WaitForNode wf)
                {
                    var timeoutMs = (int)(Math.Max(0, wf.TimeoutSeconds) * 1000);
                    log($"[WAITFOR] \"{wf.Substring}\" timeout {timeoutMs} мс");
                    var ok = await MavLinkService.WaitForAsync(host, port, wf.Substring ?? "", timeoutMs, log, ct);
                    if (ok) log($"[OK] WaitFor найдено");
                }
                else if (node is IfNode iff)
                {
                    var val = await GetValue(host, port, iff.ParamName, paramCache, varStore, log, ct);
                    var compareVal = iff.CompareVarName != null && varStore.TryGetValue(iff.CompareVarName, out var cv) ? cv : iff.CompareValue;
                    bool cond = EvaluateCondition(iff.Operator, compareVal, val);
                    if (cond)
                        await RunNodesAsync(iff.ThenBranch, host, port, paramCache, varStore, log, ct);
                    else if (iff.ElseBranch.Count > 0)
                        await RunNodesAsync(iff.ElseBranch, host, port, paramCache, varStore, log, ct);
                }
                else if (node is WhileNode whilew)
                {
                    int iterations = 0;
                    while (iterations < MaxLoopIterations)
                    {
                        if (ct.IsCancellationRequested) return false;
                        // Для актуальной проверки условия while перечитываем параметр с дрона, а не из кэша.
                        paramCache.Remove(whilew.ParamName);
                        var val = await GetValue(host, port, whilew.ParamName, paramCache, varStore, log, ct);
                        var compareVal = whilew.CompareVarName != null && varStore.TryGetValue(whilew.CompareVarName, out var wcv) ? wcv : whilew.CompareValue;
                        if (!EvaluateCondition(whilew.Operator, compareVal, val))
                            break;
                        await RunNodesAsync(whilew.Body, host, port, paramCache, varStore, log, ct);
                        iterations++;
                        try { await Task.Delay(DelaySettings.Script_WhileIteration, ct); }
                        catch (OperationCanceledException) { return false; }
                    }
                    if (iterations >= MaxLoopIterations)
                        log($"[ПРЕДУПРЕЖДЕНИЕ] Цикл while достиг лимита {MaxLoopIterations} итераций.");
                }

                try { await Task.Delay(DelaySettings.Script_BetweenNodes, ct); }
                catch (OperationCanceledException) { return false; }
            }
            return true;
        }

        private static async Task<float?> GetValue(string host, int port, string name,
            Dictionary<string, float?> paramCache, Dictionary<string, float> varStore, Action<string> log, CancellationToken ct = default)
        {
            if (varStore.TryGetValue(name, out var vv)) return vv;
            if (paramCache.TryGetValue(name, out var v)) return v;
            int timeoutMs = Math.Max(1000, DelaySettings.MavLink_ParamReadTimeoutMs);
            log($"[MAVLink] Чтение параметра '{name}'...");
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                if (ct.IsCancellationRequested) return null;
                var val = await MavLinkService.ReadParameterAsync(host, port, name, timeoutMs, log, ct);
                if (val.HasValue)
                {
                    paramCache[name] = val.Value;
                    return val;
                }
                if (attempt < 3)
                    log($"[MAVLink] Чтение '{name}' не удалось, повтор {attempt}/3...");
            }
            log($"[MAVLink] Параметр '{name}' не получен после 3 попыток.");
            return null;
        }

        private static bool EvaluateCondition(string op, float compareValue, float? paramValue)
        {
            if (!paramValue.HasValue) return false;
            float a = paramValue.Value;
            float b = compareValue;
            switch (op)
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
    }
}
