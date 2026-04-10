using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using SaveData1.CrossPlateTesting.Models;

namespace SaveData1.CrossPlateTesting.Controls
{
    /// <summary>
    /// Визуальный редактор скрипта — ноды, соединения, дерево, drag-drop, контекстное меню.
    /// </summary>
    public class ScriptCanvasControl : UserControl
    {
        private RootNode _root;
        private readonly List<NodeViewModel> _nodes = new List<NodeViewModel>();
        private readonly List<ConnectionViewModel> _connections = new List<ConnectionViewModel>();
        private NodeViewModel _selectedNode;
        private Point _panOffset;
        private Point _lastMouse;
        private bool _isPanning;
        private float _zoom = 1f;
        private NodeViewModel _dragNode;
        private PointF _dragOffset;
        private ContextMenuStrip _ctxMenu;
        private NodeViewModel _connectionDragFrom;
        private string _connectionDragHandle;
        private PointF _connectionDragTo;
        private ConnectionViewModel _selectedConnection;

        public event Action<RootNode> ModelChanged;
        public string DefaultParamName { get; set; } = "SERVO1_REVERSED";

        public RootNode Root
        {
            get => _root;
            set { _root = value ?? new RootNode(); RebuildFromModel(); }
        }

        private const int BaseNodeWidth = 220;
        private const int FieldHeight = 26;
        private const int HeaderHeight = 32;
        private const int FooterHeight = 22;
        private const int NodePadding = 10;
        private const int VerticalGap = 20;
        private const int BranchIndent = 100;
        private const int HandleRadius = 5;
        private const int CornerRadius = 12;

        public ScriptCanvasControl()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(28, 28, 32);
            _root = new RootNode();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
            AllowDrop = true;
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Delete) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Delete)
            {
                if (_selectedNode != null)
                {
                    DeleteNode(_selectedNode);
                    _selectedNode = null;
                }
                else if (_selectedConnection != null)
                {
                    if (_root.Connections == null) _root.Connections = new List<ConnectionData>();
                    var cd = _root.Connections.FirstOrDefault(c =>
                        c.FromNodeId == _selectedConnection.From.Node.Id && c.ToNodeId == _selectedConnection.To.Node.Id &&
                        (c.FromHandle ?? "out") == (_selectedConnection.FromHandle ?? "out"));
                    if (cd != null)
                    {
                        _root.Connections.Remove(cd);
                        _root.UseConnectionGraph = _root.Connections.Count > 0;
                        RebuildFromModel();
                        ModelChanged?.Invoke(_root);
                    }
                    _selectedConnection = null;
                }
            }
        }

        public void RebuildFromModel()
        {
            _nodes.Clear();
            _connections.Clear();
            _dragNode = null;
            _connectionDragFrom = null;
            _selectedConnection = null;
            if (_root == null) return;

            if (!_root.UseConnectionGraph)
            {
                ScriptNodeTree.BuildConnectionsFromTree(_root);
                var state = new LayoutState { X = 30, Y = 30 };
                ProcessNodes(_root.Children, state, null, _root.Children);
            }
            else
            {
                BuildFromConnectionGraph();
            }
            Invalidate();
        }

        private void BuildFromConnectionGraph()
        {
            var nodesById = new Dictionary<Guid, NodeViewModel>();
            foreach (var n in ScriptNodeTree.Flatten(_root))
            {
                var sz = GetNodeSize(n);
                var pos = n.Position;
                if (pos.X == 0 && pos.Y == 0)
                    pos = new PointF(30 + (nodesById.Count % 3) * 250, 30 + (nodesById.Count / 3) * 180);
                var vm = new NodeViewModel { Node = n, Bounds = new RectangleF(pos.X, pos.Y, sz.Width, sz.Height), ParentList = null, Index = -1 };
                _nodes.Add(vm);
                nodesById[n.Id] = vm;
            }
            foreach (var c in _root.Connections ?? new List<ConnectionData>())
            {
                if (nodesById.TryGetValue(c.FromNodeId, out var fromVm) && nodesById.TryGetValue(c.ToNodeId, out var toVm))
                    _connections.Add(new ConnectionViewModel { From = fromVm, To = toVm, FromHandle = c.FromHandle ?? "out", ToHandle = c.ToHandle ?? "in" });
            }
        }

        private SizeF GetNodeSize(ScriptNode node)
        {
            int rows = 2;
            bool hasFooter = false;
            if (node is SetNode) rows = 2;
            else if (node is ReadNode) rows = 1;
            else if (node is IfNode) { rows = 3; hasFooter = true; }
            else if (node is WhileNode) { rows = 3; hasFooter = true; }
            else if (node is VarDeclNode) rows = 2;
            else if (node is VarAssignNode) rows = 3;
            else if (node is SleepNode) rows = 1;
            else if (node is SleepMsNode) rows = 1;
            else if (node is SetModeNode) rows = 1;
            else if (node is ArmNode) rows = 1;
            else if (node is SendRcNode) rows = 2;
            else if (node is WaitForNode) rows = 2;
            float h = HeaderHeight + rows * FieldHeight + (hasFooter ? FooterHeight : 0) + NodePadding * 2;
            return new SizeF(BaseNodeWidth, h);
        }

        private NodeViewModel ProcessNodes(List<ScriptNode> children, LayoutState state, NodeViewModel prevOut, List<ScriptNode> parentList)
        {
            NodeViewModel lastOut = prevOut;
            for (int idx = 0; idx < children.Count; idx++)
            {
                var node = children[idx];
                var sz = GetNodeSize(node);
                float x = state.X, y = state.Y;
                if (node.Position.X != 0 || node.Position.Y != 0)
                {
                    x = node.Position.X;
                    y = node.Position.Y;
                }
                else
                {
                    node.Position = new PointF(x, y);
                }
                var vm = new NodeViewModel
                {
                    Node = node,
                    Bounds = new RectangleF(x, y, sz.Width, sz.Height),
                    ParentList = parentList,
                    Index = idx
                };
                _nodes.Add(vm);
                if (lastOut != null)
                    _connections.Add(new ConnectionViewModel { From = lastOut, To = vm, FromHandle = "out", ToHandle = "in" });

                state.Y += sz.Height + VerticalGap;

                if (node is IfNode iff)
                {
                    float branchX = state.X + BranchIndent;
                    var thenState = new LayoutState { X = branchX, Y = state.Y };
                    ProcessNodes(iff.ThenBranch, thenState, vm, iff.ThenBranch);
                    float maxY = thenState.Y;
                    if (iff.ElseBranch.Count > 0)
                    {
                        thenState.Y += VerticalGap;
                        var elseState = new LayoutState { X = branchX, Y = thenState.Y };
                        ProcessNodes(iff.ElseBranch, elseState, vm, iff.ElseBranch);
                        maxY = Math.Max(maxY, elseState.Y);
                    }
                    state.Y = maxY + VerticalGap;
                    lastOut = vm;
                }
                else if (node is WhileNode whilew)
                {
                    float branchX = state.X + BranchIndent;
                    var bodyState = new LayoutState { X = branchX, Y = state.Y };
                    ProcessNodes(whilew.Body, bodyState, vm, whilew.Body);
                    state.Y = bodyState.Y + VerticalGap;
                    lastOut = vm;
                }
                else
                {
                    lastOut = vm;
                }
            }
            return lastOut;
        }

        private class LayoutState { public float X, Y; }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.TranslateTransform(_panOffset.X, _panOffset.Y);
            g.ScaleTransform(_zoom, _zoom);

            DrawGrid(g);
            foreach (var conn in _connections)
            {
                if (conn.From == null || conn.To == null || conn.From == _dragNode || conn.To == _dragNode) continue;
                if (conn == _selectedConnection) continue;
                DrawConnection(g, conn);
            }
            if (_selectedConnection != null)
                DrawConnection(g, _selectedConnection);
            if (_connectionDragFrom != null)
            {
                var fromPt = GetHandlePosition(_connectionDragFrom, _connectionDragHandle);
                DrawConnectionLine(g, fromPt, _connectionDragTo);
            }
            foreach (var vm in _nodes)
            {
                if (vm != _dragNode)
                    DrawNode(g, vm);
            }
            if (_dragNode != null)
            {
                DrawNode(g, _dragNode);
            }

            if (_nodes.Count == 0)
            {
                g.ResetTransform();
                using (var font = new Font("Segoe UI", 12f, FontStyle.Regular))
                using (var brush = new SolidBrush(Color.FromArgb(100, 100, 110)))
                using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    var msg = "Правый клик — добавить узел\nСредняя кнопка мыши — панорамирование\nCtrl + Колёсико мыши — масштаб\nDelete — удалить выделенный узел";
                    g.DrawString(msg, font, brush, new RectangleF(0, 0, Width, Height), format);
                }
            }
        }

        private void DrawGrid(Graphics g)
        {
            int step = 25;
            using (var brush = new SolidBrush(Color.FromArgb(60, 60, 65)))
            {
                // Рисуем точечную сетку вместо линий для более чистого дизайна
                for (float x = -1000; x < 2500; x += step)
                {
                    for (float y = -1000; y < 4000; y += step)
                    {
                        g.FillRectangle(brush, x, y, 2, 2);
                    }
                }
            }
        }

        private PointF GetHandlePosition(NodeViewModel vm, string handle)
        {
            var r = vm.Bounds;
            float cx = r.X + r.Width / 2, cy = r.Y + r.Height / 2;
            if (handle == "out" || handle == "after") return new PointF(cx, r.Bottom);
            if (handle == "in") return new PointF(cx, r.Top);
            if (handle == "true") return new PointF(r.Left, cy);
            if (handle == "false") return new PointF(r.Right, cy);
            if (handle == "body") return new PointF(r.Left, cy);
            return new PointF(cx, r.Bottom);
        }

        private void DrawConnection(Graphics g, ConnectionViewModel conn)
        {
            var fromPt = GetHandlePosition(conn.From, conn.FromHandle ?? "out");
            var toPt = GetHandlePosition(conn.To, conn.ToHandle ?? "in");
            var isSelected = conn == _selectedConnection;
            using (var pen = new Pen(isSelected ? Color.FromArgb(255, 200, 80) : Color.FromArgb(90, 130, 180), isSelected ? 3.5f : 2.5f))
            {
                pen.CustomEndCap = new AdjustableArrowCap(5, 5, true);
                
                // Используем кривые Безье для красивых плавных линий соединений
                float tension = Math.Abs(toPt.Y - fromPt.Y) * 0.5f;
                tension = Math.Max(30, Math.Min(150, tension)); // Ограничиваем натяжение
                
                var pts = new[] { 
                    fromPt, 
                    new PointF(fromPt.X, fromPt.Y + tension), 
                    new PointF(toPt.X, toPt.Y - tension), 
                    toPt 
                };
                g.DrawBezier(pen, pts[0], pts[1], pts[2], pts[3]);
            }
        }

        private void DrawConnectionLine(Graphics g, PointF fromPt, PointF toPt)
        {
            using (var pen = new Pen(Color.FromArgb(120, 160, 200), 2.5f))
            {
                pen.CustomEndCap = new AdjustableArrowCap(5, 5, true);
                float tension = Math.Abs(toPt.Y - fromPt.Y) * 0.5f;
                tension = Math.Max(30, Math.Min(150, tension));
                
                var pts = new[] { 
                    fromPt, 
                    new PointF(fromPt.X, fromPt.Y + tension), 
                    new PointF(toPt.X, toPt.Y - tension), 
                    toPt 
                };
                g.DrawBezier(pen, pts[0], pts[1], pts[2], pts[3]);
            }
        }

        private void DrawNode(Graphics g, NodeViewModel vm)
        {
            var r = Rectangle.Round(vm.Bounds);
            bool selected = vm == _selectedNode;
            string title; Color c1, c2; string footer = null;
            var node = vm.Node;

            if (node is IfNode iff)
            {
                title = "If";
                c1 = Color.FromArgb(230, 120, 50);
                c2 = Color.FromArgb(200, 90, 40);
                footer = "Левая ручка = TRUE, правая = FALSE";
            }
            else if (node is WhileNode whilew)
            {
                title = "Loop";
                c1 = Color.FromArgb(70, 120, 180);
                c2 = Color.FromArgb(90, 70, 140);
                footer = "Левая ручка = ТЕЛО, правая = ПОСЛЕ";
            }
            else if (node is SetNode set)
            {
                title = "Set";
                c1 = Color.FromArgb(50, 120, 70);
                c2 = Color.FromArgb(40, 90, 55);
            }
            else if (node is ReadNode read)
            {
                title = "Read";
                c1 = Color.FromArgb(60, 70, 95);
                c2 = Color.FromArgb(50, 55, 75);
            }
            else if (node is VarDeclNode vd)
            {
                title = "Variable";
                c1 = Color.FromArgb(140, 80, 160);
                c2 = Color.FromArgb(180, 100, 140);
            }
            else if (node is VarAssignNode va)
            {
                title = "Operation";
                c1 = Color.FromArgb(55, 65, 80);
                c2 = Color.FromArgb(45, 52, 65);
            }
            else if (node is SleepNode sl)
            {
                title = "Sleep";
                c1 = Color.FromArgb(80, 70, 100);
                c2 = Color.FromArgb(60, 50, 80);
            }
            else if (node is SetModeNode sm)
            {
                title = "Mode";
                c1 = Color.FromArgb(100, 70, 50);
                c2 = Color.FromArgb(80, 55, 40);
            }
            else if (node is SleepMsNode) { title = "Sleep (ms)"; c1 = Color.FromArgb(70, 60, 90); c2 = Color.FromArgb(55, 45, 75); }
            else if (node is ArmNode arm) { title = arm.Arm ? "Arm" : "Disarm"; c1 = Color.FromArgb(60, 90, 60); c2 = Color.FromArgb(45, 70, 45); }
            else if (node is SendRcNode) { title = "SendRC"; c1 = Color.FromArgb(90, 60, 80); c2 = Color.FromArgb(70, 45, 60); }
            else if (node is WaitForNode) { title = "WaitFor"; c1 = Color.FromArgb(60, 80, 90); c2 = Color.FromArgb(45, 60, 70); }
            else { title = "?"; c1 = c2 = Color.Gray; }

            using (var path = CreateRoundedRect(r, CornerRadius))
            {
                // Тень ноды
                var shadowRect = r;
                shadowRect.Offset(3, 5);
                using (var shadowPath = CreateRoundedRect(shadowRect, CornerRadius))
                using (var shadowBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                {
                    g.FillPath(shadowBrush, shadowPath);
                }

                using (var grad = new LinearGradientBrush(r, c1, c2, 90f))
                    g.FillPath(grad, path);
                
                // Внутренний блик (glass effect)
                var innerRect = r;
                innerRect.Inflate(-1, -1);
                using (var innerPath = CreateRoundedRect(innerRect, CornerRadius - 1))
                using (var innerPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1f))
                {
                    g.DrawPath(innerPen, innerPath);
                }

                var borderColor = selected ? Color.FromArgb(255, 220, 120) : Color.FromArgb(40, 40, 50);
                if (selected)
                {
                    // Свечение для выделенной ноды
                    using (var glowPen = new Pen(Color.FromArgb(100, 255, 220, 120), 6f))
                        g.DrawPath(glowPen, path);
                }
                using (var p = new Pen(borderColor, selected ? 2.5f : 1.5f))
                    g.DrawPath(p, path);
            }

            float y = r.Y + NodePadding;
            using (var font = new Font("Segoe UI", 10f, FontStyle.Bold))
            using (var fontSmall = new Font("Segoe UI", 8.5f))
            using (var brush = new SolidBrush(Color.White))
            using (var brushDim = new SolidBrush(Color.FromArgb(220, 220, 220)))
            {
                g.DrawString(title, font, brush, r.X + NodePadding, y);
                y += HeaderHeight;

                if (node is IfNode iff2)
                {
                    DrawField(g, r, ref y, "Var", iff2.ParamName ?? "x", fontSmall, brush, brushDim);
                    DrawField(g, r, ref y, "Op", iff2.Operator ?? "==", fontSmall, brush, brushDim);
                    DrawField(g, r, ref y, "Value", (iff2.CompareVarName ?? iff2.CompareValue.ToString()) ?? "0", fontSmall, brush, brushDim);
                }
                else if (node is WhileNode w2)
                {
                    DrawField(g, r, ref y, "Var", w2.ParamName ?? "cycle", fontSmall, brush, brushDim);
                    DrawField(g, r, ref y, "Op", w2.Operator ?? "<", fontSmall, brush, brushDim);
                    DrawField(g, r, ref y, "Value", (w2.CompareVarName ?? w2.CompareValue.ToString()) ?? "5", fontSmall, brush, brushDim);
                }
                else if (node is SetNode set2)
                {
                    DrawField(g, r, ref y, "Param", set2.ParamName ?? "", fontSmall, brush, brushDim);
                    DrawField(g, r, ref y, "Value", set2.IsToggle ? "toggle" : (set2.Value?.ToString() ?? "0"), fontSmall, brush, brushDim);
                }
                else if (node is ReadNode read2)
                {
                    DrawField(g, r, ref y, "Param", read2.ParamName ?? "", fontSmall, brush, brushDim);
                }
                else if (node is VarDeclNode vd2)
                {
                    DrawField(g, r, ref y, "Name", vd2.VarName ?? "x", fontSmall, brush, brushDim);
                    DrawField(g, r, ref y, "Init", vd2.Value.ToString(), fontSmall, brush, brushDim);
                }
                else if (node is VarAssignNode va2)
                {
                    DrawField(g, r, ref y, "Res", va2.VarName ?? "x", fontSmall, brush, brushDim);
                    DrawField(g, r, ref y, "Op", va2.Op ?? "=", fontSmall, brush, brushDim);
                    DrawField(g, r, ref y, "Value", va2.Value?.ToString() ?? (va2.Op == "++" || va2.Op == "--" ? "-" : "0"), fontSmall, brush, brushDim);
                }
                else if (node is SleepNode sl2)
                {
                    DrawField(g, r, ref y, "Сек", sl2.Seconds.ToString(System.Globalization.CultureInfo.InvariantCulture), fontSmall, brush, brushDim);
                }
                else if (node is SetModeNode sm2)
                {
                    DrawField(g, r, ref y, "Режим", sm2.ModeNumber.ToString(), fontSmall, brush, brushDim);
                }
                else if (node is SleepMsNode slm2)
                {
                    DrawField(g, r, ref y, "Мс", slm2.Milliseconds.ToString(), fontSmall, brush, brushDim);
                }
                else if (node is ArmNode arm2)
                {
                    DrawField(g, r, ref y, "Действие", arm2.Arm ? "arm" : "disarm", fontSmall, brush, brushDim);
                }
                else if (node is SendRcNode src2)
                {
                    DrawField(g, r, ref y, "Канал", src2.Channel.ToString(), fontSmall, brush, brushDim);
                    DrawField(g, r, ref y, "PWM", src2.Pwm.ToString(), fontSmall, brush, brushDim);
                }
                else if (node is WaitForNode wf2)
                {
                    DrawField(g, r, ref y, "Подстрока", wf2.Substring ?? "", fontSmall, brush, brushDim);
                    DrawField(g, r, ref y, "Таймаут с", wf2.TimeoutSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture), fontSmall, brush, brushDim);
                }

                if (footer != null)
                {
                    y += 4;
                    g.DrawString(footer, fontSmall, brushDim, r.X + NodePadding, y);
                    y += FooterHeight;
                }
            }

            DrawHandles(g, vm);
        }

        private void DrawField(Graphics g, Rectangle r, ref float y, string label, string value, Font font, Brush brush, Brush brushDim)
        {
            g.DrawString(label + ":", font, brushDim, r.X + NodePadding, y + 5);
            var fieldRect = new RectangleF(r.X + 70, y + 2, r.Width - 80, FieldHeight - 4);
            var fieldR = Rectangle.Round(fieldRect);
            if (fieldR.Width > 0 && fieldR.Height > 0)
            {
                using (var path = CreateRoundedRect(fieldR, 6))
                {
                    using (var b = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                        g.FillPath(b, path);
                    using (var p = new Pen(Color.FromArgb(40, 255, 255, 255), 1f))
                        g.DrawPath(p, path);
                }
            }
            // Truncate long values
            string displayValue = value;
            var textSize = g.MeasureString(displayValue, font);
            if (textSize.Width > fieldRect.Width - 12)
            {
                while (displayValue.Length > 3 && g.MeasureString(displayValue + "...", font).Width > fieldRect.Width - 12)
                    displayValue = displayValue.Substring(0, displayValue.Length - 1);
                displayValue += "...";
                textSize = g.MeasureString(displayValue, font);
            }
            
            g.DrawString(displayValue, font, brush, fieldRect.X + 6, fieldRect.Y + (fieldRect.Height - textSize.Height) / 2);
            y += FieldHeight;
        }

        private void DrawHandles(Graphics g, NodeViewModel vm)
        {
            var r = vm.Bounds;
            float cx = r.X + r.Width / 2;
            float cy = r.Y + r.Height / 2;
            using (var b = new SolidBrush(Color.FromArgb(220, 220, 230)))
            using (var p = new Pen(Color.FromArgb(50, 50, 60), 1.5f))
            {
                g.FillEllipse(b, cx - HandleRadius, r.Y - HandleRadius, HandleRadius * 2, HandleRadius * 2);
                g.DrawEllipse(p, cx - HandleRadius, r.Y - HandleRadius, HandleRadius * 2, HandleRadius * 2);

                g.FillEllipse(b, cx - HandleRadius, r.Bottom - HandleRadius, HandleRadius * 2, HandleRadius * 2);
                g.DrawEllipse(p, cx - HandleRadius, r.Bottom - HandleRadius, HandleRadius * 2, HandleRadius * 2);

                if (vm.Node is IfNode || vm.Node is WhileNode)
                {
                    g.FillEllipse(b, r.X - HandleRadius, cy - HandleRadius, HandleRadius * 2, HandleRadius * 2);
                    g.DrawEllipse(p, r.X - HandleRadius, cy - HandleRadius, HandleRadius * 2, HandleRadius * 2);

                    g.FillEllipse(b, r.Right - HandleRadius, cy - HandleRadius, HandleRadius * 2, HandleRadius * 2);
                    g.DrawEllipse(p, r.Right - HandleRadius, cy - HandleRadius, HandleRadius * 2, HandleRadius * 2);
                }
            }
        }

        private string GetNodeText(ScriptNode node)
        {
            if (node is SetNode set)
                return set.IsToggle ? $"set {set.ParamName} toggle" : $"set {set.ParamName} {set.Value}";
            if (node is ReadNode read)
                return $"read {read.ParamName}";
            if (node is IfNode iff)
            {
                var right = iff.CompareVarName ?? iff.CompareValue.ToString();
                return $"if {iff.ParamName} {iff.Operator} {right}";
            }
            if (node is WhileNode whilew)
            {
                var right = whilew.CompareVarName ?? whilew.CompareValue.ToString();
                return $"while {whilew.ParamName} {whilew.Operator} {right}";
            }
            if (node is VarDeclNode vd)
                return $"var {vd.VarName} = {vd.Value}";
            if (node is VarAssignNode va)
            {
                if (va.Op == "++" || va.Op == "--") return $"{va.VarName}{va.Op}";
                if (va.SourceVarName != null && va.Value.HasValue)
                {
                    var op = (va.Op == ("+" + "=")) ? "+" : (va.Op == ("-" + "=")) ? "-" : (va.Op == ("*" + "=")) ? "*" : "/";
                    return $"{va.VarName} = {va.SourceVarName} {op} {va.Value}";
                }
                return va.Value.HasValue ? $"{va.VarName} {va.Op} {va.Value}" : $"{va.VarName} = 0";
            }
            if (node is SleepMsNode slm) return $"sleep_ms {slm.Milliseconds}";
            if (node is ArmNode arm) return arm.Arm ? "arm" : "disarm";
            if (node is SendRcNode src) return $"sendrc {src.Channel} {src.Pwm}";
            if (node is WaitForNode wf) return $"waitfor \"{wf.Substring}\" {wf.TimeoutSeconds}";
            return "?";
        }

        private static GraphicsPath CreateRoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(r.Right - radius * 2, r.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(r.Right - radius * 2, r.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(r.X, r.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            var pt = ToWorld(e.Location);
            var connHit = HitTestConnection(pt);
            var (handleVm, handleName) = HitTestHandle(pt);
            var hit = HitTest(pt);

            if (e.Button == MouseButtons.Right)
            {
                _selectedNode = hit;
                _selectedConnection = connHit;
                Invalidate();
                if (connHit != null)
                    ShowConnectionContextMenu(e.Location, connHit);
                else
                    ShowContextMenu(e.Location, hit);
                return;
            }

            if (e.Button == MouseButtons.Middle)
            {
                _isPanning = true;
                _lastMouse = e.Location;
                Cursor = Cursors.SizeAll;
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                if (handleVm != null && handleName != "in")
                {
                    _connectionDragFrom = handleVm;
                    _connectionDragHandle = handleName;
                    _connectionDragTo = pt;
                    _isPanning = false;
                }
                else if (hit != null)
                {
                    _selectedNode = hit;
                    _selectedConnection = null;
                    _isPanning = false;
                    _dragNode = hit;
                    _dragOffset = new PointF(pt.X - hit.Bounds.X, pt.Y - hit.Bounds.Y);
                }
                else
                {
                    _selectedNode = null;
                    _selectedConnection = null;
                    _dragNode = null;
                    _isPanning = true;
                    _lastMouse = e.Location;
                    Cursor = Cursors.SizeAll;
                }
            }
            Invalidate();
        }

        private void ShowContextMenu(Point screenPos, NodeViewModel hit)
        {
            _ctxMenu = new ContextMenuStrip();
            var targetList = (hit != null ? hit.ParentList : null) ?? _root.Children;
            var insertIndex = hit != null && hit.ParentList != null ? hit.Index + 1 : _root.Children.Count;
            var param = DefaultParamName ?? "SERVO1_REVERSED";

            _ctxMenu.Items.Add(new ToolStripMenuItem("Добавить set", null, (s, ev) => AddNode(targetList, insertIndex, new SetNode { ParamName = param, Value = 0 })));
            _ctxMenu.Items.Add(new ToolStripMenuItem("Добавить read", null, (s, ev) => AddNode(targetList, insertIndex, new ReadNode { ParamName = param })));
            _ctxMenu.Items.Add(new ToolStripMenuItem("Добавить var", null, (s, ev) => AddNode(targetList, insertIndex, new VarDeclNode { VarName = "cycle", Value = 1 })));
            _ctxMenu.Items.Add(new ToolStripMenuItem("Добавить присваивание", null, (s, ev) => AddNode(targetList, insertIndex, new VarAssignNode { VarName = "cycle", Op = "++", Value = null })));
            _ctxMenu.Items.Add(new ToolStripMenuItem("Добавить if", null, (s, ev) => AddNode(targetList, insertIndex, new IfNode { ParamName = param, Operator = "==", CompareValue = 0 })));
            _ctxMenu.Items.Add(new ToolStripMenuItem("Добавить while", null, (s, ev) => AddNode(targetList, insertIndex, new WhileNode { ParamName = "cycle", Operator = "<", CompareValue = 5 })));
            _ctxMenu.Items.Add(new ToolStripMenuItem("Добавить sleep", null, (s, ev) => AddNode(targetList, insertIndex, new SleepNode { Seconds = 1f })));
            _ctxMenu.Items.Add(new ToolStripMenuItem("Добавить sleep_ms", null, (s, ev) => AddNode(targetList, insertIndex, new SleepMsNode { Milliseconds = 1000 })));
            _ctxMenu.Items.Add(new ToolStripMenuItem("Добавить mode", null, (s, ev) => AddNode(targetList, insertIndex, new SetModeNode { ModeNumber = 5 })));
            _ctxMenu.Items.Add(new ToolStripMenuItem("Добавить arm", null, (s, ev) => AddNode(targetList, insertIndex, new ArmNode { Arm = true })));
            _ctxMenu.Items.Add(new ToolStripMenuItem("Добавить disarm", null, (s, ev) => AddNode(targetList, insertIndex, new ArmNode { Arm = false })));
            _ctxMenu.Items.Add(new ToolStripMenuItem("Добавить sendrc", null, (s, ev) => AddNode(targetList, insertIndex, new SendRcNode { Channel = 5, Pwm = 1500 })));
            _ctxMenu.Items.Add(new ToolStripMenuItem("Добавить waitfor", null, (s, ev) => AddNode(targetList, insertIndex, new WaitForNode { Substring = "Armed", TimeoutSeconds = 10f })));
            _ctxMenu.Items.Add(new ToolStripSeparator());

            if (hit != null)
            {
                if (hit.Node is IfNode iff)
                {
                    _ctxMenu.Items.Add(new ToolStripMenuItem("Добавить в ветку then", null, (s, ev) => AddNode(iff.ThenBranch, iff.ThenBranch.Count, new SetNode { ParamName = param, Value = 0 })));
                    _ctxMenu.Items.Add(new ToolStripMenuItem("Добавить в ветку else", null, (s, ev) => AddNode(iff.ElseBranch, iff.ElseBranch.Count, new SetNode { ParamName = param, Value = 0 })));
                    _ctxMenu.Items.Add(new ToolStripSeparator());
                }
                else if (hit.Node is WhileNode whilew)
                {
                    _ctxMenu.Items.Add(new ToolStripMenuItem("Добавить в тело цикла", null, (s, ev) => AddNode(whilew.Body, whilew.Body.Count, new SetNode { ParamName = param, Value = 0 })));
                    _ctxMenu.Items.Add(new ToolStripSeparator());
                }
                _ctxMenu.Items.Add(new ToolStripMenuItem("Изменить", null, (s, ev) => { if (EditNode(hit.Node)) { RebuildFromModel(); ModelChanged?.Invoke(_root); } }));
                _ctxMenu.Items.Add(new ToolStripMenuItem("Удалить", null, (s, ev) => DeleteNode(hit)));
            }
            else
            {
                _ctxMenu.Items.Add(new ToolStripMenuItem("Добавить в конец", null, (s, ev) => AddNode(_root.Children, _root.Children.Count, new SetNode { ParamName = param, Value = 0 })));
            }

            _ctxMenu.Show(this, screenPos);
        }

        private void AddNode(List<ScriptNode> list, int index, ScriptNode node)
        {
            list.Insert(Math.Min(index, list.Count), node);
            RebuildFromModel();
            ModelChanged?.Invoke(_root);
        }

        private void DeleteNode(NodeViewModel vm)
        {
            if (vm?.ParentList == null) return;
            vm.ParentList.Remove(vm.Node);
            RebuildFromModel();
            ModelChanged?.Invoke(_root);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_connectionDragFrom != null)
            {
                _connectionDragTo = ToWorld(e.Location);
                Invalidate();
            }
            else if (_dragNode != null && e.Button == MouseButtons.Left)
            {
                var pt = ToWorld(e.Location);
                var sz = _dragNode.Bounds.Size;
                _dragNode.Bounds = new RectangleF(pt.X - _dragOffset.X, pt.Y - _dragOffset.Y, sz.Width, sz.Height);
                Invalidate();
            }
            else if (_isPanning && e.Button == MouseButtons.Left)
            {
                _panOffset.X += e.X - _lastMouse.X;
                _panOffset.Y += e.Y - _lastMouse.Y;
                _lastMouse = e.Location;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            Cursor = Cursors.Default;
            
            if (e.Button == MouseButtons.Left && _connectionDragFrom != null)
            {
                var pt = ToWorld(e.Location);
                var (targetVm, targetHandle) = HitTestHandle(pt);
                if (targetVm != null && targetVm != _connectionDragFrom && targetHandle == "in")
                {
                    if (_root.Connections == null) _root.Connections = new List<ConnectionData>();
                    var fromId = _connectionDragFrom.Node.Id;
                    var fromH = _connectionDragHandle ?? "out";
                    var newConn = new ConnectionData { FromNodeId = fromId, FromHandle = fromH, ToNodeId = targetVm.Node.Id, ToHandle = "in" };
                    var updated = _connections
                        .Where(c => !(c.From.Node.Id == fromId && (c.FromHandle ?? "out") == fromH))
                        .Select(c => new ConnectionData { FromNodeId = c.From.Node.Id, FromHandle = c.FromHandle ?? "out", ToNodeId = c.To.Node.Id, ToHandle = c.ToHandle ?? "in" })
                        .ToList();
                    updated.Add(newConn);
                    _root.Connections = updated;
                    _root.UseConnectionGraph = true;
                    RebuildFromModel();
                    ModelChanged?.Invoke(_root);
                }
                _connectionDragFrom = null;
                Invalidate();
            }
            else if (e.Button == MouseButtons.Left && _dragNode != null)
            {
                _dragNode.Node.Position = new PointF(_dragNode.Bounds.X, _dragNode.Bounds.Y);
                _dragNode = null;
                Invalidate();
            }
            if (e.Button == MouseButtons.Left)
                _isPanning = false;
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (e.Button != MouseButtons.Left || _isPanning) return;
            var pt = ToWorld(e.Location);
            var hit = HitTest(pt);
            if (hit != null)
            {
                if (EditNode(hit.Node))
                {
                    RebuildFromModel();
                    ModelChanged?.Invoke(_root);
                }
            }
        }

        private bool EditNode(ScriptNode node)
        {
            if (node is SetNode set)
            {
                using (var dlg = new Form
                {
                    Text = "Редактировать set",
                    Size = new Size(400, 160),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent
                })
                {
                    var layout = new TableLayoutPanel
                    {
                        ColumnCount = 2,
                        RowCount = 4,
                        Dock = DockStyle.Fill,
                        Padding = new Padding(12, 12, 12, 12)
                    };
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                    var lblParam = new Label { Text = "Параметр:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 0) };
                    var txtParam = new TextBox { Text = set.ParamName, Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 0) };
                    var lblVal = new Label { Text = "Значение:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 0) };
                    var txtVal = new TextBox { Text = set.IsToggle ? "toggle" : set.Value?.ToString() ?? "0", Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 0) };
                    var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 85 };
                    var btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 85 };
                    layout.Controls.Add(lblParam, 0, 0);
                    layout.Controls.Add(txtParam, 1, 0);
                    layout.Controls.Add(lblVal, 0, 1);
                    layout.Controls.Add(txtVal, 1, 1);
                    var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
                    btnPanel.Controls.Add(btnCancel);
                    btnPanel.Controls.Add(new Panel { Width = 8 });
                    btnPanel.Controls.Add(btnOk);
                    layout.Controls.Add(btnPanel, 1, 3);
                    dlg.Controls.Add(layout);
                    dlg.AcceptButton = btnOk;
                    dlg.CancelButton = btnCancel;
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        set.ParamName = txtParam.Text?.Trim() ?? set.ParamName;
                        var v = txtVal.Text?.Trim();
                        set.IsToggle = string.Equals(v, "toggle", StringComparison.OrdinalIgnoreCase);
                        if (!set.IsToggle && float.TryParse(v?.Replace(',', '.'), out float f))
                            set.Value = f;
                        return true;
                    }
                }
            }
            else if (node is ReadNode read)
            {
                using (var dlg = new Form
                {
                    Text = "Редактировать read",
                    Size = new Size(400, 160),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent
                })
                {
                    var layout = new TableLayoutPanel
                    {
                        ColumnCount = 2,
                        RowCount = 3,
                        Dock = DockStyle.Fill,
                        Padding = new Padding(12, 12, 12, 12)
                    };
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                    var lblParam = new Label { Text = "Параметр:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 0) };
                    var txtParam = new TextBox { Text = read.ParamName, Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 0) };
                    var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 85 };
                    var btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 85 };
                    layout.Controls.Add(lblParam, 0, 0);
                    layout.Controls.Add(txtParam, 1, 0);
                    var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
                    btnPanel.Controls.Add(btnCancel);
                    btnPanel.Controls.Add(new Panel { Width = 8 });
                    btnPanel.Controls.Add(btnOk);
                    layout.Controls.Add(btnPanel, 1, 2);
                    dlg.Controls.Add(layout);
                    dlg.AcceptButton = btnOk;
                    dlg.CancelButton = btnCancel;
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        read.ParamName = txtParam.Text?.Trim() ?? read.ParamName;
                        return true;
                    }
                }
            }
            else if (node is SleepNode sl)
            {
                using (var dlg = new Form
                {
                    Text = "Редактировать sleep",
                    Size = new Size(400, 160),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent
                })
                {
                    var layout = new TableLayoutPanel
                    {
                        ColumnCount = 2,
                        RowCount = 3,
                        Dock = DockStyle.Fill,
                        Padding = new Padding(12, 12, 12, 12)
                    };
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                    var lblSec = new Label { Text = "Секунды:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 0) };
                    var txtSec = new TextBox { Text = sl.Seconds.ToString(System.Globalization.CultureInfo.InvariantCulture), Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 0) };
                    var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 85 };
                    var btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 85 };
                    layout.Controls.Add(lblSec, 0, 0);
                    layout.Controls.Add(txtSec, 1, 0);
                    var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
                    btnPanel.Controls.Add(btnCancel);
                    btnPanel.Controls.Add(new Panel { Width = 8 });
                    btnPanel.Controls.Add(btnOk);
                    layout.Controls.Add(btnPanel, 1, 2);
                    dlg.Controls.Add(layout);
                    dlg.AcceptButton = btnOk;
                    dlg.CancelButton = btnCancel;
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        if (float.TryParse(txtSec.Text?.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float f))
                            sl.Seconds = Math.Max(0, f);
                        return true;
                    }
                }
            }
            else if (node is SetModeNode sm)
            {
                using (var dlg = new Form
                {
                    Text = "Редактировать mode",
                    Size = new Size(420, 200),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent
                })
                {
                    var layout = new TableLayoutPanel
                    {
                        ColumnCount = 2,
                        RowCount = 3,
                        Dock = DockStyle.Fill,
                        Padding = new Padding(12, 12, 12, 12)
                    };
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                    var lblMode = new Label { Text = "Режим (0-17):", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 0) };
                    var txtMode = new TextBox { Text = sm.ModeNumber.ToString(), Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 0) };
                    var lblHint = new Label { Text = "0=Stabilize 5=Loiter 6=RTL 9=Land", AutoSize = true, ForeColor = Color.Gray, Margin = new Padding(0, 2, 0, 0) };
                    var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 85 };
                    var btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 85 };
                    layout.Controls.Add(lblMode, 0, 0);
                    layout.Controls.Add(txtMode, 1, 0);
                    layout.Controls.Add(lblHint, 1, 1);
                    var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
                    btnPanel.Controls.Add(btnCancel);
                    btnPanel.Controls.Add(new Panel { Width = 8 });
                    btnPanel.Controls.Add(btnOk);
                    layout.Controls.Add(btnPanel, 1, 2);
                    dlg.Controls.Add(layout);
                    dlg.AcceptButton = btnOk;
                    dlg.CancelButton = btnCancel;
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        var s = txtMode.Text?.Trim();
                        int num;
                        if (int.TryParse(s, out num) && num >= 0 && num <= 17)
                            sm.ModeNumber = num;
                        else
                            sm.ModeNumber = ScriptNodeTree.ResolveModeName(s ?? "");
                        return true;
                    }
                }
            }
            else if (node is SleepMsNode slm)
            {
                using (var dlg = new Form { Text = "Редактировать sleep_ms", Size = new Size(400, 140), FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent })
                {
                    var layout = new TableLayoutPanel { ColumnCount = 2, RowCount = 2, Dock = DockStyle.Fill, Padding = new Padding(12) };
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                    var lbl = new Label { Text = "Миллисекунды:", AutoSize = true, Margin = new Padding(0, 8, 8, 0) };
                    var txt = new TextBox { Text = slm.Milliseconds.ToString(), Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 0) };
                    var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 85 };
                    var btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 85 };
                    layout.Controls.Add(lbl, 0, 0); layout.Controls.Add(txt, 1, 0);
                    var pnl = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
                    pnl.Controls.Add(btnCancel); pnl.Controls.Add(new Panel { Width = 8 }); pnl.Controls.Add(btnOk);
                    layout.Controls.Add(pnl, 1, 1);
                    dlg.Controls.Add(layout); dlg.AcceptButton = btnOk; dlg.CancelButton = btnCancel;
                    if (dlg.ShowDialog() == DialogResult.OK && int.TryParse(txt.Text, out int ms) && ms >= 0)
                    { slm.Milliseconds = ms; return true; }
                }
            }
            else if (node is ArmNode arm)
            {
                using (var dlg = new Form { Text = "Редактировать arm/disarm", Size = new Size(400, 140), FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent })
                {
                    var layout = new TableLayoutPanel { ColumnCount = 2, RowCount = 2, Dock = DockStyle.Fill, Padding = new Padding(12) };
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                    var lbl = new Label { Text = "Действие:", AutoSize = true, Margin = new Padding(0, 8, 8, 0) };
                    var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, Margin = new Padding(0, 4, 0, 0) };
                    cmb.Items.AddRange(new object[] { "arm", "disarm" });
                    cmb.SelectedItem = arm.Arm ? "arm" : "disarm";
                    var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 85 };
                    var btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 85 };
                    layout.Controls.Add(lbl, 0, 0); layout.Controls.Add(cmb, 1, 0);
                    var pnl = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
                    pnl.Controls.Add(btnCancel); pnl.Controls.Add(new Panel { Width = 8 }); pnl.Controls.Add(btnOk);
                    layout.Controls.Add(pnl, 1, 1);
                    dlg.Controls.Add(layout); dlg.AcceptButton = btnOk; dlg.CancelButton = btnCancel;
                    if (dlg.ShowDialog() == DialogResult.OK)
                    { arm.Arm = (string)cmb.SelectedItem == "arm"; return true; }
                }
            }
            else if (node is SendRcNode src)
            {
                using (var dlg = new Form { Text = "Редактировать sendrc", Size = new Size(420, 180), FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent })
                {
                    var layout = new TableLayoutPanel { ColumnCount = 2, RowCount = 4, Dock = DockStyle.Fill, Padding = new Padding(12) };
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                    var lblCh = new Label { Text = "Канал (1-8):", AutoSize = true, Margin = new Padding(0, 8, 8, 0) };
                    var txtCh = new TextBox { Text = src.Channel.ToString(), Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 0) };
                    var lblPwm = new Label { Text = "PWM (1000-2000):", AutoSize = true, Margin = new Padding(0, 8, 8, 0) };
                    var txtPwm = new TextBox { Text = src.Pwm.ToString(), Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 0) };
                    var lblHint = new Label { Text = "Канал 5 часто используется для переключения режимов", AutoSize = true, ForeColor = Color.Gray };
                    var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 85 };
                    var btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 85 };
                    layout.Controls.Add(lblCh, 0, 0); layout.Controls.Add(txtCh, 1, 0);
                    layout.Controls.Add(lblPwm, 0, 1); layout.Controls.Add(txtPwm, 1, 1);
                    layout.Controls.Add(lblHint, 1, 2);
                    var pnl = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
                    pnl.Controls.Add(btnCancel); pnl.Controls.Add(new Panel { Width = 8 }); pnl.Controls.Add(btnOk);
                    layout.Controls.Add(pnl, 1, 3);
                    dlg.Controls.Add(layout); dlg.AcceptButton = btnOk; dlg.CancelButton = btnCancel;
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        if (int.TryParse(txtCh.Text, out int ch) && int.TryParse(txtPwm.Text, out int pwm))
                        { src.Channel = Math.Max(1, Math.Min(8, ch)); src.Pwm = (ushort)Math.Max(1000, Math.Min(2000, pwm)); return true; }
                    }
                }
            }
            else if (node is WaitForNode wf)
            {
                using (var dlg = new Form { Text = "Редактировать waitfor", Size = new Size(450, 180), FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent })
                {
                    var layout = new TableLayoutPanel { ColumnCount = 2, RowCount = 4, Dock = DockStyle.Fill, Padding = new Padding(12) };
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                    var lblSub = new Label { Text = "Подстрока:", AutoSize = true, Margin = new Padding(0, 8, 8, 0) };
                    var txtSub = new TextBox { Text = wf.Substring ?? "", Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 0) };
                    var lblTimeout = new Label { Text = "Таймаут (сек):", AutoSize = true, Margin = new Padding(0, 8, 8, 0) };
                    var txtTimeout = new TextBox { Text = wf.TimeoutSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture), Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 0) };
                    var lblHint = new Label { Text = "Ожидание STATUSTEXT с подстрокой (например Armed, Disarmed)", AutoSize = true, ForeColor = Color.Gray };
                    var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 85 };
                    var btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 85 };
                    layout.Controls.Add(lblSub, 0, 0); layout.Controls.Add(txtSub, 1, 0);
                    layout.Controls.Add(lblTimeout, 0, 1); layout.Controls.Add(txtTimeout, 1, 1);
                    layout.Controls.Add(lblHint, 1, 2);
                    var pnl = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
                    pnl.Controls.Add(btnCancel); pnl.Controls.Add(new Panel { Width = 8 }); pnl.Controls.Add(btnOk);
                    layout.Controls.Add(pnl, 1, 3);
                    dlg.Controls.Add(layout); dlg.AcceptButton = btnOk; dlg.CancelButton = btnCancel;
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        wf.Substring = txtSub.Text ?? "";
                        if (float.TryParse(txtTimeout.Text?.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float t))
                            wf.TimeoutSeconds = Math.Max(0, t);
                        return true;
                    }
                }
            }
            else if (node is IfNode iff)
            {
                using (var dlg = new Form
                {
                    Text = "Редактировать if",
                    Size = new Size(420, 200),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent
                })
                {
                    var layout = new TableLayoutPanel
                    {
                        ColumnCount = 2,
                        RowCount = 5,
                        Dock = DockStyle.Fill,
                        Padding = new Padding(12, 12, 12, 12)
                    };
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                    var lblParam = new Label { Text = "Параметр:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 0) };
                    var txtParam = new TextBox { Text = iff.ParamName, Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 0) };
                    var lblOp = new Label { Text = "Оператор:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 0) };
                    var cmbOp = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90, Margin = new Padding(0, 4, 0, 0) };
                    cmbOp.Items.AddRange(new object[] { "==", "!=", "<", ">", "<=", ">=" });
                    cmbOp.SelectedItem = iff.Operator ?? "==";
                    if (cmbOp.SelectedIndex < 0) cmbOp.SelectedIndex = 0;
                    var lblVal = new Label { Text = "Значение/переменная:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 0) };
                    var txtVal = new TextBox { Text = iff.CompareVarName ?? iff.CompareValue.ToString(), Width = 120, Margin = new Padding(0, 4, 0, 0) };
                    var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 85 };
                    var btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 85 };
                    layout.Controls.Add(lblParam, 0, 0);
                    layout.Controls.Add(txtParam, 1, 0);
                    layout.Controls.Add(lblOp, 0, 1);
                    layout.Controls.Add(cmbOp, 1, 1);
                    layout.Controls.Add(lblVal, 0, 2);
                    layout.Controls.Add(txtVal, 1, 2);
                    var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
                    btnPanel.Controls.Add(btnCancel);
                    btnPanel.Controls.Add(new Panel { Width = 8 });
                    btnPanel.Controls.Add(btnOk);
                    layout.Controls.Add(btnPanel, 1, 4);
                    dlg.Controls.Add(layout);
                    dlg.AcceptButton = btnOk;
                    dlg.CancelButton = btnCancel;
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        iff.ParamName = txtParam.Text?.Trim() ?? iff.ParamName;
                        iff.Operator = cmbOp.SelectedItem?.ToString() ?? "==";
                        var valStr = txtVal.Text?.Trim();
                        if (float.TryParse(valStr?.Replace(',', '.'), out float f))
                        {
                            iff.CompareValue = f;
                            iff.CompareVarName = null;
                        }
                        else if (!string.IsNullOrEmpty(valStr))
                        {
                            iff.CompareVarName = valStr;
                            iff.CompareValue = 0;
                        }
                        return true;
                    }
                }
            }
            else if (node is WhileNode whilew)
            {
                using (var dlg = new Form
                {
                    Text = "Редактировать while",
                    Size = new Size(420, 200),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent
                })
                {
                    var layout = new TableLayoutPanel
                    {
                        ColumnCount = 2,
                        RowCount = 5,
                        Dock = DockStyle.Fill,
                        Padding = new Padding(12, 12, 12, 12)
                    };
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                    var lblParam = new Label { Text = "Параметр:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 0) };
                    var txtParam = new TextBox { Text = whilew.ParamName, Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 0) };
                    var lblOp = new Label { Text = "Оператор:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 0) };
                    var cmbOp = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90, Margin = new Padding(0, 4, 0, 0) };
                    cmbOp.Items.AddRange(new object[] { "==", "!=", "<", ">", "<=", ">=" });
                    cmbOp.SelectedItem = whilew.Operator ?? "==";
                    if (cmbOp.SelectedIndex < 0) cmbOp.SelectedIndex = 0;
                    var lblVal = new Label { Text = "Значение/переменная:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 0) };
                    var txtVal = new TextBox { Text = whilew.CompareVarName ?? whilew.CompareValue.ToString(), Width = 120, Margin = new Padding(0, 4, 0, 0) };
                    var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 85 };
                    var btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 85 };
                    layout.Controls.Add(lblParam, 0, 0);
                    layout.Controls.Add(txtParam, 1, 0);
                    layout.Controls.Add(lblOp, 0, 1);
                    layout.Controls.Add(cmbOp, 1, 1);
                    layout.Controls.Add(lblVal, 0, 2);
                    layout.Controls.Add(txtVal, 1, 2);
                    var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
                    btnPanel.Controls.Add(btnCancel);
                    btnPanel.Controls.Add(new Panel { Width = 8 });
                    btnPanel.Controls.Add(btnOk);
                    layout.Controls.Add(btnPanel, 1, 4);
                    dlg.Controls.Add(layout);
                    dlg.AcceptButton = btnOk;
                    dlg.CancelButton = btnCancel;
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        whilew.ParamName = txtParam.Text?.Trim() ?? whilew.ParamName;
                        whilew.Operator = cmbOp.SelectedItem?.ToString() ?? "==";
                        var valStr = txtVal.Text?.Trim();
                        if (float.TryParse(valStr?.Replace(',', '.'), out float f))
                        {
                            whilew.CompareValue = f;
                            whilew.CompareVarName = null;
                        }
                        else if (!string.IsNullOrEmpty(valStr))
                        {
                            whilew.CompareVarName = valStr;
                            whilew.CompareValue = 0;
                        }
                        return true;
                    }
                }
            }
            else if (node is VarDeclNode vd)
            {
                using (var dlg = new Form
                {
                    Text = "Редактировать var",
                    Size = new Size(400, 160),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent
                })
                {
                    var layout = new TableLayoutPanel
                    {
                        ColumnCount = 2,
                        RowCount = 3,
                        Dock = DockStyle.Fill,
                        Padding = new Padding(12, 12, 12, 12)
                    };
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                    var lblName = new Label { Text = "Переменная:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 0) };
                    var txtName = new TextBox { Text = vd.VarName, Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 0) };
                    var lblVal = new Label { Text = "Значение:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 0) };
                    var txtVal = new TextBox { Text = vd.Value.ToString(), Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 0) };
                    var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 85 };
                    var btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 85 };
                    layout.Controls.Add(lblName, 0, 0);
                    layout.Controls.Add(txtName, 1, 0);
                    layout.Controls.Add(lblVal, 0, 1);
                    layout.Controls.Add(txtVal, 1, 1);
                    var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
                    btnPanel.Controls.Add(btnCancel);
                    btnPanel.Controls.Add(new Panel { Width = 8 });
                    btnPanel.Controls.Add(btnOk);
                    layout.Controls.Add(btnPanel, 1, 2);
                    dlg.Controls.Add(layout);
                    dlg.AcceptButton = btnOk;
                    dlg.CancelButton = btnCancel;
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        vd.VarName = txtName.Text?.Trim() ?? vd.VarName;
                        if (float.TryParse(txtVal.Text?.Replace(',', '.'), out float f))
                            vd.Value = f;
                        return true;
                    }
                }
            }
            else if (node is VarAssignNode va)
            {
                using (var dlg = new Form
                {
                    Text = "Редактировать присваивание",
                    Size = new Size(420, 200),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent
                })
                {
                    var layout = new TableLayoutPanel
                    {
                        ColumnCount = 2,
                        RowCount = 5,
                        Dock = DockStyle.Fill,
                        Padding = new Padding(12, 12, 12, 12)
                    };
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
                    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                    var lblVar = new Label { Text = "Переменная:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 0) };
                    var txtVar = new TextBox { Text = va.VarName, Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 0) };
                    var lblOp = new Label { Text = "Операция:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 0) };
                    var cmbOp = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90, Margin = new Padding(0, 4, 0, 0) };
                    cmbOp.Items.Add("=");
                    cmbOp.Items.Add("+" + "=");
                    cmbOp.Items.Add("-" + "=");
                    cmbOp.Items.Add("*" + "=");
                    cmbOp.Items.Add("/" + "=");
                    cmbOp.Items.Add("+" + "+");
                    cmbOp.Items.Add("-" + "-");
                    cmbOp.SelectedItem = va.Op ?? ("=");
                    if (cmbOp.SelectedIndex < 0) cmbOp.SelectedIndex = 0;
                    var lblVal = new Label { Text = "Значение:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 0) };
                    var txtVal = new TextBox { Text = va.Value?.ToString() ?? "0", Width = 120, Margin = new Padding(0, 4, 0, 0) };
                    layout.Controls.Add(lblVar, 0, 0);
                    layout.Controls.Add(txtVar, 1, 0);
                    layout.Controls.Add(lblOp, 0, 1);
                    layout.Controls.Add(cmbOp, 1, 1);
                    layout.Controls.Add(lblVal, 0, 2);
                    layout.Controls.Add(txtVal, 1, 2);
                    var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 85 };
                    var btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 85 };
                    var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
                    btnPanel.Controls.Add(btnCancel);
                    btnPanel.Controls.Add(new Panel { Width = 8 });
                    btnPanel.Controls.Add(btnOk);
                    layout.Controls.Add(btnPanel, 1, 4);
                    dlg.Controls.Add(layout);
                    dlg.AcceptButton = btnOk;
                    dlg.CancelButton = btnCancel;
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        va.VarName = txtVar.Text?.Trim() ?? va.VarName;
                        va.Op = cmbOp.SelectedItem?.ToString() ?? "=";
                        if (va.Op == ("+" + "+") || va.Op == ("-" + "-"))
                            va.Value = null;
                        else if (float.TryParse(txtVal.Text?.Replace(',', '.'), out float f))
                            va.Value = f;
                        return true;
                    }
                }
            }
            return false;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (ModifierKeys == Keys.Control)
            {
                _zoom *= e.Delta > 0 ? 1.1f : 0.9f;
                _zoom = Math.Max(0.3f, Math.Min(2f, _zoom));
                Invalidate();
            }
        }

        private PointF ToWorld(Point screen)
        {
            return new PointF((screen.X - _panOffset.X) / _zoom, (screen.Y - _panOffset.Y) / _zoom);
        }

        private (NodeViewModel vm, string handle) HitTestHandle(PointF pt)
        {
            float hitRadius = HandleRadius * 3;
            for (int i = _nodes.Count - 1; i >= 0; i--)
            {
                var vm = _nodes[i];
                if (vm == _dragNode) continue;
                var r = vm.Bounds;
                float cx = r.X + r.Width / 2, cy = r.Y + r.Height / 2;
                if (Distance(pt, new PointF(cx, r.Top)) <= hitRadius) return (vm, "in");
                if (Distance(pt, new PointF(cx, r.Bottom)) <= hitRadius) return (vm, "out");
                if (vm.Node is IfNode || vm.Node is WhileNode)
                {
                    if (Distance(pt, new PointF(r.Left, cy)) <= hitRadius) return (vm, vm.Node is IfNode ? "true" : "body");
                    if (Distance(pt, new PointF(r.Right, cy)) <= hitRadius) return (vm, vm.Node is IfNode ? "false" : "after");
                }
            }
            return (null, null);
        }

        private static float Distance(PointF a, PointF b)
        {
            float dx = a.X - b.X, dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private ConnectionViewModel HitTestConnection(PointF pt)
        {
            const float hitDist = 8;
            ConnectionViewModel best = null;
            float bestDist = float.MaxValue;
            foreach (var conn in _connections)
            {
                if (conn.From == null || conn.To == null) continue;
                var fromPt = GetHandlePosition(conn.From, conn.FromHandle ?? "out");
                var toPt = GetHandlePosition(conn.To, conn.ToHandle ?? "in");
                var midY = (fromPt.Y + toPt.Y) / 2;
                var pts = new[] { fromPt, new PointF(fromPt.X, midY), new PointF(toPt.X, midY), toPt };
                for (int i = 0; i < pts.Length - 1; i++)
                {
                    var d = DistanceToSegment(pt, pts[i], pts[i + 1]);
                    if (d < hitDist && d < bestDist) { bestDist = d; best = conn; }
                }
            }
            return best;
        }

        private static float DistanceToSegment(PointF p, PointF a, PointF b)
        {
            float dx = b.X - a.X, dy = b.Y - a.Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.001f) return Distance(p, a);
            float t = Math.Max(0, Math.Min(1, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (len * len)));
            var proj = new PointF(a.X + t * dx, a.Y + t * dy);
            return Distance(p, proj);
        }

        private void ShowConnectionContextMenu(Point screenPos, ConnectionViewModel conn)
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add(new ToolStripMenuItem("Удалить соединение", null, (s, ev) =>
            {
                if (_root.Connections == null) _root.Connections = new List<ConnectionData>();
                var cd = _root.Connections.FirstOrDefault(c =>
                    c.FromNodeId == conn.From.Node.Id && c.ToNodeId == conn.To.Node.Id &&
                    (c.FromHandle ?? "out") == (conn.FromHandle ?? "out"));
                if (cd != null)
                {
                    _root.Connections.Remove(cd);
                    _root.UseConnectionGraph = _root.Connections.Count > 0;
                    RebuildFromModel();
                    ModelChanged?.Invoke(_root);
                }
            }));
            menu.Show(this, screenPos);
        }

        private NodeViewModel HitTest(PointF pt)
        {
            for (int i = _nodes.Count - 1; i >= 0; i--)
            {
                var vm = _nodes[i];
                if (vm != _dragNode && vm.Bounds.Contains(pt))
                    return vm;
            }
            return null;
        }

        private class NodeViewModel
        {
            public ScriptNode Node { get; set; }
            public RectangleF Bounds { get; set; }
            public List<ScriptNode> ParentList { get; set; }
            public int Index { get; set; }
        }

        private class ConnectionViewModel
        {
            public NodeViewModel From { get; set; }
            public NodeViewModel To { get; set; }
            public string FromHandle { get; set; } = "out";
            public string ToHandle { get; set; } = "in";
        }
    }
}
