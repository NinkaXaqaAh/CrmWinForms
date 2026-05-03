#nullable enable

using CrmApp.WinForms.Theming;

namespace CrmApp.WinForms.Forms.Search;

partial class SearchDialog
{
    private System.ComponentModel.IContainer? components = null;

    private TableLayoutPanel _root = null!;
    private Label _hintLabel = null!;
    private TextBox _searchTextBox = null!;
    private ListView _resultsList = null!;
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _statusLabel = null!;
    private Panel _buttonsPanel = null!;
    private Button _closeButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        _root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(15),
            BackColor = AppPalette.Surface,
        };
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

        _hintLabel = new Label
        {
            Text = "Поиск по клиентам, сделкам, активностям и товарам",
            AutoSize = true,
            Font = new Font("Segoe UI", 9F),
            ForeColor = AppPalette.Muted,
        };

        _searchTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 13F),
            PlaceholderText = "Что ищем?",
        };

        _resultsList = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false,
            HideSelection = false,
            GridLines = false,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            Font = new Font("Segoe UI", 10F),
            BorderStyle = BorderStyle.FixedSingle,
        };
        _resultsList.Columns.Add("Тип", 110);
        _resultsList.Columns.Add("Название", 320);
        _resultsList.Columns.Add("Детали", 240);

        _statusStrip = new StatusStrip { Dock = DockStyle.Fill };
        _statusLabel = new ToolStripStatusLabel("Начните вводить запрос…");
        _statusStrip.Items.Add(_statusLabel);

        _closeButton = new Button
        {
            Text = "Закрыть",
            Size = new Size(120, 36),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5F),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        _closeButton.FlatAppearance.BorderColor = AppPalette.BorderMuted;
        _closeButton.Click += OnCloseClick;

        _buttonsPanel = new Panel { Dock = DockStyle.Fill };
        _buttonsPanel.Controls.Add(_closeButton);
        _buttonsPanel.Resize += (_, _) =>
            _closeButton.Location = new Point(_buttonsPanel.Width - _closeButton.Width, 8);

        _root.Controls.Add(_hintLabel, 0, 0);
        _root.Controls.Add(_searchTextBox, 0, 1);
        _root.Controls.Add(_resultsList, 0, 2);
        _root.Controls.Add(_statusStrip, 0, 3);
        _root.Controls.Add(_buttonsPanel, 0, 4);

        Controls.Add(_root);

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(720, 520);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Глобальный поиск";
        Font = new Font("Segoe UI", 9F);
        BackColor = AppPalette.Surface;
        ShowInTaskbar = false;

        ResumeLayout(false);
    }
}
