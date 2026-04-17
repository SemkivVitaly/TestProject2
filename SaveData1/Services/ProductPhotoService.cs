using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using SaveData1.Entity;
using SaveData1.Helpers;

namespace SaveData1.Services
{
    /// <summary>
    /// Работа с фотографиями продукта (таблица dbo.ProductPhoto + файлы на диске в папке акта).
    /// Поддерживается модель «черновик»: AddPending копит элементы в памяти,
    /// CommitAsync атомарно пишет все в БД и в файловую систему при сохранении формы.
    /// </summary>
    public static class ProductPhotoService
    {
        /// <summary>Сцена фото: 1 = сборка, 2 = тестирование.</summary>
        public const int StageAssembly = 1;
        public const int StageTesting = 2;

        public const long MaxBytes = 10 * 1024 * 1024; // 10 MB

        public static readonly string[] AllowedExtensions =
            new[] { ".jpg", ".jpeg", ".png", ".bmp" };

        /// <summary>Элемент-черновик, ожидающий сохранения.</summary>
        public sealed class PendingPhoto
        {
            public string SourcePath { get; set; }
            public byte[] Bytes { get; set; }
            public string ContentType { get; set; }
            public string OriginalName { get; set; }
            public int Stage { get; set; }
            public int? TMAID { get; set; }
            public int? TMTID { get; set; }
        }

        /// <summary>Простой DTO для отображения сохранённых фото.</summary>
        public sealed class StoredPhoto
        {
            public int ProductPhotoID { get; set; }
            public int SequenceNo { get; set; }
            public int Stage { get; set; }
            public string FileName { get; set; }
            public string RelativePath { get; set; }
            public int ByteLength { get; set; }
            public DateTime SavedUtc { get; set; }
        }

        /// <summary>Прочитать файл с диска в черновик, выполнив валидацию.</summary>
        public static PendingPhoto CreateFromFile(string path, int stage, int? tmaId, int? tmtId)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Путь к файлу не задан.", nameof(path));
            if (!File.Exists(path))
                throw new FileNotFoundException("Файл не найден.", path);

            string ext = Path.GetExtension(path);
            if (string.IsNullOrEmpty(ext) ||
                !AllowedExtensions.Contains(ext.ToLowerInvariant()))
                throw new InvalidOperationException(
                    "Недопустимый формат файла. Разрешены: " + string.Join(", ", AllowedExtensions));

            var fi = new FileInfo(path);
            if (fi.Length == 0)
                throw new InvalidOperationException("Файл пуст.");
            if (fi.Length > MaxBytes)
                throw new InvalidOperationException(
                    "Файл слишком большой (" + (fi.Length / 1024 / 1024) + " МБ). Максимум: " + (MaxBytes / 1024 / 1024) + " МБ.");

            byte[] bytes = File.ReadAllBytes(path);

            try
            {
                using (var ms = new MemoryStream(bytes))
                using (Image.FromStream(ms))
                {
                }
            }
            catch
            {
                throw new InvalidOperationException("Файл не является корректным изображением.");
            }

            return new PendingPhoto
            {
                SourcePath = path,
                Bytes = bytes,
                ContentType = MapContentType(ext),
                OriginalName = Path.GetFileName(path),
                Stage = stage,
                TMAID = tmaId,
                TMTID = tmtId
            };
        }

        private static string MapContentType(string ext)
        {
            switch ((ext ?? "").ToLowerInvariant())
            {
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".png": return "image/png";
                case ".bmp": return "image/bmp";
                default: return "application/octet-stream";
            }
        }

        /// <summary>Получить следующий свободный SequenceNo для продукта (учитывая уже сохранённые).</summary>
        public static int GetNextSequenceNo(int productId)
        {
            return DbOperation.Run(ctx =>
            {
                var max = ctx.Database.SqlQuery<int?>(
                    "SELECT MAX(SequenceNo) FROM dbo.ProductPhoto WHERE ProductID = @p0", productId)
                    .FirstOrDefault();
                return (max ?? 0) + 1;
            }, "ProductPhotoService.GetNextSequenceNo");
        }

        /// <summary>Вернуть все сохранённые фото продукта.</summary>
        public static List<StoredPhoto> GetStoredPhotos(int productId)
        {
            return DbOperation.Run(ctx =>
            {
                return ctx.Database.SqlQuery<StoredPhoto>(
                    @"SELECT ProductPhotoID, SequenceNo, Stage, FileName, RelativePath, ByteLength, SavedUtc
                      FROM dbo.ProductPhoto WHERE ProductID = @p0
                      ORDER BY SequenceNo ASC", productId).ToList();
            }, "ProductPhotoService.GetStoredPhotos");
        }

        /// <summary>
        /// Сохранить все pending-фото в БД и в файловую систему (папка акта/серийник).
        /// Название файла формируется как «SN (N).ext», где N — SequenceNo.
        /// При ошибке БД файлы, уже созданные в этой операции, удаляются.
        /// </summary>
        public static CommitResult Commit(int productId, string productSerial, string actNumber,
            int byUserId, IList<PendingPhoto> pending)
        {
            if (pending == null || pending.Count == 0)
                return new CommitResult { SavedCount = 0 };

            if (string.IsNullOrWhiteSpace(productSerial))
                throw new InvalidOperationException("Серийный номер продукта не задан.");
            if (string.IsNullOrWhiteSpace(actNumber))
                throw new InvalidOperationException("Акт продукта не задан.");

            string basePath = DbOperation.Run(ctx => ctx.GetSavePathForAct(actNumber),
                "ProductPhotoService.Commit.GetBasePath");
            if (string.IsNullOrWhiteSpace(basePath))
                throw new InvalidOperationException(
                    "Для акта «" + actNumber + "» не задан путь сохранения (SavePath). " +
                    "Откройте вкладку пути/настроек и укажите базовую папку акта.");

            string safeSerial = InputValidator.SanitizeFileName(productSerial);
            string productFolder = Path.Combine(basePath, safeSerial);
            Directory.CreateDirectory(productFolder);

            var createdFiles = new List<string>();
            int savedCount = 0;

            try
            {
                int nextSeq = GetNextSequenceNo(productId);

                DbOperation.Execute(ctx =>
                {
                    using (var tx = ctx.Database.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        try
                        {
                            foreach (var p in pending)
                            {
                                string ext = Path.GetExtension(p.OriginalName ?? "").ToLowerInvariant();
                                if (string.IsNullOrEmpty(ext))
                                    ext = ".jpg";

                                string fileName = safeSerial + " (" + nextSeq + ")" + ext;
                                string tempPath = Path.Combine(productFolder, fileName + ".tmp");
                                string finalPath = Path.Combine(productFolder, fileName);

                                File.WriteAllBytes(tempPath, p.Bytes);
                                if (File.Exists(finalPath))
                                    File.Delete(finalPath);
                                File.Move(tempPath, finalPath);
                                createdFiles.Add(finalPath);

                                string relative = safeSerial + "/" + fileName;

                                ctx.Database.ExecuteSqlCommand(
                                    @"INSERT INTO dbo.ProductPhoto
                                      (ProductID, SequenceNo, Stage, TMAID, TMTID, FileName, RelativePath,
                                       ContentType, ByteLength, PhotoBytes, ByUserID)
                                      VALUES (@pid, @seq, @stage, @tma, @tmt, @fn, @rel, @ct, @len, @bytes, @uid)",
                                    new SqlParameter("@pid", productId),
                                    new SqlParameter("@seq", nextSeq),
                                    new SqlParameter("@stage", p.Stage),
                                    new SqlParameter("@tma", (object)p.TMAID ?? DBNull.Value),
                                    new SqlParameter("@tmt", (object)p.TMTID ?? DBNull.Value),
                                    new SqlParameter("@fn", fileName),
                                    new SqlParameter("@rel", relative),
                                    new SqlParameter("@ct", p.ContentType ?? "application/octet-stream"),
                                    new SqlParameter("@len", p.Bytes.Length),
                                    new SqlParameter("@bytes", SqlDbType.VarBinary, -1) { Value = p.Bytes },
                                    new SqlParameter("@uid", byUserId));

                                nextSeq++;
                                savedCount++;
                            }
                            tx.Commit();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }, "ProductPhotoService.Commit");

                return new CommitResult { SavedCount = savedCount, FolderPath = productFolder };
            }
            catch
            {
                foreach (var path in createdFiles)
                {
                    try { if (File.Exists(path)) File.Delete(path); }
                    catch (Exception fex) { AppLog.Warn("ProductPhotoService rollback file delete: " + fex.Message); }
                }
                throw;
            }
        }

        public sealed class CommitResult
        {
            public int SavedCount { get; set; }
            public string FolderPath { get; set; }
        }
    }
}
