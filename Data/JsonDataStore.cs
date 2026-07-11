using System.Text.Json;
using SalaryManagementAPI.Models;

namespace SalaryManagementAPI.Data
{
    // Everything lives in one JSON file on disk. All reads/writes go through
    // this single lock so concurrent requests can't corrupt the file.
    // Good enough for a personal/small-scale app; swap for a real database
    // later if you need multi-instance hosting or heavier concurrency.
    public class JsonDataStore
    {
        private readonly string _filePath;
        private readonly object _lock = new();
        private DataFile _data = new();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public JsonDataStore(IConfiguration config, IWebHostEnvironment env)
        {
            var configuredPath = config["DataFile:Path"] ?? "data/salary-data.json";
            _filePath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(env.ContentRootPath, configuredPath);

            Load();
        }

        private void Load()
        {
            lock (_lock)
            {
                var dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        _data = JsonSerializer.Deserialize<DataFile>(json) ?? new DataFile();
                        return;
                    }
                }

                _data = new DataFile();
                Save();
            }
        }

        private void Save()
        {
            // Caller must hold _lock
            var json = JsonSerializer.Serialize(_data, JsonOptions);
            var tmpPath = _filePath + ".tmp";
            File.WriteAllText(tmpPath, json);
            File.Move(tmpPath, _filePath, overwrite: true);
        }

        // Runs `mutate` under the write lock, then persists to disk.
        // `mutate` returns whatever value the caller wants back.
        public T Mutate<T>(Func<DataFile, T> mutate)
        {
            lock (_lock)
            {
                var result = mutate(_data);
                Save();
                return result;
            }
        }

        public void Mutate(Action<DataFile> mutate)
        {
            lock (_lock)
            {
                mutate(_data);
                Save();
            }
        }

        // Read-only access; returns the live lists, so treat as read-only
        // unless you're inside Mutate.
        public T Read<T>(Func<DataFile, T> read)
        {
            lock (_lock)
            {
                return read(_data);
            }
        }
    }

    // The root object persisted to disk.
    public class DataFile
    {
        public int NextUserId { get; set; } = 1;
        public int NextSalaryId { get; set; } = 1;
        public int NextExpenseId { get; set; } = 1;
        public int NextBudgetId { get; set; } = 1;

        public List<User> Users { get; set; } = new();
        public List<Salary> Salaries { get; set; } = new();
        public List<Expense> Expenses { get; set; } = new();
        public List<Budget> Budgets { get; set; } = new();
    }
}
