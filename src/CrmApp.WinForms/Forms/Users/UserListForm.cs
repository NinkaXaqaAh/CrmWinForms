using System.ComponentModel;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using CrmApp.Core.Abstractions;
using CrmApp.Core.Enums;
using CrmApp.Core.Models;
using CrmApp.WinForms.Controls;
using CrmApp.WinForms.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CrmApp.WinForms.Forms.Users;

// Список пользователей системы — доступ только для Admin (контроль на уровне меню MainForm).
public partial class UserListForm : Form
{
    private readonly IUserRepository _users;
    private readonly ICurrentUserContext _userContext;
    private readonly IServiceProvider _services;
    private readonly IExcelExportService _excel;
    private readonly ILogger<UserListForm> _logger;
    private readonly BindingList<User> _items = new();
    private ThreeStateSortManager<User>? _sortManager;

    public UserListForm(
        IUserRepository users,
        ICurrentUserContext userContext,
        IServiceProvider services,
        IExcelExportService excel,
        ILogger<UserListForm> logger)
    {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(userContext);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(excel);
        ArgumentNullException.ThrowIfNull(logger);

        _users = users;
        _userContext = userContext;
        _services = services;
        _excel = excel;
        _logger = logger;

        InitializeComponent();
        SetupGrid();

        Load += async (_, _) => await ReloadAsync();
        Activated += async (_, _) => await ReloadAsync();
    }

    private void SetupGrid()
    {
        var source = new BindingSource { DataSource = _items };
        _grid.DataSource = source;
        _grid.AutoGenerateColumns = false;
        _grid.Columns.Clear();

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(User.Login),
            HeaderText = "Логин",
            FillWeight = 18, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(User.FullName),
            HeaderText = "ФИО",
            FillWeight = 28, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(User.Email),
            HeaderText = "Email",
            FillWeight = 22, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(User.Role),
            HeaderText = "Роль",
            FillWeight = 14, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(User.IsActive),
            HeaderText = "Активен",
            FillWeight = 8, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(User.LastLoginAt),
            HeaderText = "Последний вход",
            DefaultCellStyle = new DataGridViewCellStyle { Format = "g" },
            FillWeight = 10, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });

        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) EditSelected();
        };

        _grid.CellFormatting += (_, e) =>
        {
            if (e.ColumnIndex < 0 || e.RowIndex < 0) return;
            var col = _grid.Columns[e.ColumnIndex];
            if (col.DataPropertyName == nameof(User.Role) && e.Value is UserRole r)
            {
                e.Value = r.ToRussian();
                e.FormattingApplied = true;
            }
        };

        _sortManager = new ThreeStateSortManager<User>(_grid, _items,
            new Dictionary<string, Func<User, object?>>
            {
                [nameof(User.Login)] = u => u.Login,
                [nameof(User.FullName)] = u => u.FullName,
                [nameof(User.Email)] = u => u.Email,
                [nameof(User.Role)] = u => u.Role.ToRussian(),
                [nameof(User.IsActive)] = u => u.IsActive,
                [nameof(User.LastLoginAt)] = u => u.LastLoginAt,
            });
    }

    private async Task ReloadAsync()
    {
        try
        {
            var items = (await _users.GetAllAsync())
                .OrderBy(u => u.Login)
                .ToList();

            if (_sortManager is not null)
            {
                _sortManager.Reset(items);
            }
            else
            {
                _items.Clear();
                foreach (var u in items) _items.Add(u);
            }

            _statusLabel.Text = $"Пользователей: {_items.Count}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка загрузки списка пользователей");
            MessageBox.Show("Не удалось загрузить пользователей: " + ex.Message,
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnAddClick(object? sender, EventArgs e)
    {
        var form = _services.GetRequiredService<UserEditForm>();
        form.SetUser(new User(), isNew: true);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await ReloadAsync();
        }
    }

    private void OnEditClick(object? sender, EventArgs e) => EditSelected();

    private async void EditSelected()
    {
        var selected = GetSelected();
        if (selected is null) return;

        var form = _services.GetRequiredService<UserEditForm>();
        form.SetUser(Clone(selected), isNew: false);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await ReloadAsync();
        }
    }

    private async void OnDeleteClick(object? sender, EventArgs e)
    {
        var selected = GetSelected();
        if (selected is null) return;

        // Защита от случайной потери доступа: текущего пользователя удалить нельзя.
        // Иначе администратор может оставить себя без входа в систему.
        if (_userContext.Current is { } current && current.Id == selected.Id)
        {
            MessageBox.Show(
                "Нельзя удалить пользователя, под которым выполнен вход. Войдите под другой учётной записью.",
                "Удаление запрещено",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var ok = MessageBox.Show(
            $"Удалить пользователя \"{selected.Login}\"?\nДействие необратимо.",
            "Удаление",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (ok != DialogResult.Yes) return;

        try
        {
            await _users.DeleteAsync(selected.Id);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка удаления пользователя {Id}", selected.Id);
            MessageBox.Show("Не удалось удалить: " + ex.Message,
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnExportClick(object? sender, EventArgs e)
    {
        if (_items.Count == 0)
        {
            MessageBox.Show("Список пользователей пуст — нечего выгружать.",
                "Экспорт в Excel", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Filter = "Excel-файл (*.xlsx)|*.xlsx",
            FileName = $"Пользователи_{DateTime.Today:yyyy-MM-dd}.xlsx",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var headers = new[] { "Логин", "ФИО", "Email", "Роль", "Активен", "Последний вход", "Создан" };
            var rows = _items.Select(u => (IReadOnlyList<object?>)new object?[]
            {
                u.Login,
                u.FullName,
                u.Email,
                u.Role.ToRussian(),
                u.IsActive,
                u.LastLoginAt,
                u.CreatedAt,
            }).ToList();

            await _excel.ExportAsync("Пользователи", headers, rows, dlg.FileName);
            OfferToOpen(dlg.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка экспорта пользователей в Excel");
            MessageBox.Show("Не удалось сохранить файл: " + ex.Message,
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OfferToOpen(string filePath)
    {
        var result = MessageBox.Show(
            $"Файл сохранён:\n{filePath}\n\nОткрыть его сейчас?",
            "Готово",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);
        if (result != DialogResult.Yes) return;
        try
        {
            Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось открыть файл {File}", filePath);
        }
    }

    private User? GetSelected()
    {
        if (_grid.CurrentRow?.DataBoundItem is User u) return u;
        MessageBox.Show("Выберите пользователя в таблице", "Внимание",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
        return null;
    }

    // Копия для редактирования — чтобы при отмене изменения в кэше не остались.
    private static User Clone(User src) => new()
    {
        Id = src.Id,
        Login = src.Login,
        FullName = src.FullName,
        Email = src.Email,
        PasswordHash = src.PasswordHash,
        Role = src.Role,
        IsActive = src.IsActive,
        LastLoginAt = src.LastLoginAt,
        CreatedAt = src.CreatedAt,
        UpdatedAt = src.UpdatedAt,
    };
}
