using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace DocTree.Forms
{
    internal sealed class TextViewerControl : UserControl
    {
        private const int GutterHorizontalPadding = 8;
        private const int TextLeftPadding = 6;
        private const int TextRightPadding = 8;
        private const int ScrollbarSmallChange = 3;

        private readonly VScrollBar _verticalScrollBar;
        private readonly HScrollBar _horizontalScrollBar;
        private readonly List<VisualLine> _visualLines = new();
        private string[] _lines = [""];
        private bool _wordWrap;
        private int _lineHeight;
        private int _charWidth;
        private int _gutterWidth;
        private int _maxLineWidth;
        private bool _layoutUpdating;
        private bool _isSelecting;
        private bool _hasSelection;
        private TextPosition _selectionAnchor;
        private TextPosition _selectionCaret;

        public TextViewerControl()
        {
            _verticalScrollBar = new VScrollBar { Dock = DockStyle.Right };
            _horizontalScrollBar = new HScrollBar { Dock = DockStyle.Bottom };

            BorderStyle = BorderStyle.Fixed3D;
            BackColor = SystemColors.Window;
            ForeColor = SystemColors.WindowText;
            TabStop = true;

            Controls.Add(_verticalScrollBar);
            Controls.Add(_horizontalScrollBar);

            _verticalScrollBar.Scroll += (_, _) => Invalidate();
            _horizontalScrollBar.Scroll += (_, _) => Invalidate();

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        [AllowNull]
        public override Font Font
        {
            get => base.Font;
            set
            {
                base.Font = value;
                RecalculateLayout();
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string TextContent
        {
            get => string.Join(Environment.NewLine, _lines);
            set
            {
                _lines = SplitLines(value);
                RecalculateLayout();
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ReadOnly { get; set; } = true;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool WordWrap
        {
            get => _wordWrap;
            set
            {
                if (_wordWrap == value) return;
                _wordWrap = value;
                RecalculateLayout();
            }
        }

        public void SelectStart()
        {
            _verticalScrollBar.Value = 0;
            _horizontalScrollBar.Value = 0;
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            RecalculateLayout();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            var delta = e.Delta > 0 ? -ScrollbarSmallChange : ScrollbarSmallChange;
            SetScrollValue(_verticalScrollBar, _verticalScrollBar.Value + delta);
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            Focus();
            _isSelecting = true;
            Capture = true;
            _selectionAnchor = GetTextPositionFromPoint(e.Location);
            _selectionCaret = _selectionAnchor;
            _hasSelection = false;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_isSelecting)
            {
                return;
            }

            ScrollWhileSelecting(e.Location);
            _selectionCaret = GetTextPositionFromPoint(e.Location);
            _hasSelection = ComparePositions(_selectionAnchor, _selectionCaret) != 0;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            _isSelecting = false;
            Capture = false;
            _selectionCaret = GetTextPositionFromPoint(e.Location);
            _hasSelection = ComparePositions(_selectionAnchor, _selectionCaret) != 0;
            Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Control && e.KeyCode == Keys.C && _hasSelection)
            {
                Clipboard.SetText(GetSelectedText());
                e.Handled = true;
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Up:
                    SetScrollValue(_verticalScrollBar, _verticalScrollBar.Value - 1);
                    e.Handled = true;
                    break;
                case Keys.Down:
                    SetScrollValue(_verticalScrollBar, _verticalScrollBar.Value + 1);
                    e.Handled = true;
                    break;
                case Keys.PageUp:
                    SetScrollValue(_verticalScrollBar, _verticalScrollBar.Value - GetVisibleRowCount());
                    e.Handled = true;
                    break;
                case Keys.PageDown:
                    SetScrollValue(_verticalScrollBar, _verticalScrollBar.Value + GetVisibleRowCount());
                    e.Handled = true;
                    break;
                case Keys.Home:
                    SetScrollValue(_verticalScrollBar, 0);
                    e.Handled = true;
                    break;
                case Keys.End:
                    SetScrollValue(_verticalScrollBar, GetVerticalScrollMaximumValue());
                    e.Handled = true;
                    break;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var viewport = GetViewportRectangle();
            e.Graphics.SetClip(viewport);

            using var windowBrush = new SolidBrush(SystemColors.Window);
            e.Graphics.FillRectangle(windowBrush, viewport);

            var gutterRect = new Rectangle(viewport.Left, viewport.Top, _gutterWidth, viewport.Height);
            using var gutterBrush = new SolidBrush(SystemColors.Control);
            e.Graphics.FillRectangle(gutterBrush, gutterRect);
            using var divider = new Pen(SystemColors.ControlDark);
            e.Graphics.DrawLine(divider, gutterRect.Right - 1, gutterRect.Top, gutterRect.Right - 1, gutterRect.Bottom);

            var firstRow = _verticalScrollBar.Value;
            var visibleRows = GetVisibleRowCount();
            var textX = viewport.Left + _gutterWidth + TextLeftPadding -
                (_horizontalScrollBar.Visible ? _horizontalScrollBar.Value : 0);

            for (var visibleIndex = 0; visibleIndex < visibleRows; visibleIndex++)
            {
                var row = firstRow + visibleIndex;
                if (row >= _visualLines.Count)
                {
                    break;
                }

                var visualLine = _visualLines[row];
                var y = viewport.Top + (visibleIndex * _lineHeight);

                DrawSelectionForLine(e.Graphics, viewport, visualLine, y);

                if (!visualLine.IsContinuation)
                {
                    var lineNumberText = (visualLine.SourceLine + 1).ToString();
                    var lineNumberRect = new Rectangle(gutterRect.Left, y, _gutterWidth - GutterHorizontalPadding, _lineHeight);
                    TextRenderer.DrawText(e.Graphics, lineNumberText, Font, lineNumberRect, SystemColors.GrayText,
                        TextFormatFlags.Right | TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);
                }

                DrawVisualLineText(e.Graphics, viewport, visualLine, textX, y);
            }
        }

        private void DrawSelectionForLine(Graphics graphics, Rectangle viewport, VisualLine visualLine, int y)
        {
            if (!_hasSelection)
            {
                return;
            }

            var (selectionStart, selectionEnd) = GetNormalizedSelection();
            var lineIndex = visualLine.SourceLine;
            if (lineIndex < selectionStart.Line || lineIndex > selectionEnd.Line)
            {
                return;
            }

            var line = _lines[lineIndex];
            var lineSelectionStart = lineIndex == selectionStart.Line ? selectionStart.Column : 0;
            var lineSelectionEnd = lineIndex == selectionEnd.Line ? selectionEnd.Column : line.Length;
            var visualStart = visualLine.Start;
            var visualEnd = visualLine.Start + visualLine.Length;

            if (visualLine.Length == 0 && lineSelectionStart == 0 && lineSelectionEnd == 0 &&
                selectionStart.Line != selectionEnd.Line)
            {
                FillSelection(graphics, viewport, visualLine, 0, 1, y);
                return;
            }

            var start = Math.Max(lineSelectionStart, visualStart);
            var end = Math.Min(lineSelectionEnd, visualEnd);
            if (end <= start)
            {
                return;
            }

            FillSelection(graphics, viewport, visualLine, start, end, y);
        }

        private void FillSelection(Graphics graphics, Rectangle viewport, VisualLine visualLine, int startColumn, int endColumn, int y)
        {
            var line = _lines[visualLine.SourceLine];
            var horizontalOffset = _horizontalScrollBar.Visible ? _horizontalScrollBar.Value : 0;
            var textOriginX = viewport.Left + _gutterWidth + TextLeftPadding;
            var safeStartColumn = Math.Clamp(startColumn, 0, line.Length);
            var safeEndColumn = Math.Clamp(endColumn, safeStartColumn, line.Length);
            var x = textOriginX + MeasureTextWidth(line, visualLine.Start, safeStartColumn - visualLine.Start) - horizontalOffset;
            var width = safeEndColumn == safeStartColumn && line.Length == 0
                ? _charWidth
                : MeasureTextWidth(line, safeStartColumn, safeEndColumn - safeStartColumn);
            var rect = Rectangle.Intersect(new Rectangle(x, y, width, _lineHeight), viewport);

            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            using var brush = new SolidBrush(SystemColors.Highlight);
            graphics.FillRectangle(brush, rect);
        }

        private void RecalculateLayout()
        {
            if (_layoutUpdating || IsDisposed)
            {
                return;
            }

            _layoutUpdating = true;
            try
            {
                using var graphics = CreateGraphics();
                var size = TextRenderer.MeasureText(graphics, "M", Font, Size.Empty, TextFormatFlags.NoPadding);
                _charWidth = Math.Max(1, size.Width);
                _lineHeight = Math.Max(1, Font.Height);
                _maxLineWidth = _lines.Length == 0 ? 0 : _lines.Max(line => MeasureTextWidth(line));
                _gutterWidth = Math.Max(36, (_lines.Length.ToString().Length * _charWidth) + (GutterHorizontalPadding * 2));

                BuildVisualLines();
                UpdateScrollBars();
                Invalidate();
            }
            finally
            {
                _layoutUpdating = false;
            }
        }

        private void BuildVisualLines()
        {
            _visualLines.Clear();

            var viewport = GetViewportRectangle();
            var textWidth = Math.Max(_charWidth, viewport.Width - _gutterWidth - TextLeftPadding - TextRightPadding);

            for (var lineIndex = 0; lineIndex < _lines.Length; lineIndex++)
            {
                var line = _lines[lineIndex];
                if (!_wordWrap || MeasureTextWidth(line) <= textWidth)
                {
                    _visualLines.Add(new VisualLine(lineIndex, 0, line.Length, false));
                    continue;
                }

                var start = 0;
                while (start < line.Length)
                {
                    var length = GetWrapLength(line, start, textWidth);
                    _visualLines.Add(new VisualLine(lineIndex, start, length, start > 0));
                    start += length;
                }
            }
        }

        private void UpdateScrollBars()
        {
            var visibleRows = GetVisibleRowCount();
            ConfigureScrollBar(_verticalScrollBar, _visualLines.Count, visibleRows);

            var viewport = GetViewportRectangle();
            var visibleWidth = Math.Max(1, viewport.Width - _gutterWidth - TextLeftPadding - TextRightPadding);
            var needsHorizontalScroll = !_wordWrap && _maxLineWidth > visibleWidth;
            _horizontalScrollBar.Visible = needsHorizontalScroll;
            ConfigureScrollBar(_horizontalScrollBar, needsHorizontalScroll ? _maxLineWidth : 0, visibleWidth);
        }

        private Rectangle GetViewportRectangle()
        {
            var width = ClientSize.Width - (_verticalScrollBar.Visible ? _verticalScrollBar.Width : 0);
            var height = ClientSize.Height - (_horizontalScrollBar.Visible ? _horizontalScrollBar.Height : 0);
            return new Rectangle(0, 0, Math.Max(0, width), Math.Max(0, height));
        }

        private int GetVisibleRowCount()
        {
            var viewport = GetViewportRectangle();
            return Math.Max(1, viewport.Height / Math.Max(1, _lineHeight));
        }

        private int GetVerticalScrollMaximumValue()
        {
            return GetMaximumScrollValue(_verticalScrollBar);
        }

        private void DrawVisualLineText(Graphics graphics, Rectangle viewport, VisualLine visualLine, int textX, int y)
        {
            var line = _lines[visualLine.SourceLine];
            var x = textX;
            var (selectionStart, selectionEnd) = _hasSelection
                ? GetNormalizedSelection()
                : (new TextPosition(-1, 0), new TextPosition(-1, 0));

            for (var index = visualLine.Start; index < visualLine.Start + visualLine.Length; index++)
            {
                var characterWidth = GetCharacterWidth(line[index]);
                if (x + characterWidth >= viewport.Left && x <= viewport.Right)
                {
                    var selected = _hasSelection &&
                        ComparePositions(new TextPosition(visualLine.SourceLine, index), selectionStart) >= 0 &&
                        ComparePositions(new TextPosition(visualLine.SourceLine, index + 1), selectionEnd) <= 0;
                    var color = selected ? SystemColors.HighlightText : ForeColor;
                    var displayText = line[index] == '\t' ? "    " : line[index].ToString();
                    var rect = new Rectangle(x, y, characterWidth, _lineHeight);
                    TextRenderer.DrawText(graphics, displayText, Font, rect, color,
                        TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.NoClipping | TextFormatFlags.VerticalCenter);
                }

                x += characterWidth;
            }
        }

        private TextPosition GetTextPositionFromPoint(Point point)
        {
            if (_visualLines.Count == 0)
            {
                return new TextPosition(0, 0);
            }

            var viewport = GetViewportRectangle();
            var rowOffset = Math.Clamp((point.Y - viewport.Top) / Math.Max(1, _lineHeight), 0, GetVisibleRowCount() - 1);
            var row = Math.Clamp(_verticalScrollBar.Value + rowOffset, 0, _visualLines.Count - 1);
            var visualLine = _visualLines[row];
            var horizontalOffset = _horizontalScrollBar.Visible ? _horizontalScrollBar.Value : 0;
            var textOriginX = viewport.Left + _gutterWidth + TextLeftPadding;
            var x = Math.Max(0, point.X - textOriginX + horizontalOffset);
            var visualColumn = GetColumnFromX(_lines[visualLine.SourceLine], visualLine.Start, visualLine.Length, x);
            var lineLength = _lines[visualLine.SourceLine].Length;
            var column = Math.Clamp(visualLine.Start + visualColumn, 0, lineLength);

            return new TextPosition(visualLine.SourceLine, column);
        }

        private void ScrollWhileSelecting(Point point)
        {
            var viewport = GetViewportRectangle();
            if (point.Y < viewport.Top)
            {
                SetScrollValue(_verticalScrollBar, _verticalScrollBar.Value - 1);
            }
            else if (point.Y >= viewport.Bottom)
            {
                SetScrollValue(_verticalScrollBar, _verticalScrollBar.Value + 1);
            }

            if (!_wordWrap)
            {
                if (point.X < viewport.Left + _gutterWidth)
                {
                    SetScrollValue(_horizontalScrollBar, _horizontalScrollBar.Value - 1);
                }
                else if (point.X >= viewport.Right)
                {
                    SetScrollValue(_horizontalScrollBar, _horizontalScrollBar.Value + 1);
                }
            }
        }

        private string GetSelectedText()
        {
            var (start, end) = GetNormalizedSelection();
            if (ComparePositions(start, end) == 0)
            {
                return "";
            }

            if (start.Line == end.Line)
            {
                return _lines[start.Line].Substring(start.Column, end.Column - start.Column);
            }

            var selectedLines = new List<string>
            {
                _lines[start.Line][start.Column..]
            };

            for (var line = start.Line + 1; line < end.Line; line++)
            {
                selectedLines.Add(_lines[line]);
            }

            selectedLines.Add(_lines[end.Line][..end.Column]);
            return string.Join(Environment.NewLine, selectedLines);
        }

        private (TextPosition Start, TextPosition End) GetNormalizedSelection()
        {
            return ComparePositions(_selectionAnchor, _selectionCaret) <= 0
                ? (_selectionAnchor, _selectionCaret)
                : (_selectionCaret, _selectionAnchor);
        }

        private static int ComparePositions(TextPosition left, TextPosition right)
        {
            var lineCompare = left.Line.CompareTo(right.Line);
            return lineCompare != 0 ? lineCompare : left.Column.CompareTo(right.Column);
        }

        private static string[] SplitLines(string text)
        {
            return text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        }

        private static void ConfigureScrollBar(ScrollBar scrollBar, int totalUnits, int visibleUnits)
        {
            scrollBar.SmallChange = ScrollbarSmallChange;
            scrollBar.LargeChange = Math.Max(1, visibleUnits);
            scrollBar.Minimum = 0;
            scrollBar.Maximum = Math.Max(0, totalUnits - 1);
            SetScrollValue(scrollBar, scrollBar.Value);
        }

        private static void SetScrollValue(ScrollBar scrollBar, int value)
        {
            scrollBar.Value = Math.Clamp(value, scrollBar.Minimum, GetMaximumScrollValue(scrollBar));
        }

        private static int GetMaximumScrollValue(ScrollBar scrollBar)
        {
            return Math.Max(scrollBar.Minimum, scrollBar.Maximum - scrollBar.LargeChange + 1);
        }

        private int GetWrapLength(string line, int start, int maxWidth)
        {
            var width = 0;
            for (var index = start; index < line.Length; index++)
            {
                var charWidth = MeasureTextWidth(line, index, 1);
                if (index > start && width + charWidth > maxWidth)
                {
                    return index - start;
                }

                width += charWidth;
            }

            return line.Length - start;
        }

        private int GetColumnFromX(string line, int start, int length, int x)
        {
            var width = 0;
            for (var offset = 0; offset < length; offset++)
            {
                var charWidth = MeasureTextWidth(line, start + offset, 1);
                if (x < width + (charWidth / 2))
                {
                    return offset;
                }

                width += charWidth;
            }

            return length;
        }

        private int MeasureTextWidth(string text)
        {
            return MeasureTextWidth(text, 0, text.Length);
        }

        private int MeasureTextWidth(string text, int start, int length)
        {
            if (length <= 0 || start >= text.Length)
            {
                return 0;
            }

            start = Math.Max(0, start);
            var end = Math.Min(text.Length, start + length);
            var width = 0;
            for (var index = start; index < end; index++)
            {
                width += GetCharacterWidth(text[index]);
            }

            return width;
        }

        private int GetCharacterWidth(char character)
        {
            if (character == '\t')
            {
                return _charWidth * 4;
            }

            return IsWideCharacter(character) ? _charWidth * 2 : _charWidth;
        }

        private static bool IsWideCharacter(char character)
        {
            return character >= 0x1100 &&
                (character <= 0x115F ||
                 character == 0x2329 ||
                 character == 0x232A ||
                 (character >= 0x2E80 && character <= 0xA4CF && character != 0x303F) ||
                 (character >= 0xAC00 && character <= 0xD7A3) ||
                 (character >= 0xF900 && character <= 0xFAFF) ||
                 (character >= 0xFE10 && character <= 0xFE19) ||
                 (character >= 0xFE30 && character <= 0xFE6F) ||
                 (character >= 0xFF00 && character <= 0xFF60) ||
                 (character >= 0xFFE0 && character <= 0xFFE6));
        }

        private readonly record struct VisualLine(int SourceLine, int Start, int Length, bool IsContinuation);

        private readonly record struct TextPosition(int Line, int Column);
    }
}
