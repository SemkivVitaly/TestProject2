using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SaveData1.Entity;
using SaveData1.Helpers;

namespace SaveData1
{
    public partial class ProductWorkForm : Form
    {
        private readonly int _tmId;
        private readonly bool _isTesting;
        private readonly string _fio;
        private List<Description> _descriptions = new List<Description>();

        public ProductWorkForm(int tmId, string serialNumber, string category, string fio,
            DateTime date, TimeSpan timeStart, TimeSpan timeEnd, bool isTesting = false)
        {
            InitializeComponent();
            _tmId = tmId;
            _isTesting = isTesting;
            _fio = fio;

            txtSerial.Text = serialNumber ?? "";
            txtCategory.Text = category ?? "";
            txtFIO.Text = fio ?? "";
            dtpDate.Value = date.Date;
            dtpTimeStart.Value = DateTime.Today + timeStart;
            dtpTimeEnd.Value = DateTime.Today + timeEnd;

            btnSoundTest.Visible = _isTesting;

            LoadDescriptions();
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
                MessageBox.Show("Ошибка загрузки причин неисправности: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
