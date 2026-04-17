using System;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SaveData1.Entity;

namespace SaveData1.Helpers
{
    /// <summary>Единая точка выполнения операций с БД: создаёт контекст, ретраит транзиентные сбои, ставит WaitCursor.</summary>
    public static class DbOperation
    {
        private static readonly int[] RetryDelaysMs = { 200, 500, 1200 };

        /// <summary>Транзиентные коды SQL Server (сеть/deadlock/временная недоступность).</summary>
        private static readonly int[] TransientSqlNumbers =
        {
            -2,     // client timeout
            40,
            1205,   // deadlock
            4060,
            10054, 10060, 11001,
            40197, 40501, 40613,
            49918, 49919, 49920
        };

        /// <summary>Синхронное выполнение операции с ретраями. Контекст создаётся и закрывается внутри.</summary>
        public static T Run<T>(Func<SaveDataEntities2, T> operation, string operationName = null)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            Cursor previous = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                return RunWithRetries(operation, operationName);
            }
            finally
            {
                Cursor.Current = previous;
            }
        }

        /// <summary>Синхронное выполнение без возврата значения.</summary>
        public static void Execute(Action<SaveDataEntities2> operation, string operationName = null)
        {
            Run<object>(ctx => { operation(ctx); return null; }, operationName);
        }

        /// <summary>Асинхронное выполнение в Task.Run (для долгих запросов и записи).</summary>
        public static Task<T> RunAsync<T>(Func<SaveDataEntities2, T> operation, string operationName = null, CancellationToken ct = default(CancellationToken))
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                return RunWithRetries(operation, operationName);
            }, ct);
        }

        public static Task ExecuteAsync(Action<SaveDataEntities2> operation, string operationName = null, CancellationToken ct = default(CancellationToken))
        {
            return RunAsync<object>(ctx => { operation(ctx); return null; }, operationName, ct);
        }

        private static T RunWithRetries<T>(Func<SaveDataEntities2, T> operation, string operationName)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    using (var ctx = ConnectionHelper.CreateContext())
                    {
                        return operation(ctx);
                    }
                }
                catch (Exception ex)
                {
                    bool transient = IsTransient(ex);
                    if (transient && attempt < RetryDelaysMs.Length)
                    {
                        int delay = RetryDelaysMs[attempt];
                        AppLog.Warn("DbOperation[" + (operationName ?? "?") + "] transient failure, retry in " + delay + " ms (attempt " + (attempt + 1) + ")", ex);
                        Thread.Sleep(delay);
                        attempt++;
                        continue;
                    }
                    AppLog.Error("DbOperation[" + (operationName ?? "?") + "] failed after " + (attempt + 1) + " attempt(s)", ex);
                    throw;
                }
            }
        }

        /// <summary>Является ли исключение транзиентным (стоит ретраить).</summary>
        public static bool IsTransient(Exception ex)
        {
            for (var e = ex; e != null; e = e.InnerException)
            {
                if (e is SqlException sql)
                {
                    foreach (int n in TransientSqlNumbers)
                        if (sql.Number == n) return true;
                    return false;
                }
                if (e is TimeoutException) return true;
                if (e is EntityException) continue; // посмотрим ниже на SqlException
                if (e is DbUpdateException) continue;
            }
            return false;
        }
    }
}
