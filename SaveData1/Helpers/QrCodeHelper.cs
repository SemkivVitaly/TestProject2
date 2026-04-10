using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using QRCoder;

namespace SaveData1.Helpers
{
    /// <summary>Генерация и сохранение стилизованных QR-кодов (чёрная карточка, белый QR, подпись).</summary>
    public static class QrCodeHelper
    {
        /// <summary>Генерация стилизованного QR-кода: белый QR на чёрной карточке с подписью.</summary>
        public static Bitmap GenerateStyledQrCode(string data, int pixelsPerModule = 20)
        {
            using (var generator = new QRCodeGenerator())
            {
                QRCodeData qrData = generator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new QRCode(qrData))
                {
                    Bitmap qrBitmap = qrCode.GetGraphic(pixelsPerModule, Color.Black, Color.White, false);

                    int quietZone = pixelsPerModule * 3;
                    int whiteBoxSize = qrBitmap.Width + quietZone * 2;
                    int cornerRadius = pixelsPerModule;
                    int padding = pixelsPerModule * 2;
                    int labelHeight = pixelsPerModule * 6;

                    int totalWidth = whiteBoxSize + padding * 2;
                    int totalHeight = whiteBoxSize + padding + labelHeight;

                    var result = new Bitmap(totalWidth, totalHeight, PixelFormat.Format32bppArgb);

                    using (var g = Graphics.FromImage(result))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                        
                        g.Clear(Color.Black);
                        var whiteRect = new Rectangle(padding, padding, whiteBoxSize, whiteBoxSize);
                        using (var path = CreateRoundedRectPath(whiteRect, cornerRadius))
                        using (var brush = new SolidBrush(Color.White))
                        {
                            g.FillPath(brush, path);
                        }

                        int qrX = padding + quietZone;
                        int qrY = padding + quietZone;
                        g.DrawImage(qrBitmap, qrX, qrY, qrBitmap.Width, qrBitmap.Height);

                        var labelRect = new RectangleF(0, padding + whiteBoxSize, totalWidth, labelHeight);

                        float fontSize = Math.Min(labelHeight * 0.55f, totalWidth / (data.Length * 0.6f));
                        fontSize = Math.Max(fontSize, 16f);

                        using (var font = new Font("Arial", fontSize, FontStyle.Bold))
                        using (var brush = new SolidBrush(Color.White))
                        using (var sf = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        })
                        {
                            g.DrawString(data, font, brush, labelRect, sf);
                        }
                    }

                    qrBitmap.Dispose();
                    return result;
                }
            }
        }

        /// <summary>Сохраняет стилизованный QR-код в файл.</summary>
        public static void SaveQrCode(string data, string filePath, int pixelsPerModule = 20)
        {
            using (var bitmap = GenerateStyledQrCode(data, pixelsPerModule))
            {
                string dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                bitmap.Save(filePath, ImageFormat.Png);
            }
        }

        private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
