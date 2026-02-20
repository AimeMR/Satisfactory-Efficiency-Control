using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using SatisfactoryManagerApp.Data;

namespace SatisfactoryManagerApp.ViewModels
{
    /// <summary>
    /// ViewModel for the left-side machine library panel.
    /// Loads all machines from the SQLite database and exposes them as a
    /// filterable, observable list for display and drag-and-drop.
    /// Falls back to a built-in list of Satisfactory machines when the DB is empty.
    /// </summary>
    public class MachineLibraryViewModel : ViewModelBase
    {
        // ── All loaded entries ─────────────────────────────────────────────────
        private readonly List<MachineLibraryItem> _all = new();

        // ── Bound to the sidebar ListBox ──────────────────────────────────────
        public ObservableCollection<MachineLibraryItem> Filtered { get; } = new();

        // ── Search text ───────────────────────────────────────────────────────
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetField(ref _searchText, value))
                    ApplyFilter();
            }
        }

        // ── Constructor ───────────────────────────────────────────────────────

        public MachineLibraryViewModel()
        {
            Task.Run(LoadAsync);
        }

        // ── Data loading ──────────────────────────────────────────────────────

        private async Task LoadAsync()
        {
            try
            {
                await using var db = new SatisfactoryDbContext();
                var machines = await db.Machines
                    .AsNoTracking()
                    .OrderBy(m => m.Name)
                    .ToListAsync();

                if (machines.Count > 0)
                {
                    foreach (var m in machines)
                        _all.Add(new MachineLibraryItem { MachineName = m.Name, PowerMW = m.PowerConsumption });
                }
                else
                {
                    LoadFallback();
                }
            }
            catch
            {
                // If DB can't be opened, use the built-in list
                LoadFallback();
            }

            App.Current?.Dispatcher.Invoke(ApplyFilter);
        }

        /// <summary>
        /// Built-in list of core Satisfactory 1.0 production machines.
        /// Used when the database is unavailable or empty.
        /// </summary>
        private void LoadFallback()
        {
            var fallback = new (string name, double mw)[]
            {
                // Extraction
                ("Minero Mk.1",            5),
                ("Minero Mk.2",           12),
                ("Minero Mk.3",           30),
                ("Extractor de Petróleo", 40),
                ("Extractor de Nodos",     0),
                // Smelting / Foundry
                ("Fundidora",              4),
                ("Fundición",             16),
                // Manufacturing
                ("Constructora",           4),
                ("Ensambladora",          15),
                ("Manufacturera",         55),
                ("Refinería",             30),
                ("Mezcladora",            12),
                ("Centrífuga",            26),
                ("Aceleradora de Partículas", 500),
                ("Convertidor",           100),
                // Power
                ("Generador de Carbón",   75),
                ("Generador de Combustible", 150),
                ("Generador Nuclear",     2500),
                ("Generador Geotérmico",   0),
                // Logistics
                ("Divisor",                0),
                ("Fusionador",             0),
                ("Divisor de Tuberías",    0),
                ("Fusionador de Tuberías", 0),
            };

            foreach (var (name, mw) in fallback)
                _all.Add(new MachineLibraryItem { MachineName = name, PowerMW = mw });
        }

        // ── Filtering ─────────────────────────────────────────────────────────

        private void ApplyFilter()
        {
            Filtered.Clear();
            var term = _searchText.Trim().ToLowerInvariant();
            foreach (var item in _all)
            {
                if (string.IsNullOrEmpty(term) ||
                    item.MachineName.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    Filtered.Add(item);
                }
            }
        }
    }
}
