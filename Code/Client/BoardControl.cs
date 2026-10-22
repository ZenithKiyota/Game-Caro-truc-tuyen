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
        // TODO(Tiến): vẽ lưới Protocol.BoardSize x BoardSize, quân X/O, viền nước cuối.
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        // TODO(Tiến): đổi tọa độ chuột -> (row, col) rồi CellClicked?.Invoke(row, col).
    }
}
