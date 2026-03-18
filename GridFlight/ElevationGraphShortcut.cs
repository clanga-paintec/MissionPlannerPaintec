using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MissionPlanner;
using MissionPlanner.Plugin;
using MissionPlanner.Controls;
using MissionPlanner.Utilities;
using GridFlight;
using SkiaSharp;

namespace ElevationGraphShortcut
{
    public class Plugin : MissionPlanner.Plugin.Plugin
    {
        private ToolStripButton _btnElevation;

        public override string Name    => "Elevation Graph Shortcut";
        public override string Version => "1.0";
        public override string Author  => "GridFlight";

        // Frecuencia del loop en Hz: suficiente para una respuesta visual rápida
        // sin desperdiciar CPU. Se usa para actualizar la visibilidad del botón.
        private const float _checkRateHz = 2f;

        public override bool Init() => GridFlightProfile.IsPilot;

        public override bool Loaded()
        {
            _btnElevation = new ToolStripButton();
            _btnElevation.DisplayStyle = ToolStripItemDisplayStyle.Image;
            _btnElevation.Image        = RenderElevationIcon(48);
            _btnElevation.ToolTipText  = "Open Elevation Profile for current mission";
            _btnElevation.Margin       = new Padding(4, 1, 0, 2);
            // Oculto por defecto; Loop() lo muestra cuando hay un plan cargado.
            _btnElevation.Visible      = false;
            _btnElevation.Click       += BtnElevation_Click;

            var mainMenu = Host.MainForm.Controls.Find("MainMenu", true);
            if (mainMenu.Length > 0 && mainMenu[0] is ToolStrip toolStrip)
                toolStrip.Items.Add(_btnElevation);

            loopratehz = _checkRateHz;
            return true; // true = registrar en el loop activo de plugins
        }

        /// <summary>
        /// Actualiza la visibilidad del botón según si hay waypoints cargados.
        /// Un plan válido tiene al menos 2 filas en Commands (fila 0 = HOME).
        /// Loop() corre en hilo de fondo; la actualización de UI se delega a BeginInvoke.
        /// </summary>
        public override bool Loop()
        {
            bool hasPlan = Host.MainForm.FlightPlanner.Commands.Rows.Count > 1;

            if (_btnElevation.Visible != hasPlan)
                Host.MainForm.BeginInvoke((MethodInvoker)(() => _btnElevation.Visible = hasPlan));

            return true;
        }

        public override bool Exit() => true;

        private void BtnElevation_Click(object sender, EventArgs e)
        {
            try
            {
                Host.MainForm.FlightPlanner.elevationGraphToolStripMenuItem_Click(sender, e);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(
                    "Could not open Elevation Graph.\n" +
                    "Make sure you have waypoints loaded in the Flight Planner.\n\n" +
                    "Error: " + ex.Message,
                    "Elevation Graph",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Renders the ElevationGraphIcon.svg path into a GDI+ Bitmap using SkiaSharp.
        ///
        /// SVG viewBox "0 -960 960 960": the coordinate space spans x∈[0,960], y∈[-960,0].
        /// To map this to screen space (origin top-left, y increasing downward) we apply
        /// Translate(0, 960) first, then Scale(size/960). Both transforms are concatenated
        /// via the SkiaSharp CTM so points are mapped as: result = (x·s, (y+960)·s).
        /// </summary>
        private static Bitmap RenderElevationIcon(int size)
        {
            const string pathData =
                "M730-490v-181l-74 73-42-42 146-146 146 146-42 43-74-74v181h-60Z" +
                "M40-80l240-320 195 260h325L560-459 435-293l-38-50 163-217L920-80H40Zm395-60Z";

            Bitmap bitmap;

            using (var skBitmap = new SKBitmap(size, size, SKColorType.Bgra8888, SKAlphaType.Premul))
            {
                using (var canvas = new SKCanvas(skBitmap))
                {
                    canvas.Clear(SKColors.Transparent);

                    float scale = size / 960f;
                    canvas.Scale(scale, scale);
                    canvas.Translate(0, 960);

                    using (var path = SKPath.ParseSvgPathData(pathData))
                    using (var paint = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Fill })
                    {
                        canvas.DrawPath(path, paint);
                    }
                }

                // Copy Skia BGRA8888 pixels into GDI+ Format32bppArgb (same byte order on Windows).
                bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
                var bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, size, size),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppArgb);

                byte[] pixels = skBitmap.Bytes;
                Marshal.Copy(pixels, 0, bmpData.Scan0, pixels.Length);

                bitmap.UnlockBits(bmpData);
            }

            return bitmap;
        }
    }
}
