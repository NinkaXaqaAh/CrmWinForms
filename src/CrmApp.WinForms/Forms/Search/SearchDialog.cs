using CrmApp.WinForms.Search;
using Microsoft.Extensions.Logging;

namespace CrmApp.WinForms.Forms.Search;

// Диалог глобального поиска (Ctrl+K).
// Принцип работы:
//   1) пользователь вводит запрос,
//   2) после 300 мс паузы (debounce) вызывается ISearchService.SearchAsync,
//   3) результаты показываются в ListView,
//   4) Enter / двойной клик закрывает диалог с выбранным SearchHit.
//
// Диалог сам не открывает дочерние окна — это решает MainForm после возврата DialogResult.OK.
public partial class SearchDialog : Form
{
    private readonly ISearchService _search;
    private readonly ILogger<SearchDialog> _logger;
    private readonly System.Windows.Forms.Timer _debounce;

    public SearchHit? SelectedHit { get; private set; }

    public SearchDialog(ISearchService search, ILogger<SearchDialog> logger)
    {
        ArgumentNullException.ThrowIfNull(search);
        ArgumentNullException.ThrowIfNull(logger);

        _search = search;
        _logger = logger;

        InitializeComponent();

        _debounce = new System.Windows.Forms.Timer { Interval = 300 };
        _debounce.Tick += async (_, _) =>
        {
            _debounce.Stop();
            await PerformSearchAsync();
        };

        _searchTextBox.TextChanged += (_, _) =>
        {
            _debounce.Stop();
            _debounce.Start();
        };

        _searchTextBox.KeyDown += OnSearchKeyDown;
        _resultsList.DoubleClick += (_, _) => AcceptSelected();
        _resultsList.KeyDown += OnResultsKeyDown;

        AcceptButton = null;     // Enter — это AcceptSelected, не "OK"-кнопка
        CancelButton = _closeButton;
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            // Если в списке только один элемент — выбираем его сразу.
            if (_resultsList.Items.Count > 0)
            {
                _resultsList.Items[0].Selected = true;
                AcceptSelected();
            }
        }
        else if (e.KeyCode == Keys.Down && _resultsList.Items.Count > 0)
        {
            // Стрелка вниз перебрасывает фокус на список — стандартное поведение поиска.
            e.SuppressKeyPress = true;
            _resultsList.Focus();
            _resultsList.Items[0].Selected = true;
        }
    }

    private void OnResultsKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            AcceptSelected();
        }
    }

    private void AcceptSelected()
    {
        if (_resultsList.SelectedItems.Count == 0) return;
        if (_resultsList.SelectedItems[0].Tag is SearchHit hit)
        {
            SelectedHit = hit;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    private async Task PerformSearchAsync()
    {
        var query = _searchTextBox.Text;

        if (string.IsNullOrWhiteSpace(query))
        {
            _resultsList.Items.Clear();
            _statusLabel.Text = "Начните вводить запрос…";
            return;
        }

        try
        {
            _statusLabel.Text = "Поиск…";
            var hits = await _search.SearchAsync(query);

            _resultsList.BeginUpdate();
            _resultsList.Items.Clear();
            foreach (var hit in hits)
            {
                var item = new ListViewItem(LocalizeKind(hit.Kind))
                {
                    Tag = hit,
                };
                item.SubItems.Add(hit.Title);
                item.SubItems.Add(hit.Subtitle);
                _resultsList.Items.Add(item);
            }
            _resultsList.EndUpdate();

            _statusLabel.Text = hits.Count == 0
                ? $"Ничего не найдено по запросу «{query}»"
                : $"Найдено: {hits.Count}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка глобального поиска");
            _statusLabel.Text = "Ошибка поиска: " + ex.Message;
        }
    }

    private static string LocalizeKind(SearchHitKind k) => k switch
    {
        SearchHitKind.Customer => "Клиент",
        SearchHitKind.Deal => "Сделка",
        SearchHitKind.Activity => "Активность",
        SearchHitKind.Product => "Товар",
        _ => k.ToString(),
    };

    private void OnCloseClick(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _debounce.Stop();
        _debounce.Dispose();
        base.OnFormClosed(e);
    }
}
