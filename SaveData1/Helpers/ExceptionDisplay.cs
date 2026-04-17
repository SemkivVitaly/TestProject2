using System;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SaveData1.Helpers
{
    /// <summary>Сообщения об ошибках для UI: EF часто прячет причину во внутреннем исключении (например SqlException).</summary>
    public static class ExceptionDisplay
    {
        /// <summary>Склеивает Message + InnerException (до maxDepth уровней) для показа пользователю.</summary>
        public static string MessageWithInners(Exception ex, int maxDepth = 6)
        {
            if (ex == null) return "";
            var sb = new StringBuilder(ex.Message.TrimEnd());
            int depth = 0;
            for (var inner = ex.InnerException; inner != null && depth < maxDepth; inner = inner.InnerException, depth++)
            {
                sb.AppendLine().Append("→ ").Append(inner.Message.TrimEnd());
            }
            return sb.ToString();
        }

        /// <summary>Человеческий текст для SQL/EF ошибок + детали валидации EF. Для неизвестных исключений возвращает <see cref="MessageWithInners"/>.</summary>
        public static string Humanize(Exception ex)
        {
            if (ex == null) return "";

            var sql = FindInner<SqlException>(ex);
            if (sql != null)
            {
                string byCode = DescribeSqlError(sql);
                if (!string.IsNullOrEmpty(byCode))
                    return byCode;
            }

            var validation = FindInner<DbEntityValidationException>(ex);
            if (validation != null)
            {
                var parts = validation.EntityValidationErrors
                    .SelectMany(e => e.ValidationErrors)
                    .Select(v => "• " + v.PropertyName + ": " + v.ErrorMessage);
                string joined = string.Join(Environment.NewLine, parts);
                return "Не прошла проверка данных:" + Environment.NewLine + joined;
            }

            if (FindInner<DbUpdateException>(ex) != null && sql == null)
            {
                return "Не удалось сохранить изменения в базе данных." + Environment.NewLine + MessageWithInners(ex);
            }

            if (FindInner<EntityException>(ex) != null && sql == null)
            {
                return "Ошибка работы с базой данных." + Environment.NewLine + MessageWithInners(ex);
            }

            return MessageWithInners(ex);
        }

        /// <summary>Показывает окно ошибки с человеческим текстом, кнопкой «Подробнее…», и пишет в <see cref="AppLog"/>.</summary>
        public static void ShowError(IWin32Window owner, Exception ex, string title = "Ошибка")
        {
            if (ex == null) return;
            AppLog.Error(title ?? "Ошибка", ex);

            string summary = Humanize(ex);
            string details = BuildDetails(ex);

            using (var form = new Form())
            using (var lbl = new Label())
            using (var btnDetails = new Button())
            using (var btnCopy = new Button())
            using (var btnOk = new Button())
            using (var txt = new TextBox())
            {
                form.Text = string.IsNullOrWhiteSpace(title) ? "Ошибка" : title;
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.Sizable;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ShowIcon = false;
                form.ShowInTaskbar = false;
                form.ClientSize = new System.Drawing.Size(560, 180);
                form.MinimumSize = new System.Drawing.Size(420, 180);

                lbl.Text = summary;
                lbl.Location = new System.Drawing.Point(12, 12);
                lbl.Size = new System.Drawing.Size(form.ClientSize.Width - 24, 110);
                lbl.AutoSize = false;
                lbl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                lbl.Font = new System.Drawing.Font("Segoe UI", 9.75F);

                txt.Multiline = true;
                txt.ReadOnly = true;
                txt.ScrollBars = ScrollBars.Both;
                txt.WordWrap = false;
                txt.Text = details;
                txt.Location = new System.Drawing.Point(12, 130);
                txt.Size = new System.Drawing.Size(form.ClientSize.Width - 24, 200);
                txt.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                txt.Visible = false;
                txt.Font = new System.Drawing.Font("Consolas", 9F);

                btnDetails.Text = "Подробнее…";
                btnDetails.AutoSize = true;
                btnDetails.Location = new System.Drawing.Point(12, form.ClientSize.Height - 34);
                btnDetails.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

                btnCopy.Text = "Копировать";
                btnCopy.AutoSize = true;
                btnCopy.Location = new System.Drawing.Point(btnDetails.Right + 6, btnDetails.Top);
                btnCopy.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                btnCopy.Visible = false;

                btnOk.Text = "OK";
                btnOk.AutoSize = true;
                btnOk.Location = new System.Drawing.Point(form.ClientSize.Width - 100, form.ClientSize.Height - 34);
                btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                btnOk.DialogResult = DialogResult.OK;

                btnDetails.Click += (s, e) =>
                {
                    bool show = !txt.Visible;
                    txt.Visible = show;
                    btnCopy.Visible = show;
                    if (show)
                    {
                        form.Height = 420;
                        btnDetails.Text = "Скрыть подробности";
                    }
                    else
                    {
                        form.Height = 220;
                        btnDetails.Text = "Подробнее…";
                    }
                };

                btnCopy.Click += (s, e) =>
                {
                    try { Clipboard.SetText(summary + Environment.NewLine + Environment.NewLine + details); }
                    catch { /* игнор — копирование не критично */ }
                };

                form.Controls.Add(lbl);
                form.Controls.Add(txt);
                form.Controls.Add(btnDetails);
                form.Controls.Add(btnCopy);
                form.Controls.Add(btnOk);
                form.AcceptButton = btnOk;
                form.CancelButton = btnOk;
                form.ShowDialog(owner);
            }
        }

        /// <summary>Простое окно ошибки без «Подробнее». Эквивалент <see cref="ShowError"/> для случаев, где не требуется стектрейс.</summary>
        public static void ShowWarning(IWin32Window owner, string message, string title = "Внимание")
        {
            AppLog.Warn((title ?? "") + ": " + (message ?? ""));
            MessageBox.Show(owner, message ?? "", string.IsNullOrWhiteSpace(title) ? "Внимание" : title,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static string BuildDetails(Exception ex)
        {
            var sb = new StringBuilder();
            int depth = 0;
            for (var e = ex; e != null && depth < 10; e = e.InnerException, depth++)
            {
                sb.Append("[").Append(depth).Append("] ").Append(e.GetType().FullName).Append(": ").AppendLine(e.Message);
                if (e is SqlException se)
                {
                    sb.Append("    Number=").Append(se.Number)
                      .Append(" State=").Append(se.State)
                      .Append(" Class=").Append(se.Class)
                      .Append(" LineNumber=").Append(se.LineNumber).AppendLine();
                }
                if (!string.IsNullOrEmpty(e.StackTrace))
                {
                    sb.AppendLine(e.StackTrace);
                }
            }
            return sb.ToString();
        }

        private static T FindInner<T>(Exception ex) where T : Exception
        {
            for (var e = ex; e != null; e = e.InnerException)
                if (e is T t) return t;
            return null;
        }

        /// <summary>Перевод известных кодов SQL Server в человеческий текст.</summary>
        private static string DescribeSqlError(SqlException sql)
        {
            switch (sql.Number)
            {
                case -2:
                case 11: return "Таймаут ожидания ответа от сервера БД. Попробуйте ещё раз.";
                case 53:
                case 11001: return "Сервер базы данных не найден в сети. Проверьте подключение и настройки.";
                case 40:
                case 10054:
                case 10060: return "Потеряно соединение с сервером БД. Проверьте сеть/VPN.";
                case 18456: return "Неверные учётные данные для подключения к базе данных.";
                case 4060: return "База данных недоступна. Обратитесь к администратору.";
                case 40613:
                case 40197:
                case 40501:
                case 49918:
                case 49919:
                case 49920: return "Временная недоступность облачной базы. Повторите попытку.";
                case 1205: return "Конфликт транзакций (deadlock). Повторите операцию.";
                case 2627:
                case 2601: return "Запись с такими данными уже существует. Нарушена уникальность.";
                case 547: return "Нарушены ограничения целостности (внешний ключ или проверка). Проверьте связанные данные.";
                case 515: return "Попытка вставить пустое значение в обязательное поле.";
                case 8152:
                case 2628: return "Значение не помещается в поле (слишком длинная строка).";
                default:
                    return null; // неизвестный код — покажем «как есть»
            }
        }
    }
}
