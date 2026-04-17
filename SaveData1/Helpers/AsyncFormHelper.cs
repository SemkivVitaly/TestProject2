using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SaveData1.Helpers
{
    /// <summary>Помощник для выполнения async-операций из UI-потока с WaitCursor и блокировкой кнопок.</summary>
    public static class AsyncFormHelper
    {
        /// <summary>Выполняет операцию, ставя WaitCursor и блокируя указанные контролы. Ошибки показываются через <see cref="ExceptionDisplay"/>.</summary>
        public static async Task RunWithWaitAsync(this Form form, Func<Task> operation, string operationName = null, params Control[] disableWhileRunning)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            var toggled = new List<Control>();
            try
            {
                form.UseWaitCursor = true;
                foreach (var c in disableWhileRunning)
                {
                    if (c != null && c.Enabled)
                    {
                        c.Enabled = false;
                        toggled.Add(c);
                    }
                }
                await operation().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                ExceptionDisplay.ShowError(form, ex, string.IsNullOrEmpty(operationName) ? "Ошибка" : operationName);
            }
            finally
            {
                foreach (var c in toggled)
                {
                    try { c.Enabled = true; } catch { /* форма может быть закрыта */ }
                }
                try { form.UseWaitCursor = false; } catch { }
            }
        }

        /// <summary>Вариант с возвратом результата. Для отказа передаёт default(T) только если была ошибка — ошибку показываем сами.</summary>
        public static async Task<(bool Ok, T Result)> RunWithWaitAsync<T>(this Form form, Func<Task<T>> operation, string operationName = null, params Control[] disableWhileRunning)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            var toggled = new List<Control>();
            try
            {
                form.UseWaitCursor = true;
                foreach (var c in disableWhileRunning)
                {
                    if (c != null && c.Enabled)
                    {
                        c.Enabled = false;
                        toggled.Add(c);
                    }
                }
                var result = await operation().ConfigureAwait(true);
                return (true, result);
            }
            catch (Exception ex)
            {
                ExceptionDisplay.ShowError(form, ex, string.IsNullOrEmpty(operationName) ? "Ошибка" : operationName);
                return (false, default(T));
            }
            finally
            {
                foreach (var c in toggled)
                {
                    try { c.Enabled = true; } catch { }
                }
                try { form.UseWaitCursor = false; } catch { }
            }
        }
    }
}
