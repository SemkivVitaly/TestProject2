using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SaveData1.Entity;
using SaveData1.Helpers;

namespace SaveData1
{
    /// <summary>Форма инспектора: просмотр обнаруженной неисправности, выбор своих находок, результат проверки, сохранение и генерация ярлыка.</summary>
    public partial class InspectionWorkForm : Form
    {
        private readonly int _errorId;
        private readonly UsersProfile _inspector;
        private List<Description> _descriptions = new List<Description>();
        private List<ResultTable> _results = new List<ResultTable>();

        public InspectionWorkForm(int errorId, UsersProfile inspector)
        {
            InitializeComponent();
            _errorId = errorId;
            _inspector = inspector;

            LoadErrorData();
            LoadDescriptions();
            LoadResults();
        }

        private void LoadErrorData()
        {
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var error = context.Error
                        .Include("Place")
                        .Include("Product")
                        .Include("Product.ProducType")
                        .Include("Product.Act")
                        .Include("TechnicalMapFull")
                        .Include("TechnicalMapFull.Product")
                        .Include("TechnicalMapFull.Product.ProducType")
                        .Include("TechnicalMapFull.Product.Act")
                        .FirstOrDefault(e => e.ErrorID == _errorId);

                    if (error == null) { Close(); return; }

                    string serial = "";
                    string category = "";
                    string actNumber = "";

                    if (error.TechnicalMapFull != null)
                    {
                        var p = error.TechnicalMapFull.Product;
                        serial = p?.ProductSerial ?? "";
                        category = p?.ProducType?.TypeName ?? "";
                        actNumber = p?.Act?.ActNumber ?? "";
                    }
                    else if (error.Product != null)
                    {
                        serial = error.Product.ProductSerial ?? "";
                        category = error.Product.ProducType?.TypeName ?? "";
                        actNumber = error.Product.Act?.ActNumber ?? "";
                    }

                    txtSerial.Text = serial;
                    txtCategory.Text = category;
                    txtAct.Text = actNumber;
                    txtPlace.Text = error.Place?.PlaceName ?? "";

                    var faultTexts = FaultDescriptionHelper.GetErrorDefectTexts(_errorId, error.TMID);
                    txtOriginalFault.Text = faultTexts != null && faultTexts.Count > 0
                        ? string.Join(", ", faultTexts.Distinct())
                        : "(не указано)";
                }
            }
            catch (Exception ex)
            {
                ExceptionDisplay.ShowError(this, ex, "Ошибка загрузки данных");
            }
        }

        private void LoadDescriptions()
        {
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    _descriptions = context.Description.ToList();
                    clbInspectorDescriptions.Items.Clear();
                    foreach (var d in _descriptions)
                        clbInspectorDescriptions.Items.Add(d.DescriptionText, false);
                }
            }
            catch (Exception ex)
            {
                ExceptionDisplay.ShowError(this, ex, "Ошибка загрузки описаний");
            }
        }

        private void LoadResults()
        {
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    _results = context.ResultTable.ToList();
                    cmbResult.DataSource = _results;
                    cmbResult.DisplayMember = "ResultText";
                    cmbResult.ValueMember = "ResultID";
                }
            }
            catch (Exception ex)
            {
                ExceptionDisplay.ShowError(this, ex, "Ошибка загрузки результатов");
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (clbInspectorDescriptions.CheckedIndices.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы одну неисправность.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (cmbResult.SelectedItem == null)
            {
                MessageBox.Show("Выберите результат проверки.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                int resultId = (int)cmbResult.SelectedValue;
                string resultText = ((ResultTable)cmbResult.SelectedItem).ResultText ?? "";

                var selectedDescriptions = new List<Description>();
                foreach (int idx in clbInspectorDescriptions.CheckedIndices)
                {
                    if (idx < _descriptions.Count)
                        selectedDescriptions.Add(_descriptions[idx]);
                }

                DateTime errorDate = DateTime.Now;
                string fio1 = "";
                using (var context = ConnectionHelper.CreateContext())
                {
                    foreach (var desc in selectedDescriptions)
                    {
                        var inspection = new Inspection
                        {
                            UserID = _inspector.UserID,
                            ErrorID = _errorId,
                            DescriptionID = desc.DescriptionID,
                            ResultID = resultId
                        };
                        context.Inspection.Add(inspection);
                    }

                    var error = context.Error.Find(_errorId);
                    if (error != null)
                        error.inProgress = false;

                    context.SaveChanges();

                    errorDate = error != null ? error.Date : DateTime.Now;
                    if (error != null && error.TMID.HasValue)
                    {
                        int tmidForFio = error.TMID.Value;
                        var asmUser = context.TechnicalMapAssembly
                            .Where(a => a.TMID == tmidForFio && a.Fault)
                            .Select(a => a.UsersProfile.UserName).FirstOrDefault();
                        var tstUser = context.TechnicalMapTesting
                            .Where(t => t.TMID == tmidForFio && t.Fault)
                            .Select(t => t.UsersProfile.UserName).FirstOrDefault();
                        fio1 = asmUser ?? tstUser ?? "";
                    }

                    if (resultText.IndexOf("Отклонение разрешено", StringComparison.OrdinalIgnoreCase) >= 0 && error != null && error.TMID.HasValue)
                    {
                        int tmid = error.TMID.Value;
                        var full = context.TechnicalMapFull.Find(tmid);
                        if (full != null)
                            full.Inspection = false;
                        if (error.PlaceID == 2)
                        {
                            foreach (var asm in context.TechnicalMapAssembly.Where(a => a.TMID == tmid && a.Fault))
                                asm.Fault = false;
                        }
                        else if (error.PlaceID == 3)
                        {
                            foreach (var tst in context.TechnicalMapTesting.Where(t => t.TMID == tmid && t.Fault))
                                tst.Fault = false;
                        }
                        context.SaveChanges();
                    }
                }

                string defectText = NonConformityLabelHelper.BuildDefectText(selectedDescriptions);
                string placeName = txtPlace.Text;

                NonConformityLabelHelper.GenerateLabel(
                    _errorId, txtSerial.Text, txtCategory.Text, placeName,
                    defectText, errorDate, txtAct.Text,
                    fio1, _inspector.UserName ?? "", resultText);

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
