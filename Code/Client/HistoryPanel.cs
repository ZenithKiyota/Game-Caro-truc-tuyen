
// Giao diện lịch sử và kết quả trận đấu.
using System.Text.Json.Nodes;
using CaroOnline.Shared;

namespace CaroOnline.Client;

public sealed partial class MainForm
{
    private DataGridView? _historyTable;

    private Control BuildHistoryPanel()
    {
        var group = new GroupBox
        {
            Text = "Lịch sử trận (50 trận gần nhất)",
            Dock = DockStyle.Fill
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(6)
        };

        layout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 40));

        _historyTable = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            MultiSelect = false,
            SelectionMode =
                DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill
        };

        _historyTable.Columns.Add("room", "Phòng");
        _historyTable.Columns.Add("players", "X vs O");
        _historyTable.Columns.Add("winner", "Thắng");
        _historyTable.Columns.Add("reason", "Lý do");
        _historyTable.Columns.Add("endedAt", "Kết thúc");

        _historyTable.Columns[0].FillWeight = 12;
        _historyTable.Columns[1].FillWeight = 32;
        _historyTable.Columns[2].FillWeight = 18;
        _historyTable.Columns[3].FillWeight = 22;
        _historyTable.Columns[4].FillWeight = 24;

        var refreshButton = new Button
        {
            Text = "Tải lại lịch sử",
            Dock = DockStyle.Fill
        };

        refreshButton.Click += (_, _) =>
        {
            SendMessage(new JsonObject
            {
                ["type"] = Msg.History
            });
        };

        layout.Controls.Add(_historyTable, 0, 0);
        layout.Controls.Add(refreshButton, 0, 1);

        group.Controls.Add(layout);
        return group;
    }

    private void OnHistory(JsonObject msg)
    {
        if (_historyTable is null) return;

        _historyTable.Rows.Clear();

        if (msg["matches"] is not JsonArray matches)
            return;

        foreach (JsonNode? node in matches)
        {
            if (node is not JsonObject match)
                continue;

            string room =
                match["room"]?.ToString() ?? "";

            string x =
                match["x"]?.ToString() ?? "";

            string o =
                match["o"]?.ToString() ?? "";

            string winner =
                match["winner"]?.ToString() ?? "";

            string reason =
                match["reason"]?.ToString() ?? "";

            string endedAt =
                match["endedAt"]?.ToString() ?? "";

            _historyTable.Rows.Add(
                room,
                $"{x} vs {o}",
                winner,
                reason,
                endedAt
            );
        }
    }
}
