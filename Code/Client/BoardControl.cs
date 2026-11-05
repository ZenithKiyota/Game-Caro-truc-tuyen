// Phụ trách: Hứa Hồng Tiến — vẽ bàn cờ 15x15, đánh dấu nước cuối, chuyển click chuột thành (row, col).
using CaroOnline.Shared;

namespace CaroOnline.Client;

public sealed class BoardControl : Control
{
    private string[] _board = Array.Empty<string>(); // 15 chuỗi gồm 'X', 'O', '.'
    private int _lastRow = -1, _lastCol = -1;

    public event Action<int, int>? CellClicked;

    public BoardControl()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    public void SetBoard(string[] board, int lastRow = -1, int lastCol = -1)
    {
        _board = board;
        _lastRow = lastRow;
        _lastCol = lastCol;
        Invalidate();
    }

    public void ResetBoard() => SetBoard(Array.Empty<string>());

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var size = Math.Min(ClientSize.Width, ClientSize.Height);
        if (size <= 0) return;

        const int padding = 18;
        var boardPixels = Math.Max(1, size - padding * 2);
        var cell = boardPixels / (float)(Protocol.BoardSize - 1);
        var left = (ClientSize.Width - cell * (Protocol.BoardSize - 1)) / 2f;
        var top = (ClientSize.Height - cell * (Protocol.BoardSize - 1)) / 2f;
        using var gridPen = new Pen(Color.SaddleBrown, 1f);
        using var xPen = new Pen(Color.Firebrick, Math.Max(2f, cell / 12f));
        using var oPen = new Pen(Color.RoyalBlue, Math.Max(2f, cell / 12f));
        using var lastPen = new Pen(Color.DarkGreen, 2f);

        for (var i = 0; i < Protocol.BoardSize; i++)
        {
            var x = left + i * cell;
            var y = top + i * cell;
            e.Graphics.DrawLine(gridPen, x, top, x, top + cell * (Protocol.BoardSize - 1));
            e.Graphics.DrawLine(gridPen, left, y, left + cell * (Protocol.BoardSize - 1), y);
        }

        var radius = Math.Max(4f, cell * 0.36f);
        for (var row = 0; row < Protocol.BoardSize; row++)
        for (var col = 0; col < Protocol.BoardSize; col++)
        {
            var value = row < _board.Length && _board[row].Length == Protocol.BoardSize
                ? _board[row][col] : '.';
            var centerX = left + col * cell;
            var centerY = top + row * cell;
            if (value == 'X')
            {
                e.Graphics.DrawLine(xPen, centerX - radius, centerY - radius, centerX + radius, centerY + radius);
                e.Graphics.DrawLine(xPen, centerX + radius, centerY - radius, centerX - radius, centerY + radius);
            }
            else if (value == 'O')
            {
                e.Graphics.DrawEllipse(oPen, centerX - radius, centerY - radius, radius * 2, radius * 2);
            }
        }

        if (_lastRow >= 0 && _lastRow < Protocol.BoardSize && _lastCol >= 0 && _lastCol < Protocol.BoardSize)
        {
            var centerX = left + _lastCol * cell;
            var centerY = top + _lastRow * cell;
            var mark = Math.Max(3f, cell * 0.46f);
            e.Graphics.DrawRectangle(lastPen, centerX - mark, centerY - mark, mark * 2, mark * 2);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;

        var size = Math.Min(ClientSize.Width, ClientSize.Height);
        const int padding = 18;
        var boardPixels = Math.Max(1, size - padding * 2);
        var cell = boardPixels / (float)(Protocol.BoardSize - 1);
        var left = (ClientSize.Width - cell * (Protocol.BoardSize - 1)) / 2f;
        var top = (ClientSize.Height - cell * (Protocol.BoardSize - 1)) / 2f;
        var col = (int)Math.Round((e.X - left) / cell);
        var row = (int)Math.Round((e.Y - top) / cell);
        if (row < 0 || row >= Protocol.BoardSize || col < 0 || col >= Protocol.BoardSize) return;

        var centerX = left + col * cell;
        var centerY = top + row * cell;
        if (Math.Abs(e.X - centerX) <= cell / 2f && Math.Abs(e.Y - centerY) <= cell / 2f)
            CellClicked?.Invoke(row, col);
    }
}
