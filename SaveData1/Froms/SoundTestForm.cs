using System;
using System.Linq;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace SaveData1
{
    public partial class SoundTestForm : Form
    {
        private WaveInEvent waveIn;
        private Timer timer;
        private bool isRecording = false;

        public SoundTestForm()
        {
            InitializeComponent();
            LoadDevices();

            timer = new Timer();
            timer.Interval = 100;
            timer.Tick += Timer_Tick;
        }

        private void LoadDevices()
        {
            try
            {
                int waveInDevices = WaveIn.DeviceCount;
                for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
                {
                    WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(waveInDevice);
                    cmbDevices.Items.Add(deviceInfo.ProductName);
                }

                if (cmbDevices.Items.Count > 0)
                {
                    cmbDevices.SelectedIndex = 0;
                }
                else
                {
                    MessageBox.Show("Устройства ввода звука не найдены.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    btnStartStop.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при получении устройств: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnStartStop_Click(object sender, EventArgs e)
        {
            if (isRecording)
            {
                StopRecording();
            }
            else
            {
                StartRecording();
            }
        }

        private void StartRecording()
        {
            if (cmbDevices.SelectedIndex < 0) return;

            try
            {
                waveIn = new WaveInEvent();
                waveIn.DeviceNumber = cmbDevices.SelectedIndex;
                waveIn.WaveFormat = new WaveFormat(44100, 1); // 44.1kHz, mono
                waveIn.DataAvailable += WaveIn_DataAvailable;
                
                waveIn.StartRecording();
                timer.Start();

                isRecording = true;
                btnStartStop.Text = "Стоп";
                cmbDevices.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при запуске записи: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopRecording()
        {
            if (waveIn != null)
            {
                waveIn.StopRecording();
                waveIn.Dispose();
                waveIn = null;
            }
            timer.Stop();

            isRecording = false;
            btnStartStop.Text = "Старт";
            cmbDevices.Enabled = true;

            MessageBox.Show($"Финальный результат:\nГромкость: {lblDb.Text}\nЧастота: {lblHz.Text}", "Результат проверки звука", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private float maxLoudness = 0;
        private double currentHz = 0;

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            // Calculate RMS and max for DB mapping
            float max = 0;
            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                short sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i]);
                float sample32 = sample / 32768f;
                if (sample32 < 0) sample32 = -sample32;
                if (sample32 > max) max = sample32;
            }
            
            maxLoudness = max;

            // Naive Zero-crossing frequency estimation
            int zeroCrossings = 0;
            bool lastPositive = false;
            
            if (e.BytesRecorded >= 2)
            {
                short firstSample = (short)((e.Buffer[1] << 8) | e.Buffer[0]);
                lastPositive = firstSample >= 0;
            }

            for (int i = 2; i < e.BytesRecorded; i += 2)
            {
                short sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i]);
                bool positive = sample >= 0;

                if (positive != lastPositive)
                {
                    zeroCrossings++;
                    lastPositive = positive;
                }
            }

            double durationInSeconds = (double)e.BytesRecorded / 2 / waveIn.WaveFormat.SampleRate;
            currentHz = (zeroCrossings / 2.0) / durationInSeconds;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (isRecording)
            {
                // Convert linear amplitude to approx DB, 0 dBFS is max
                double db = 20 * Math.Log10(maxLoudness + 0.0001); // offset to avoid -Infinity
                lblDb.Text = $"{db:F2} дБFS";
                lblHz.Text = $"{currentHz:F2} Гц";
            }
        }

        private void SoundTestForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isRecording)
            {
                StopRecording();
            }
        }
    }
}