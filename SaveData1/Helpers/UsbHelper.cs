using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SaveData1.Helpers
{
    /// <summary>Работа со съёмными дисками: серийный номер тома, копирование, очистка, извлечение.</summary>
    public static class UsbHelper
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetVolumeInformation(
            string rootPathName,
            System.Text.StringBuilder volumeNameBuffer,
            int volumeNameSize,
            out uint volumeSerialNumber,
            out uint maximumComponentLength,
            out uint fileSystemFlags,
            System.Text.StringBuilder fileSystemNameBuffer,
            int nFileSystemNameSize);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr SecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const int FILE_SHARE_READ = 0x1;
        private const int FILE_SHARE_WRITE = 0x2;
        private const int OPEN_EXISTING = 3;

        private const uint FSCTL_LOCK_VOLUME = 0x00090018;
        private const uint FSCTL_DISMOUNT_VOLUME = 0x00090020;
        private const uint IOCTL_STORAGE_EJECT_MEDIA = 0x002D4808;

        /// <summary>Возвращает серийный номер тома по букве диска.</summary>
        public static string GetVolumeSerialNumber(string driveLetter)
        {
            if (string.IsNullOrEmpty(driveLetter)) return "";
            string rootPath = driveLetter;
            if (!rootPath.EndsWith("\\")) rootPath += "\\";

            uint serialNumber;
            uint maxComponentLength;
            uint fileSystemFlags;

            bool result = GetVolumeInformation(
                rootPath, null, 0, out serialNumber,
                out maxComponentLength, out fileSystemFlags, null, 0);

            if (result)
            {
                return serialNumber.ToString("X");
            }
            return "";
        }

        /// <summary>Список съёмных дисков, готовых к использованию.</summary>
        public static DriveInfo[] GetRemovableDrives()
        {
            return Array.FindAll(DriveInfo.GetDrives(), d => d.DriveType == DriveType.Removable && d.IsReady);
        }

        /// <summary>Рекурсивное копирование папки со всеми файлами.</summary>
        public static void CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists) throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir);
            }
        }

        /// <summary>Проверяет, есть ли на диске хотя бы один файл с заданными расширениями.</summary>
        public static bool HasFilesWithExtensions(string sourceDir, string[] extensions)
        {
            if (extensions == null || extensions.Length == 0) return false;
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists) return false;

            foreach (FileInfo file in dir.GetFiles())
            {
                string ext = file.Extension;
                if (string.IsNullOrEmpty(ext)) continue;
                foreach (var e in extensions)
                {
                    string norm = e.StartsWith(".") ? e : "." + e;
                    if (string.Equals(ext, norm, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                if (HasFilesWithExtensions(subDir.FullName, extensions))
                    return true;
            }
            return false;
        }

        /// <summary>Копирование только файлов с заданными расширениями, рекурсивно.</summary>
        public static void CopyDirectoryWithExtensions(string sourceDir, string destinationDir, string[] extensions)
        {
            if (extensions == null || extensions.Length == 0) return;
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists) throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string ext = file.Extension;
                if (string.IsNullOrEmpty(ext)) continue;
                bool allowed = false;
                foreach (var e in extensions)
                {
                    string norm = e.StartsWith(".") ? e : "." + e;
                    if (string.Equals(ext, norm, StringComparison.OrdinalIgnoreCase)) { allowed = true; break; }
                }
                if (allowed)
                {
                    string targetFilePath = Path.Combine(destinationDir, file.Name);
                    file.CopyTo(targetFilePath, true);
                }
            }

            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectoryWithExtensions(subDir.FullName, newDestinationDir, extensions);
            }
        }

        /// <summary>Очистка только файлов с заданными расширениями.</summary>
        public static void ClearDirectoryWithExtensions(string targetDir, string[] extensions)
        {
            if (extensions == null || extensions.Length == 0) return;
            var dir = new DirectoryInfo(targetDir);
            if (!dir.Exists) return;

            foreach (FileInfo file in dir.GetFiles())
            {
                string ext = file.Extension;
                if (string.IsNullOrEmpty(ext)) continue;
                bool allowed = false;
                foreach (var e in extensions)
                {
                    string norm = e.StartsWith(".") ? e : "." + e;
                    if (string.Equals(ext, norm, StringComparison.OrdinalIgnoreCase)) { allowed = true; break; }
                }
                if (allowed)
                {
                    file.Delete();
                }
            }

            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                ClearDirectoryWithExtensions(subDir.FullName, extensions);
                try
                {
                    if (subDir.GetFileSystemInfos().Length == 0)
                        subDir.Delete();
                }
                catch { }
            }
        }

        /// <summary>Удаляет подпапку с указанным именем в заданной папке вместе с содержимым, если она существует.</summary>
        public static void RemoveSubfolderIfExists(string parentDir, string subfolderName)
        {
            if (string.IsNullOrEmpty(parentDir) || string.IsNullOrEmpty(subfolderName)) return;
            string path = Path.Combine(parentDir, subfolderName);
            if (Directory.Exists(path))
            {
                try
                {
                    Directory.Delete(path, true);
                }
                catch { }
            }
        }

        /// <summary>Очистка содержимого папки (файлы и подпапки).</summary>
        public static void ClearDirectory(string targetDir)
        {
            var dir = new DirectoryInfo(targetDir);
            if (!dir.Exists) return;

            foreach (FileInfo file in dir.GetFiles())
            {
                file.Delete();
            }

            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                subDir.Delete(true);
            }
        }

        /// <summary>Извлечение съёмного диска по букве.</summary>
        public static bool EjectDrive(char driveLetter)
        {
            string filename = $@"\\.\{driveLetter}:";
            IntPtr handle = CreateFile(filename, GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);

            if (handle.ToInt64() == -1)
            {
                return false;
            }

            uint returnedBytes;
            bool result = false;

            if (DeviceIoControl(handle, FSCTL_LOCK_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out returnedBytes, IntPtr.Zero))
            {
                if (DeviceIoControl(handle, FSCTL_DISMOUNT_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out returnedBytes, IntPtr.Zero))
                {
                    result = DeviceIoControl(handle, IOCTL_STORAGE_EJECT_MEDIA, IntPtr.Zero, 0, IntPtr.Zero, 0, out returnedBytes, IntPtr.Zero);
                }
            }

            CloseHandle(handle);
            return result;
        }
    }
}