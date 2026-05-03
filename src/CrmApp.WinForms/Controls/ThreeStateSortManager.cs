using System.ComponentModel;

namespace CrmApp.WinForms.Controls;

// Менеджер 3-state-сортировки колонок DataGridView над BindingList<T>.
// Цикл по клику на заголовок: asc → desc → none → asc → ...
//
// Зачем своя реализация:
//   - Стандартная DataGridView.Sort умеет только 2 состояния (asc/desc) и требует
//     IBindingListView с поддержкой сортировки. Обычный BindingList<T> такого не умеет.
//   - 3-е состояние "вернуть исходный порядок" — типичная и удобная UX-фича CRM.
//
// Семантика "asc" подбирается типом ключа автоматически:
//   - строки: А → Я
//   - числа: меньше → больше
//   - даты: старая → новая
//   - enum/bool: по натуральному порядку
public sealed class ThreeStateSortManager<T>
{
    private readonly DataGridView _grid;
    private readonly BindingList<T> _bindingList;
    private readonly IReadOnlyDictionary<string, Func<T, object?>> _sortKeys;

    private List<T> _originalOrder = new();
    private string? _activeColumn;
    private SortDirection _direction = SortDirection.None;

    public ThreeStateSortManager(
        DataGridView grid,
        BindingList<T> bindingList,
        IReadOnlyDictionary<string, Func<T, object?>> sortKeys)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(bindingList);
        ArgumentNullException.ThrowIfNull(sortKeys);

        _grid = grid;
        _bindingList = bindingList;
        _sortKeys = sortKeys;

        // Programmatic-режим отключает попытки DataGridView сортировать самому
        // (всё равно не получится — BindingList не поддерживает IBindingListView).
        // Глифы стрелочек у заголовков продолжают работать.
        foreach (DataGridViewColumn c in _grid.Columns)
        {
            c.SortMode = DataGridViewColumnSortMode.Programmatic;
        }

        _grid.ColumnHeaderMouseClick += OnHeaderClick;
    }

    // Сбрасывает состояние сортировки и заполняет binding исходным порядком из items.
    // Вызывается из ReloadAsync формы — после перечитывания репозитория.
    public void Reset(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        _originalOrder = items.ToList();
        _activeColumn = null;
        _direction = SortDirection.None;
        FillBinding(_originalOrder);
        ClearGlyphs();
    }

    private void OnHeaderClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.ColumnIndex < 0) return;
        var col = _grid.Columns[e.ColumnIndex];
        var key = col.DataPropertyName;
        if (string.IsNullOrEmpty(key) || !_sortKeys.ContainsKey(key)) return;

        if (_activeColumn == key)
        {
            _direction = _direction switch
            {
                SortDirection.None => SortDirection.Ascending,
                SortDirection.Ascending => SortDirection.Descending,
                _ => SortDirection.None,
            };
            if (_direction == SortDirection.None) _activeColumn = null;
        }
        else
        {
            _activeColumn = key;
            _direction = SortDirection.Ascending;
        }

        ApplySort();
        UpdateGlyphs();
    }

    private void ApplySort()
    {
        IEnumerable<T> ordered = _originalOrder;
        if (_direction != SortDirection.None && _activeColumn is not null)
        {
            var keyFn = _sortKeys[_activeColumn];
            ordered = _direction == SortDirection.Ascending
                ? _originalOrder.OrderBy(keyFn, NullSafeComparer.Instance)
                : _originalOrder.OrderByDescending(keyFn, NullSafeComparer.Instance);
        }
        FillBinding(ordered);
    }

    private void FillBinding(IEnumerable<T> items)
    {
        // Гасим события на время массового обновления, иначе грид перерисовывается на каждый Add.
        _bindingList.RaiseListChangedEvents = false;
        _bindingList.Clear();
        foreach (var i in items) _bindingList.Add(i);
        _bindingList.RaiseListChangedEvents = true;
        _bindingList.ResetBindings();
    }

    private void UpdateGlyphs()
    {
        ClearGlyphs();
        if (_activeColumn is null || _direction == SortDirection.None) return;
        foreach (DataGridViewColumn c in _grid.Columns)
        {
            if (c.DataPropertyName == _activeColumn)
            {
                c.HeaderCell.SortGlyphDirection = _direction == SortDirection.Ascending
                    ? SortOrder.Ascending
                    : SortOrder.Descending;
                break;
            }
        }
    }

    private void ClearGlyphs()
    {
        foreach (DataGridViewColumn c in _grid.Columns)
        {
            c.HeaderCell.SortGlyphDirection = SortOrder.None;
        }
    }
}

public enum SortDirection
{
    None,
    Ascending,
    Descending,
}

// Сравнение значений: nulls в конец при asc, в начало при desc.
// IComparable — естественный путь для строк, чисел, дат, enum'ов.
internal sealed class NullSafeComparer : IComparer<object?>
{
    public static readonly NullSafeComparer Instance = new();

    public int Compare(object? x, object? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        if (x is string sx && y is string sy)
        {
            // OrdinalIgnoreCase: сравнивает по Unicode-codepoint, регистронезависимо.
            // Для кириллицы (U+0410..U+044F) даёт ожидаемый порядок А→Я / а→я.
            return string.Compare(sx, sy, StringComparison.OrdinalIgnoreCase);
        }

        if (x is IComparable cx) return cx.CompareTo(y);
        return string.Compare(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
