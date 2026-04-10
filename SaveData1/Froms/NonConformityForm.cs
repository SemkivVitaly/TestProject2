using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SaveData1.Entity;
using SaveData1.Helpers;

namespace SaveData1
{
    /// <summary>Форма создания ярлыка несоответствия. Склад: showPlaceChoice=true — выбор приёмка (1) / отгрузка (4).
    /// Выбор продукта у сотрудника и «Ярлык несоответствия»: showPlaceChoice=false, fixedPlaceId=1.</summary>
    public partial class NonConformityForm : Form
    {
        private readonly int _productId;
        private readonly string _fio;
        private readonly bool _showPlaceChoice;
        private readonly int _fixedPlaceId;
        private List<Description> _descriptions = new List<Description>();

        public NonConformityForm(int productId, string serial, string category, string actNumber,
            string fio, bool showPlaceChoice = true, int fixedPlaceId = 1)
        {
            InitializeComponent();
            _productId = productId;
            _fio = fio;
            _showPlaceChoice = showPlaceChoice;
            _fixedPlaceId = fixedPlaceId;

            txtSerial.Text = serial ?? "";
            txtCategory.Text = category ?? "";
            txtAct.Text = actNumber ?? "";

            grpPlace.Visible = showPlaceChoice;
            if (!showPlaceChoice)
            {
                lblDescriptions.Top = grpPlace.Top;
                clbDescriptions.Top = lblDescriptions.Bottom + 3;
                btnSave.Top = clbDescriptions.Bottom + 10;
                btnCancel.Top = btnSave.Top;
                this.ClientSize = new System.Drawing.Size(this.ClientSize.Width, btnSave.Bottom + 12);
            }

            LoadDescriptions();
        }

        private void LoadDescriptions()
        {
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    _descriptions = context.Description.ToList();
                    clbDescriptions.Items.Clear();
                    foreach (var d in _descriptions)
                    {
                        clbDescriptions.Items.Add(d.DescriptionText, false);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки описаний: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (clbDescriptions.CheckedIndices.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы одну неисправность.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                int placeId;
                if (_showPlaceChoice)
                    placeId = rbShipping.Checked ? 4 : 1;
                else
                    placeId = _fixedPlaceId;

                var selectedDescriptions = new List<Description>();
                foreach (int idx in clbDescriptions.CheckedIndices)
                {
                    if (idx < _descriptions.Count)
                        selectedDescriptions.Add(_descriptions[idx]);
                }

                int errorId;
                using (var context = ConnectionHelper.CreateContext())
                {
                    var error = new Error
                    {
                        TMID = null,
                        ProductID = _productId,
                        PlaceID = placeId,
                        Date = DateTime.Now,
                        inProgress = false
                    };
                    context.Error.Add(error);
                    context.SaveChanges();
                    errorId = error.ErrorID;
                }

                var descIds = selectedDescriptions.Select(d => d.DescriptionID).ToList();
                FaultDescriptionHelper.SetErrorDescriptions(errorId, descIds);

                string defectText = NonConformityLabelHelper.BuildDefectText(selectedDescriptions);
                string placeName;
                using (var context = ConnectionHelper.CreateContext())
                {
                    placeName = context.Place.Where(p => p.PlaceID == placeId).Select(p => p.PlaceName).FirstOrDefault() ?? "";
                }

                NonConformityLabelHelper.GenerateLabel(
                    errorId, txtSerial.Text, txtCategory.Text, placeName,
                    defectText, DateTime.Now, txtAct.Text, _fio,
                    "__________", null);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка создания ярлыка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
