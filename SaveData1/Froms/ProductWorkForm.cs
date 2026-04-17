using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SaveData1.Entity;
using SaveData1.Helpers;
using SaveData1.Services;

namespace SaveData1
{
    public partial class ProductWorkForm : Form
    {
        private readonly int _tmId;
        private readonly bool _isTesting;
        private readonly string _fio;
        private readonly int _productId;
        private readonly string _actNumber;
        private readonly int _userId;
        private readonly bool _photoEnabled;
        private List<Description> _descriptions = new List<Description>();

        private readonly List<ProductPhotoService.PendingPhoto> _pendingPhotos = new List<ProductPhotoService.PendingPhoto>();
        private readonly List<ProductPhotoService.StoredPhoto> _storedPhotos = new List<ProductPhotoService.StoredPhoto>();
        private GroupBox _photoGroup;
        private FlowLayoutPanel _photoList;
        private Label _photoCounter;

        public ProductWorkForm(int tmId, string serialNumber, string category, string fio,
            DateTime date, TimeSpan timeStart, TimeSpan timeEnd, bool isTesting = false)
            : this(tmId, 0, serialNumber, category, fio, date, timeStart, timeEnd, isTesting, null, 0)
        {
        }

        public ProductWorkForm(int tmId, int productId, string serialNumber, string category, string fio,
            DateTime date, TimeSpan timeStart, TimeSpan timeEnd, bool isTesting, string actNumber, int userId)
        {
            InitializeComponent();
            _tmId = tmId;
            _isTesting = isTesting;
            _fio = fio;
            _productId = productId;
            _actNumber = actNumber;
            _userId = userId;
            _photoEnabled = productId > 0 && !string.IsNullOrWhiteSpace(actNumber) && userId > 0;

            txtSerial.Text = serialNumber ?? "";
            txtCategory.Text = category ?? "";
            txtFIO.Text = fio ?? "";
            dtpDate.Value = date.Date;

            if (timeStart == TimeSpan.Zero)
            {
                dtpTimeStart.Value = DateTime.Today + DateTime.Now.TimeOfDay;
                dtpTimeStart.Enabled = false;
            }
            else
            {
                dtpTimeStart.Value = DateTime.Today + timeStart;
            }

            if (timeEnd == TimeSpan.Zero)
                dtpTimeEnd.Value = dtpTimeStart.Value;
            else
                dtpTimeEnd.Value = DateTime.Today + timeEnd;
            if (dtpTimeEnd.Value.TimeOfDay < dtpTimeStart.Value.TimeOfDay)
                dtpTimeEnd.Value = dtpTimeStart.Value;

            btnSoundTest.Visible = _isTesting;

            LoadDescriptions();

            if (_photoEnabled)
                BuildPhotoPanel();
        }

        private void BuildPhotoPanel()
        {
            int formRight = this.ClientSize.Width;
            int panelLeft = formRight + 10;
            int panelWidth = 360;

            _photoGroup = new GroupBox
            {
                Text = "Фотографии (сохраняются при нажатии «Сохранить»)",
                Left = panelLeft,
                Top = 10,
                Width = panelWidth,
                Height = this.ClientSize.Height - 20,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom
            };

            var btnAdd = new Button
            {
                Text = "Добавить…",
                Left = 10,
                Top = 20,
                Width = 110,
                Height = 28
            };
            btnAdd.Click += PhotoAdd_Click;

            _photoCounter = new Label
            {
                Left = 130,
                Top = 25,
                AutoSize = true,
                Text = "Фото: 0"
            };

            _photoList = new FlowLayoutPanel
            {
                Left = 10,
                Top = 55,
                Width = panelWidth - 25,
                Height = _photoGroup.Height - 65,
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                BackColor = Color.White
            };

            _photoGroup.Controls.Add(btnAdd);
            _photoGroup.Controls.Add(_photoCounter);
            _photoGroup.Controls.Add(_photoList);

            this.ClientSize = new Size(formRight + panelWidth + 20, Math.Max(this.ClientSize.Height, 520));
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(this.Width, this.Height);
            this.MaximizeBox = false;
            this.Controls.Add(_photoGroup);

            LoadStoredPhotos();
            RefreshPhotoList();
        }

        private void LoadStoredPhotos()
        {
            try
            {
                _storedPhotos.Clear();
                _storedPhotos.AddRange(ProductPhotoService.GetStoredPhotos(_productId));
            }
            catch (Exception ex)
            {
                AppLog.Warn("ProductWorkForm.LoadStoredPhotos: " + ex.Message);
            }
        }

        private int NextDisplaySequence()
        {
            int max = 0;
            foreach (var sp in _storedPhotos)
                if (sp.SequenceNo > max) max = sp.SequenceNo;
            return max + 1 + _pendingPhotos.Count;
        }

        private void PhotoAdd_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Выберите фотографии";
                dlg.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp|Все файлы|*.*";
                dlg.Multiselect = true;
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                foreach (var path in dlg.FileNames)
                {
                    try
                    {
                        var pending = ProductPhotoService.CreateFromFile(path,
                            _isTesting ? ProductPhotoService.StageTesting : ProductPhotoService.StageAssembly,
                            tmaId: null, tmtId: null);
                        _pendingPhotos.Add(pending);
                    }
                    catch (Exception ex)
                    {
                        ExceptionDisplay.ShowError(this, ex, "Не удалось добавить файл: " + Path.GetFileName(path));
                    }
                }
                RefreshPhotoList();
            }
        }

        private void RefreshPhotoList()
        {
            if (_photoList == null) return;
            _photoList.Controls.Clear();

            foreach (var sp in _storedPhotos.OrderBy(x => x.SequenceNo))
                _photoList.Controls.Add(BuildThumbPanel("#" + sp.SequenceNo + " — " + sp.FileName, null, allowRemove: false, onRemove: null));

            int baseSeq = _storedPhotos.Count > 0 ? _storedPhotos.Max(x => x.SequenceNo) : 0;
            for (int i = 0; i < _pendingPhotos.Count; i++)
            {
                var p = _pendingPhotos[i];
                int displaySeq = baseSeq + i + 1;
                int localIndex = i;
                _photoList.Controls.Add(BuildThumbPanel(
                    "#" + displaySeq + " — " + (p.OriginalName ?? "(новое фото)") + "  (новое)",
                    p.Bytes, allowRemove: true,
                    onRemove: () => { _pendingPhotos.RemoveAt(localIndex); RefreshPhotoList(); }));
            }

            int total = _storedPhotos.Count + _pendingPhotos.Count;
            _photoCounter.Text = "Фото: " + total +
                (_pendingPhotos.Count > 0 ? "  (к сохранению: " + _pendingPhotos.Count + ")" : "");
        }

        private Control BuildThumbPanel(string caption, byte[] bytes, bool allowRemove, Action onRemove)
        {
            var p = new Panel
            {
                Width = _photoList.ClientSize.Width - 8,
                Height = 80,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(2)
            };
            var pb = new PictureBox
            {
                Left = 2,
                Top = 2,
                Width = 100,
                Height = 74,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Gainsboro
            };
            if (bytes != null)
            {
                try { using (var ms = new MemoryStream(bytes)) pb.Image = Image.FromStream(ms); }
                catch { pb.BackColor = Color.LightPink; }
            }
            else
            {
                pb.BackColor = Color.LightGreen;
            }
            var lbl = new Label
            {
                Left = 108,
                Top = 8,
                AutoSize = false,
                Width = p.Width - 220,
                Height = 60,
                Text = caption
            };
            p.Controls.Add(pb);
            p.Controls.Add(lbl);

            if (allowRemove && onRemove != null)
            {
                var btn = new Button
                {
                    Text = "Удалить",
                    Left = p.Width - 90,
                    Top = 25,
                    Width = 80,
                    Height = 28,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                btn.Click += (s, e) => onRemove();
                p.Controls.Add(btn);
            }
            return p;
        }

        private void btnSoundTest_Click(object sender, EventArgs e)
        {
            using (var form = new SoundTestForm())
                form.ShowDialog(this);
        }

        private void LoadDescriptions()
        {
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    _descriptions = context.Description.ToList();
                    clbDescription.Items.Clear();
                    foreach (var d in _descriptions)
                    {
                        clbDescription.Items.Add(d.DescriptionText, false);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionDisplay.ShowError(this, ex, "Ошибка загрузки причин неисправности");
            }
        }

        private void chkFault_CheckedChanged(object sender, EventArgs e)
        {
            lblDescription.Visible = chkFault.Checked;
            clbDescription.Visible = chkFault.Checked;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            TimeSpan timeStart = dtpTimeStart.Value.TimeOfDay;
            TimeSpan timeEnd = dtpTimeEnd.Value.TimeOfDay;

            if (timeEnd < timeStart)
            {
                MessageBox.Show("Время окончания не может быть раньше времени начала.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (chkFault.Checked && clbDescription.CheckedIndices.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы одну причину неисправности.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                int? firstDescriptionId = null;
                List<Description> selectedDescriptions = new List<Description>();

                if (chkFault.Checked)
                {
                    foreach (int idx in clbDescription.CheckedIndices)
                    {
                        if (idx < _descriptions.Count)
                            selectedDescriptions.Add(_descriptions[idx]);
                    }
                    if (selectedDescriptions.Count > 0)
                        firstDescriptionId = selectedDescriptions[0].DescriptionID;
                }

                int errorId = 0;
                int tmaId = 0, tmtId = 0;

                using (var context = ConnectionHelper.CreateContext())
                {
                    if (!_isTesting)
                    {
                        var tmAssembly = context.TechnicalMapAssembly.Find(_tmId);
                        if (tmAssembly != null)
                        {
                            tmAssembly.TimeStart = timeStart;
                            tmAssembly.TimeEnd = timeEnd;
                            tmAssembly.IsReady = !chkFault.Checked;
                            tmAssembly.InProgress = false;
                            tmAssembly.Fault = chkFault.Checked;
                            tmAssembly.DescriptionID = firstDescriptionId;
                            tmaId = tmAssembly.TMAID;

                            if (chkFault.Checked)
                            {
                                var full = context.TechnicalMapFull.FirstOrDefault(f => f.TMID == tmAssembly.TMID);
                                if (full != null)
                                {
                                    full.Inspection = true;

                                    var error = new Error
                                    {
                                        TMID = full.TMID,
                                        ProductID = full.ProductID,
                                        PlaceID = 2,
                                        Date = DateTime.Now,
                                        inProgress = false
                                    };
                                    context.Error.Add(error);
                                    context.SaveChanges();
                                    errorId = error.ErrorID;
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("Запись сборки не найдена.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                    else
                    {
                        var tmTesting = context.TechnicalMapTesting.Find(_tmId);
                        if (tmTesting != null)
                        {
                            tmTesting.TimeStart = timeStart;
                            tmTesting.TimeEnd = timeEnd;
                            tmTesting.IsReadt = !chkFault.Checked;
                            tmTesting.InProgress = false;
                            tmTesting.Fault = chkFault.Checked;
                            tmTesting.DescriptionID = firstDescriptionId;
                            tmtId = tmTesting.TMTID;

                            if (chkFault.Checked)
                            {
                                var full = context.TechnicalMapFull.FirstOrDefault(f => f.TMID == tmTesting.TMID);
                                if (full != null)
                                {
                                    full.Inspection = true;

                                    const int testingPlaceId = 3;
                                    var error = new Error
                                    {
                                        TMID = full.TMID,
                                        ProductID = full.ProductID,
                                        PlaceID = testingPlaceId,
                                        Date = DateTime.Now,
                                        inProgress = false
                                    };
                                    context.Error.Add(error);
                                    context.SaveChanges();
                                    errorId = error.ErrorID;
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("Запись тестирования не найдена.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    context.SaveChanges();
                }

                if (chkFault.Checked && selectedDescriptions.Count > 0)
                {
                    var descIds = selectedDescriptions.Select(d => d.DescriptionID).ToList();
                    if (tmaId != 0) FaultDescriptionHelper.SetAssemblyFaultDescriptions(tmaId, descIds);
                    if (tmtId != 0) FaultDescriptionHelper.SetTestingFaultDescriptions(tmtId, descIds);
                }

                if (chkFault.Checked && errorId > 0)
                {
                    string defectText = NonConformityLabelHelper.BuildDefectText(selectedDescriptions);
                    NonConformityLabelHelper.OfferGenerateLabel(errorId, _fio, defectText);
                }

                if (_photoEnabled && _pendingPhotos.Count > 0)
                {
                    try
                    {
                        foreach (var p in _pendingPhotos)
                        {
                            if (_isTesting) p.TMTID = tmtId == 0 ? (int?)null : tmtId;
                            else p.TMAID = tmaId == 0 ? (int?)null : tmaId;
                        }
                        var result = ProductPhotoService.Commit(
                            _productId, txtSerial.Text, _actNumber, _userId, _pendingPhotos);
                        if (result.SavedCount > 0)
                            AppLog.Info("ProductWorkForm: сохранено фото=" + result.SavedCount + " в " + result.FolderPath);
                        _pendingPhotos.Clear();
                    }
                    catch (Exception photoEx)
                    {
                        ExceptionDisplay.ShowError(this, photoEx, "Ошибка сохранения фотографий");
                        return;
                    }
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                ExceptionDisplay.ShowError(this, ex, "Ошибка сохранения");
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
