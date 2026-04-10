using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SaveData1.Entity;
using SaveData1.Helpers;

namespace SaveData1.Froms
{
    public partial class FlightTestErrorForm : Form
    {
        private readonly int _productId;
        private readonly int _tflightId;
        private readonly string _serialNumber;
        private readonly string _actNumber;
        private readonly UsersProfile _user;
        private readonly Action _onSaved;
        private readonly List<TestFlight> _existingRecords;
        private readonly int? _existingNonConformityErrorId;

        private readonly string[] _checkLabels = new[]
        {
            "Визуальный осмотр", "Ударная нагрузка", "FC1_питание", "FC2_питание",
            "+5B_FC1", "+5B_FC2", "FC_тест пройден", "Внешние датчики_тест пройден", "Длительный тест пройден"
        };

        private List<NumericUpDown> _numStands = new List<NumericUpDown>();
        private List<CheckBox[]> _rowChecks = new List<CheckBox[]>();

        public FlightTestErrorForm(int productId, int tflightId, string serialNumber, string actNumber,
            UsersProfile user, Action onSaved, List<TestFlight> existingRecords = null, int? existingNonConformityErrorId = null)
        {
            InitializeComponent();
            _productId = productId;
            _tflightId = tflightId;
            _serialNumber = serialNumber ?? "";
            _actNumber = actNumber ?? "";
            _user = user;
            _onSaved = onSaved;
            _existingRecords = existingRecords;
            _existingNonConformityErrorId = existingNonConformityErrorId;

            txtSerial.Text = _serialNumber;
            cmbItog.SelectedIndex = 0;

            BuildRows();
            if (_existingRecords != null && _existingRecords.Count > 0)
                LoadFromExisting();
        }

        private void BuildRows()
        {
            tlpRows.ColumnStyles.Clear();
            for (int c = 0; c < 12; c++)
                tlpRows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, c == 0 ? 45 : (c == 1 ? 50 : 95)));

            for (int row = 0; row < 3; row++)
            {
                var numStand = new NumericUpDown
                {
                    Minimum = 0,
                    Maximum = 999,
                    Value = 0,
                    Width = 50
                };
                tlpRows.Controls.Add(new Label { Text = "Стенд:", AutoSize = true }, 0, row);
                tlpRows.Controls.Add(numStand, 1, row);
                _numStands.Add(numStand);

                var checks = new CheckBox[9];
                for (int i = 0; i < 9; i++)
                {
                    checks[i] = new CheckBox { Text = _checkLabels[i], AutoSize = true };
                    tlpRows.Controls.Add(checks[i], 2 + i, row);
                }
                _rowChecks.Add(checks);
            }
        }

        private void LoadFromExisting()
        {
            string result = null;
            string desc = null;
            for (int i = 0; i < Math.Min(3, _existingRecords.Count); i++)
            {
                var r = _existingRecords[i];
                _numStands[i].Value = Math.Min(999, Math.Max(0, r.Stand));
                _rowChecks[i][0].Checked = r.Visual;
                _rowChecks[i][1].Checked = r.Damage;
                _rowChecks[i][2].Checked = r.FC1;
                _rowChecks[i][3].Checked = r.FC2;
                _rowChecks[i][4].Checked = r.C_5V_FC1;
                _rowChecks[i][5].Checked = r.C_5V_FC2;
                _rowChecks[i][6].Checked = r.FC_Test_Pass;
                _rowChecks[i][7].Checked = r.Externa_Test_Pass;
                _rowChecks[i][8].Checked = r.Long_Test_Pass;
                if (!string.IsNullOrWhiteSpace(r.Result)) result = r.Result;
                if (!string.IsNullOrWhiteSpace(r.Description)) desc = r.Description;
            }
            if (result != null) txtResult.Text = result;
            if (desc != null) txtNote.Text = desc;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtResult.Text))
            {
                MessageBox.Show("Заполните поле «Результат».", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtResult.Focus();
                return;
            }

            string resultText = txtResult.Text.Trim();
            string descText = string.IsNullOrWhiteSpace(txtNote.Text) ? null : txtNote.Text.Trim();
            bool toRepair = cmbItog.SelectedIndex == 0;

            try
            {
                using (var ctx = ConnectionHelper.CreateContext())
                {
                    var existing = ctx.TestFlight.Where(t => t.TFlightID == _tflightId).ToList();
                    if (existing.Count > 0)
                    {
                        for (int i = 0; i < Math.Min(3, existing.Count); i++)
                            UpdateTestFlight(existing[i], i, resultText, descText);
                        while (existing.Count < 3)
                        {
                            var n = new TestFlight();
                            SetTestFlightFromRow(n, existing.Count, resultText, descText);
                            n.TFlightID = _tflightId;
                            ctx.TestFlight.Add(n);
                            existing.Add(n);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            var n = new TestFlight { TFlightID = _tflightId };
                            SetTestFlightFromRow(n, i, resultText, descText);
                            ctx.TestFlight.Add(n);
                        }
                    }
                    ctx.SaveChanges();

                    if (toRepair)
                    {
                        int productTypeId = ctx.Product.Where(p => p.ProductID == _productId).Select(p => p.TypeID).FirstOrDefault();
                        if (productTypeId == 0)
                            throw new InvalidOperationException("Продукт не найден или не задан тип (TypeID).");

                        if (_existingNonConformityErrorId.HasValue)
                        {
                            int errId = _existingNonConformityErrorId.Value;
                            var desc = ctx.Description.FirstOrDefault(d => d.DescriptionText == resultText);
                            if (desc == null)
                            {
                                desc = new Description { DescriptionText = resultText, TypeID = productTypeId };
                                ctx.Description.Add(desc);
                                ctx.SaveChanges();
                            }
                            try
                            {
                                int cnt = ctx.Database.SqlQuery<int>(
                                    "SELECT COUNT(*) FROM dbo.ErrorDescription WHERE ErrorID = @p0 AND DescriptionID = @p1",
                                    errId, desc.DescriptionID).FirstOrDefault();
                                if (cnt == 0)
                                {
                                    ctx.Database.ExecuteSqlCommand(
                                        "INSERT INTO dbo.ErrorDescription (ErrorID, DescriptionID) VALUES (@p0, @p1)",
                                        errId, desc.DescriptionID);
                                }
                            }
                            catch { }

                            var tmf = ctx.TechnicalMapFull.FirstOrDefault(t => t.ProductID == _productId);
                            var err = ctx.Error.Find(errId);
                            if (err != null && tmf != null)
                                err.TMID = tmf.TMID;
                            ctx.SaveChanges();
                        }
                        else
                        {
                            const int testingPlaceId = 3;
                            var err = new Error
                            {
                                ProductID = _productId,
                                PlaceID = testingPlaceId,
                                TMID = null,
                                Date = DateTime.Now,
                                inProgress = false
                            };
                            ctx.Error.Add(err);
                            ctx.SaveChanges();

                            var desc = ctx.Description.FirstOrDefault(d => d.DescriptionText == resultText);
                            if (desc == null)
                            {
                                desc = new Description { DescriptionText = resultText, TypeID = productTypeId };
                                ctx.Description.Add(desc);
                                ctx.SaveChanges();
                            }
                            try
                            {
                                ctx.Database.ExecuteSqlCommand(
                                    "INSERT INTO dbo.ErrorDescription (ErrorID, DescriptionID) VALUES (@p0, @p1)",
                                    err.ErrorID, desc.DescriptionID);
                            }
                            catch { }

                            var tmf = ctx.TechnicalMapFull.FirstOrDefault(t => t.ProductID == _productId);
                            if (tmf != null)
                            {
                                tmf.Inspection = true;
                                err.TMID = tmf.TMID;
                            }
                            ctx.SaveChanges();

                            NonConformityLabelHelper.OfferGenerateLabel(err.ErrorID, _user?.UserName ?? "", resultText);
                        }
                    }
                    else
                    {
                        var tf = ctx.TechnicalMatFlight.Find(_tflightId);
                        if (tf != null)
                        {
                            tf.Test_Pass = true;
                            ctx.SaveChanges();
                        }
                    }
                }

                _onSaved?.Invoke();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetTestFlightFromRow(TestFlight t, int rowIdx, string resultText, string descText)
        {
            t.Stand = (int)_numStands[rowIdx].Value;
            t.Visual = _rowChecks[rowIdx][0].Checked;
            t.Damage = _rowChecks[rowIdx][1].Checked;
            t.FC1 = _rowChecks[rowIdx][2].Checked;
            t.FC2 = _rowChecks[rowIdx][3].Checked;
            t.C_5V_FC1 = _rowChecks[rowIdx][4].Checked;
            t.C_5V_FC2 = _rowChecks[rowIdx][5].Checked;
            t.FC_Test_Pass = _rowChecks[rowIdx][6].Checked;
            t.Externa_Test_Pass = _rowChecks[rowIdx][7].Checked;
            t.Long_Test_Pass = _rowChecks[rowIdx][8].Checked;
            t.Result = resultText;
            t.Description = descText;
        }

        private void UpdateTestFlight(TestFlight t, int rowIdx, string resultText, string descText)
        {
            t.Stand = (int)_numStands[rowIdx].Value;
            t.Visual = _rowChecks[rowIdx][0].Checked;
            t.Damage = _rowChecks[rowIdx][1].Checked;
            t.FC1 = _rowChecks[rowIdx][2].Checked;
            t.FC2 = _rowChecks[rowIdx][3].Checked;
            t.C_5V_FC1 = _rowChecks[rowIdx][4].Checked;
            t.C_5V_FC2 = _rowChecks[rowIdx][5].Checked;
            t.FC_Test_Pass = _rowChecks[rowIdx][6].Checked;
            t.Externa_Test_Pass = _rowChecks[rowIdx][7].Checked;
            t.Long_Test_Pass = _rowChecks[rowIdx][8].Checked;
            t.Result = resultText;
            t.Description = descText;
        }
    }
}
