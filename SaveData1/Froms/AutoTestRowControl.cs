using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SaveData1.Helpers;

namespace SaveData1.Froms
{
    public partial class AutoTestRowControl : UserControl
    {
        public event EventHandler RemoveRequested;
        public event EventHandler ErrorRequested;
        public event EventHandler<SerialNumberChangedEventArgs> SerialNumberChanged;
        public event EventHandler<FlashSavedEventArgs> FlashSaved;

        public string SavedVolumeSerialNumber { get; private set; }
        public string SavedVolumeLabel { get; private set; }
        public string SerialNumber => txtSerial.Text.Trim();
        public string Stand => txtStand.Text.Trim();

        private List<string> _allSerials = new List<string>();
        private bool _updatingText;
        private string _lastKnownSerial = "";

        public class SerialNumberChangedEventArgs : EventArgs
        {
            public string OldSerial { get; set; }
            public string NewSerial { get; set; }
            public string StandNumber { get; set; }
        }

        public class FlashSavedEventArgs : EventArgs
        {
            public string SerialNumber { get; set; }
            public string VolumeSerialNumber { get; set; }
            public string StandNumber { get; set; }
        }

        public AutoTestRowControl()
        {
            InitializeComponent();
            RefreshUsbDrives();
            txtSerial.TextChanged += TxtSerial_TextChanged;
            txtSerial.Leave += TxtSerial_Leave;
            _lastKnownSerial = txtSerial.Text.Trim();
        }

        public void SetAutoCompleteSource(IEnumerable<string> serials)
        {
            if (serials == null) return;
            _allSerials = serials.Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToList();
            var src = new AutoCompleteStringCollection();
            foreach (var s in _allSerials)
                src.Add(s);
            var added = new HashSet<string>();
            foreach (var s in _allSerials)
            {
                string last4 = GetLast4Digits(s);
                if (!string.IsNullOrEmpty(last4))
                {
                    string key = last4 + " - " + s;
                    if (!added.Contains(key)) { added.Add(key); src.Add(key); }
                }
            }
            txtSerial.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            txtSerial.AutoCompleteSource = AutoCompleteSource.CustomSource;
            txtSerial.AutoCompleteCustomSource = src;
        }

        private static string GetLast4Digits(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length < 4) return null;
            var digits = new string(s.Reverse().TakeWhile(char.IsDigit).Take(4).Reverse().ToArray());
            return digits.Length == 4 ? digits : null;
        }

        private void TxtSerial_TextChanged(object sender, EventArgs e)
        {
            if (_updatingText) return;
            string t = txtSerial.Text;
            if (string.IsNullOrEmpty(t) || !t.Contains(" - ")) return;
            int idx = t.LastIndexOf(" - ", StringComparison.Ordinal);
            string afterDash = t.Substring(idx + 3).Trim();
            if (_allSerials.Contains(afterDash))
            {
                _updatingText = true;
                txtSerial.Text = afterDash;
                txtSerial.SelectAll();
                _updatingText = false;
            }
        }

        private void TxtSerial_Leave(object sender, EventArgs e)
        {
            string current = txtSerial.Text.Trim();
            if (!string.IsNullOrEmpty(_lastKnownSerial) && _lastKnownSerial != current)
            {
                SerialNumberChanged?.Invoke(this, new SerialNumberChangedEventArgs
                {
                    OldSerial = _lastKnownSerial,
                    NewSerial = current,
                    StandNumber = Stand
                });
            }
            _lastKnownSerial = current;
        }

        public void SetLoadedState(string standNumber, string volumeSerialNumber, string serialNumber, string volumeLabel = "")
        {
            txtStand.Text = standNumber ?? "";
            txtSerial.Text = serialNumber ?? "";
            _lastKnownSerial = serialNumber ?? "";
            if (!string.IsNullOrEmpty(volumeSerialNumber))
            {
                SavedVolumeSerialNumber = volumeSerialNumber;
                SavedVolumeLabel = volumeLabel ?? "";
                lblStatus.Text = "Сохранено";
                lblStatus.ForeColor = Color.Green;
                RefreshUsbDrivesWithSaved(volumeSerialNumber, volumeLabel ?? "");
            }
        }

        private void btnError_Click(object sender, EventArgs e)
        {
            ErrorRequested?.Invoke(this, EventArgs.Empty);
        }

        public void RefreshUsbDrives()
        {
            RefreshUsbDrivesWithSaved(null, null);
        }

        private void RefreshUsbDrivesWithSaved(string savedVolumeSerial, string savedVolumeLabel)
        {
            var drives = UsbHelper.GetRemovableDrives();
            cmbUsb.Items.Clear();
            int matchIndex = -1;

            foreach (var d in drives)
            {
                var item = new UsbDriveItem
                {
                    DriveLetter = d.Name,
                    VolumeLabel = d.IsReady ? d.VolumeLabel : "",
                    VolumeSerialNumber = UsbHelper.GetVolumeSerialNumber(d.Name)
                };
                cmbUsb.Items.Add(item);
                if (!string.IsNullOrEmpty(savedVolumeSerial) &&
                    string.Equals(item.VolumeSerialNumber, savedVolumeSerial, StringComparison.OrdinalIgnoreCase))
                    matchIndex = cmbUsb.Items.Count - 1;
            }

            if (!string.IsNullOrEmpty(savedVolumeSerial) && matchIndex < 0)
            {
                cmbUsb.Items.Insert(0, new UsbDriveItem
                {
                    DriveLetter = "",
                    VolumeLabel = savedVolumeLabel ?? "",
                    VolumeSerialNumber = savedVolumeSerial
                });
                cmbUsb.SelectedIndex = 0;
            }
            else if (matchIndex >= 0)
            {
                cmbUsb.SelectedIndex = matchIndex;
            }
            else if (cmbUsb.Items.Count > 0)
            {
                cmbUsb.SelectedIndex = 0;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (cmbUsb.SelectedItem is UsbDriveItem item)
            {
                if (string.IsNullOrEmpty(item.VolumeSerialNumber))
                {
                    MessageBox.Show("Не удалось получить серийный номер тома для этой флешки. Выберите другую.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                SavedVolumeSerialNumber = item.VolumeSerialNumber;
                SavedVolumeLabel = item.VolumeLabel ?? "";
                lblStatus.Text = "Сохранено";
                lblStatus.ForeColor = Color.Green;
                _lastKnownSerial = SerialNumber;
                FlashSaved?.Invoke(this, new FlashSavedEventArgs
                {
                    SerialNumber = SerialNumber,
                    VolumeSerialNumber = item.VolumeSerialNumber,
                    StandNumber = Stand
                });
            }
            else
            {
                MessageBox.Show("Выберите флешку для сохранения.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            RemoveRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnRefreshUsb_Click(object sender, EventArgs e)
        {
            RefreshUsbDrivesWithSaved(SavedVolumeSerialNumber, SavedVolumeLabel);
        }

        public void SetStatus(string text, Color color)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetStatus(text, color)));
                return;
            }
            lblStatus.Text = text;
            lblStatus.ForeColor = color;
        }

        public void SetMonitoringMode(bool isMonitoring)
        {
            btnRemove.Enabled = !isMonitoring;
        }

        private class UsbDriveItem
        {
            public string DriveLetter { get; set; }
            public string VolumeLabel { get; set; }
            public string VolumeSerialNumber { get; set; }

            public override string ToString()
            {
                if (string.IsNullOrEmpty(DriveLetter))
                    return string.IsNullOrEmpty(VolumeLabel) ? "Флешка" : VolumeLabel;
                string name = string.IsNullOrEmpty(VolumeLabel) ? "Флешка" : VolumeLabel;
                return $"{name} ({DriveLetter.TrimEnd('\\')})";
            }
        }
    }
}
