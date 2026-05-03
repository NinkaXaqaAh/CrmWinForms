#nullable enable

using CrmApp.WinForms.Theming;

namespace CrmApp.WinForms.Forms.Users;

partial class UserListForm
{
    private System.ComponentModel.IContainer? components = null;

    private TableLayoutPanel _root = null!;
    private Panel _toolbar = null!;
    private Button _exportButton = null!;
    private Button _addButton = null!;
    private Button _editButton = null!;
    private Button _deleteButton = null!;
    private DataGridView _grid = null!;
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _statusLabel = null!;

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
            ColumnCount = 1, RowCount = 3,
            Padding = new Padding(10),
            BackColor = AppPalette.WindowBackground,
        };
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        _toolbar = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

        _exportButton = MakeBtn("Экспорт в Excel", 0, OnExportClick, accent: false);
        _addButton = MakeBtn("Добавить", 0, OnAddClick, accent: true);
        _editButton = MakeBtn("Редактировать", 0, OnEditClick, accent: false);
        _deleteButton = MakeBtn("Удалить", 0, OnDeleteClick, accent: false);
        _deleteButton.ForeColor = AppPalette.Danger;

        _toolbar.Controls.AddRange(new Control[]
        {
            _exportButton, _addButton, _editButton, _deleteButton,
        });
        _toolbar.Resize += (_, _) =>
        {
            var x = _toolbar.Width;
            x -= _deleteButton.Width + 5; _deleteButton.Location = new Point(x, 12);
            x -= _editButton.Width + 5; _editButton.Location = new Point(x, 12);
            x -= _addButton.Width + 5; _addButton.Location = new Point(x, 12);
            x -= _exportButton.Width + 5; _exportButton.Location = new Point(x, 12);
        };

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = AppPalette.Surface,
            ForeColor = AppPalette.TextPrimary,
            GridColor = AppPalette.Border,
            BorderStyle = BorderStyle.FixedSingle,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            Font = new Font("Segoe UI", 9.5F),
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppPalette.SurfaceMuted,
                ForeColor = AppPalette.TextPrimary,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Padding = new Padding(6, 4, 6, 4),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
            },
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 36,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppPalette.Surface,
                ForeColor = AppPalette.TextPrimary,
                Padding = new Padding(6, 0, 6, 0),
                SelectionBackColor = AppPalette.SelectionBackground,
                SelectionForeColor = AppPalette.SelectionForeground,
            },
            RowTemplate = { Height = 32 },
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
        };

        _statusStrip = new StatusStrip
        {
            Dock = DockStyle.Fill,
            BackColor = AppPalette.SurfaceMuted,
            ForeColor = AppPalette.TextPrimary,
        };
        _statusLabel = new ToolStripStatusLabel("Загрузка...")
        {
            ForeColor = AppPalette.TextPrimary,
        };
        _statusStrip.Items.Add(_statusLabel);

        _root.Controls.Add(_toolbar, 0, 0);
        _root.Controls.Add(_grid, 0, 1);
        _root.Controls.Add(_statusStrip, 0, 2);

        Controls.Add(_root);

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1100, 700);
        WindowState = FormWindowState.Maximized;
        Text = "Пользователи";
        Font = new Font("Segoe UI", 9F);
        BackColor = AppPalette.WindowBackground;

        ResumeLayout(false);
    }

    private static Button MakeBtn(string text, int x, EventHandler handler, bool accent)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, 10),
            Size = new Size(140, 32),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5F),
            BackColor = accent ? AppPalette.Accent : AppPalette.Surface,
            ForeColor = accent ? AppPalette.AccentText : AppPalette.TextPrimary,
        };
        btn.FlatAppearance.BorderColor = accent ? AppPalette.Accent : AppPalette.BorderMuted;
        btn.Click += handler;
        return btn;
    }
}
