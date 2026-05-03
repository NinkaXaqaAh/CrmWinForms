#nullable enable

using CrmApp.WinForms.Theming;

namespace CrmApp.WinForms.Forms.Customers;

partial class CustomerEditForm
{
    private System.ComponentModel.IContainer? components = null;

    private TableLayoutPanel _root = null!;

    private Label _typeLabel = null!;
    private ComboBox _typeCombo = null!;
    private Label _statusLabel = null!;
    private ComboBox _statusCombo = null!;

    private Label _nameLabel = null!;
    private TextBox _nameTextBox = null!;
    private Label _phoneLabel = null!;
    // Тип сменён с TextBox на MaskedTextBox: фиксированный формат "+7 (___) ___-__-__"
    // защищает от мусорного ввода и не заставляет пользователя самому набирать скобки.
    private MaskedTextBox _phoneTextBox = null!;
    private Label _emailLabel = null!;
    private TextBox _emailTextBox = null!;
    private Label _addressLabel = null!;
    private TextBox _addressTextBox = null!;
    private Label _notesLabel = null!;
    private TextBox _notesTextBox = null!;

    private GroupBox _companyGroup = null!;
    private TextBox _companyNameTextBox = null!;
    private TextBox _innTextBox = null!;
    private TextBox _positionTextBox = null!;

    private GroupBox _personGroup = null!;
    private CheckBox _hasBirthDateCheck = null!;
    private DateTimePicker _birthDatePicker = null!;

    private Panel _buttonsPanel = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;

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

        // -- Поля верха: Тип / Статус (горизонтально) --
        // FormattingEnabled=true нужно, чтобы сработал ComboBox.Format (см. CustomerEditForm.cs),
        // который локализует значения enum на экране.
        _typeLabel = MakeLabel("Тип клиента");
        _typeCombo = new ComboBox
        {
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FormattingEnabled = true,
        };

        _statusLabel = MakeLabel("Статус");
        _statusCombo = new ComboBox
        {
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FormattingEnabled = true,
        };

        var topPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10),
        };
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topPanel.Controls.Add(_typeLabel, 0, 0);
        topPanel.Controls.Add(_typeCombo, 1, 0);
        topPanel.Controls.Add(_statusLabel, 2, 0);
        topPanel.Controls.Add(_statusCombo, 3, 0);

        // -- Общие поля --
        _nameLabel = MakeLabel("ФИО (контактное лицо)");
        _nameTextBox = MakeTextBox();
        _phoneLabel = MakeLabel("Телефон");
        _phoneTextBox = new MaskedTextBox
        {
            Mask = "+7 (000) 000-00-00",
            PromptChar = '_',
            // Когда фокус не на поле, маска прячется — UX мягче.
            HidePromptOnLeave = true,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10F),
        };
        _emailLabel = MakeLabel("Email");
        _emailTextBox = MakeTextBox();
        _addressLabel = MakeLabel("Адрес");
        _addressTextBox = MakeTextBox();
        _notesLabel = MakeLabel("Заметки");
        _notesTextBox = MakeTextBox(multiline: true);

        // -- Группа юр.лица --
        _companyGroup = new GroupBox
        {
            Text = "Реквизиты компании",
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            Margin = new Padding(0, 6, 0, 6),
            Font = new Font("Segoe UI Semibold", 9.5F),
        };
        _companyNameTextBox = MakeTextBox();
        _companyNameTextBox.MaxLength = 200;
        // ИНН — 10 или 12 цифр (валидатор), здесь жёстко ограничиваем длину ввода.
        // KeyPress-фильтр на цифры подписывается в CustomerEditForm.cs.
        _innTextBox = MakeTextBox();
        _innTextBox.MaxLength = 12;
        _positionTextBox = MakeTextBox();
        _positionTextBox.MaxLength = 200;
        var companyLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, AutoSize = true,
        };
        companyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        companyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        companyLayout.Controls.Add(MakeLabel("Название"), 0, 0);
        companyLayout.Controls.Add(_companyNameTextBox, 1, 0);
        companyLayout.Controls.Add(MakeLabel("ИНН"), 0, 1);
        companyLayout.Controls.Add(_innTextBox, 1, 1);
        companyLayout.Controls.Add(MakeLabel("Должность контакта"), 0, 2);
        companyLayout.Controls.Add(_positionTextBox, 1, 2);
        _companyGroup.Controls.Add(companyLayout);

        // -- Группа физ.лица --
        _personGroup = new GroupBox
        {
            Text = "Личные данные",
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            Margin = new Padding(0, 6, 0, 6),
            Font = new Font("Segoe UI Semibold", 9.5F),
        };
        _hasBirthDateCheck = new CheckBox
        {
            Text = "Дата рождения известна",
            AutoSize = true,
            ForeColor = AppPalette.TextPrimary,
        };
        _birthDatePicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Width = 200,
            MaxDate = DateTime.Today,
            MinDate = new DateTime(1900, 1, 1),
        };
        var personLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight,
        };
        personLayout.Controls.Add(_hasBirthDateCheck);
        personLayout.Controls.Add(_birthDatePicker);
        _personGroup.Controls.Add(personLayout);

        // -- Кнопки --
        _saveButton = new Button
        {
            Text = "Сохранить",
            Size = new Size(140, 36),
            BackColor = AppPalette.Accent,
            ForeColor = AppPalette.AccentText,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.5F),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        _saveButton.FlatAppearance.BorderSize = 0;
        _saveButton.Click += OnSaveClick;

        _cancelButton = new Button
        {
            Text = "Отмена",
            Size = new Size(120, 36),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5F),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = AppPalette.Surface,
            ForeColor = AppPalette.TextPrimary,
        };
        _cancelButton.FlatAppearance.BorderColor = AppPalette.BorderMuted;
        _cancelButton.Click += OnCancelClick;

        _buttonsPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 10, 0, 0),
            Height = 50,
        };
        _buttonsPanel.Resize += (_, _) =>
        {
            _saveButton.Location = new Point(_buttonsPanel.Width - _saveButton.Width, 8);
            _cancelButton.Location = new Point(_buttonsPanel.Width - _saveButton.Width - _cancelButton.Width - 8, 8);
        };
        _buttonsPanel.Controls.Add(_saveButton);
        _buttonsPanel.Controls.Add(_cancelButton);

        // -- Корневая разметка --
        _root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(20),
            BackColor = AppPalette.Surface, AutoScroll = true,
        };
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Заполнение строк
        AddRow(topPanel, columnSpan: 2);
        AddRow(_nameLabel, _nameTextBox);
        AddRow(_phoneLabel, _phoneTextBox);
        AddRow(_emailLabel, _emailTextBox);
        AddRow(_addressLabel, _addressTextBox);
        AddRow(_notesLabel, _notesTextBox);
        AddRow(_companyGroup, columnSpan: 2);
        AddRow(_personGroup, columnSpan: 2);
        AddRow(_buttonsPanel, columnSpan: 2);

        Controls.Add(_root);

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        // Ограничения по длине: имя 200, email 200 — для согласованности с валидатором/UI-длиной.
        _nameTextBox.MaxLength = 200;
        _emailTextBox.MaxLength = 200;
        // Высота с запасом, потому что отображается одна из двух условных групп
        // (companyGroup / personGroup) — но не обе одновременно.
        ClientSize = new Size(660, 600);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Клиент";
        Font = new Font("Segoe UI", 9F);
        BackColor = AppPalette.Surface;
        ForeColor = AppPalette.TextPrimary;

        ResumeLayout(false);
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Margin = new Padding(0, 8, 10, 0),
        ForeColor = AppPalette.TextSecondary,
    };

    private static TextBox MakeTextBox(bool multiline = false) => new()
    {
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI", 10F),
        Multiline = multiline,
        Height = multiline ? 80 : 25,
        ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None,
    };

    private void AddRow(Control labelOrSpan, Control? right = null, int columnSpan = 1)
    {
        if (right is null)
        {
            // Однотогольный контрол на всю ширину
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.SetColumnSpan(labelOrSpan, columnSpan);
            _root.Controls.Add(labelOrSpan, 0, _root.RowCount);
            _root.RowCount++;
        }
        else
        {
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.Controls.Add(labelOrSpan, 0, _root.RowCount);
            _root.Controls.Add(right, 1, _root.RowCount);
            _root.RowCount++;
        }
    }
}
