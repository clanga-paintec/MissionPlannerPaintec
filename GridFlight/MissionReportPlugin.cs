using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using log4net;
using MissionPlanner;
using MissionPlanner.Plugin;
using MissionPlanner.Utilities;
using SkiaSharp;

namespace GridFlight
{
    /// <summary>
    /// Plugin de reporte de misión post-vuelo para GridFlight.
    ///
    /// Rastrea estadísticas durante el vuelo (altitud máx, velocidad máx,
    /// batería consumida, modos usados, distancia) y genera un reporte
    /// HTML con resumen completo al pulsar el botón del toolbar.
    ///
    /// Activo para AMBOS perfiles.
    /// No modifica ningún archivo del código fuente de MissionPlanner.
    /// </summary>
    public class MissionReportPlugin : MissionPlanner.Plugin.Plugin
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MissionReportPlugin));

        public override string Name    => "GridFlight - Mission Report";
        public override string Version => "1.0";
        public override string Author  => "GridFlight";

        // ── Estado de rastreo ─────────────────────────────────────────────

        private bool   _wasArmed;
        private double _maxAlt;
        private double _maxSpeed;
        private double _startVoltage;
        private double _endVoltage;
        private int    _startBatteryPct;
        private double _startMah;
        private string _lastMode = "";
        private DateTime _armTime;
        private readonly List<string> _modesUsed = new List<string>();

        private static string ReportsDirectory
        {
            get
            {
                var dir = Path.Combine(GridFlightProfile.ConfigsDirectory, "reports");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public override bool Init() => true; // Ambos perfiles

        public override bool Loaded()
        {
            AddToolbarButton();
            loopratehz = 1f; // 1 Hz para rastreo de stats
            return true;     // Registrar en loop
        }

        public override bool Exit() => true;

        // ── Loop: rastreo de datos de vuelo ───────────────────────────────

        public override bool Loop()
        {
            try
            {
                var cs = MainV2.comPort.MAV.cs;
                bool armed = cs.armed;

                // Detectar transición a armado → iniciar rastreo
                if (armed && !_wasArmed)
                {
                    _maxAlt          = 0;
                    _maxSpeed        = 0;
                    _startVoltage    = cs.battery_voltage;
                    _startBatteryPct = cs.battery_remaining;
                    _startMah        = cs.battery_usedmah;
                    _armTime         = DateTime.Now;
                    _lastMode        = "";
                    _modesUsed.Clear();
                }

                // Mientras armado → registrar máximos y modos
                if (armed)
                {
                    if (cs.alt > _maxAlt)
                        _maxAlt = cs.alt;
                    if (cs.groundspeed > _maxSpeed)
                        _maxSpeed = cs.groundspeed;

                    var currentMode = cs.mode;
                    if (!string.IsNullOrEmpty(currentMode) && currentMode != _lastMode)
                    {
                        _lastMode = currentMode;
                        if (!_modesUsed.Contains(currentMode))
                            _modesUsed.Add(currentMode);
                    }
                }

                // Detectar transición a desarmado → guardar voltaje final
                if (!armed && _wasArmed)
                {
                    _endVoltage = cs.battery_voltage;
                }

                _wasArmed = armed;
            }
            catch
            {
                // Silenciar errores de telemetría (vehículo puede estar desconectado)
            }

            return true;
        }

        // ── Botón en toolbar ──────────────────────────────────────────────

        private void AddToolbarButton()
        {
            var mainMenu = Host.MainForm.Controls.Find("MainMenu", true);
            if (mainMenu.Length == 0 || !(mainMenu[0] is ToolStrip toolStrip))
                return;

            var btn = new ToolStripButton
            {
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                TextImageRelation = TextImageRelation.ImageAboveText,
                Text = "REPORT",
                Font = new Font("Segoe UI", 8f),
                ToolTipText  = "Mission Report",
                Margin       = new Padding(4, 1, 0, 2),
                Image        = RenderReportIcon(24)
            };
            btn.Click += BtnReport_Click;

            int logoIndex = toolStrip.Items.IndexOf(Host.MainForm.MenuArduPilot);
            if (logoIndex >= 0)
                toolStrip.Items.Insert(logoIndex + 1, btn);
            else
                toolStrip.Items.Add(btn);
        }

        // ── Generación de reporte ─────────────────────────────────────────

        private void BtnReport_Click(object sender, EventArgs e)
        {
            try
            {
                var cs = MainV2.comPort.MAV.cs;

                // Datos actuales (pueden ser post-vuelo o en tiempo real)
                var flightTime   = cs.timeInAir;
                var distance     = cs.distTraveled;
                var mahUsed      = cs.battery_usedmah - _startMah;
                var endBattPct   = cs.battery_remaining;
                var endVolt      = _endVoltage > 0 ? _endVoltage : cs.battery_voltage;
                var startVolt    = _startVoltage > 0 ? _startVoltage : endVolt;
                var startPct     = _startBatteryPct > 0 ? _startBatteryPct : endBattPct;
                var modes        = _modesUsed.Count > 0
                    ? string.Join(" → ", _modesUsed)
                    : cs.mode;

                var html = GenerateHtml(
                    flightTime, distance, _maxAlt, _maxSpeed,
                    startVolt, endVolt, startPct, endBattPct,
                    mahUsed, modes, _armTime);

                var fileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm") + "_mission_report.html";
                var filePath = Path.Combine(ReportsDirectory, fileName);
                File.WriteAllText(filePath, html, Encoding.UTF8);

                // Abrir en navegador del sistema
                System.Diagnostics.Process.Start(filePath);

                log.Info("MissionReport: generated " + filePath);
            }
            catch (Exception ex)
            {
                log.Error("MissionReport: failed to generate report", ex);
                MessageBox.Show(
                    "Error al generar el reporte: " + ex.Message,
                    "GridFlight - Mission Report",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static string GenerateHtml(
            float flightTimeSec, float distanceM, double maxAltM,
            double maxSpeedMs, double startVolt, double endVolt,
            int startBattPct, int endBattPct, double mahUsed,
            string modes, DateTime armTime)
        {
            var mins = (int)(flightTimeSec / 60);
            var secs = (int)(flightTimeSec % 60);
            var distKm = distanceM / 1000.0;
            var maxSpeedKmh = maxSpeedMs * 3.6;
            var ci = CultureInfo.InvariantCulture;

            return $@"<!DOCTYPE html>
<html lang=""es"">
<head>
<meta charset=""UTF-8"">
<title>GridFlight - Reporte de Mision</title>
<style>
  * {{ margin: 0; padding: 0; box-sizing: border-box; }}
  body {{
    font-family: 'Segoe UI', sans-serif;
    background: #181818; color: #F5F5F5;
    padding: 40px; max-width: 700px; margin: 0 auto;
  }}
  h1 {{ color: #FFC107; font-size: 24px; margin-bottom: 5px; }}
  .subtitle {{ color: #AAAAAA; font-size: 13px; margin-bottom: 30px; }}
  .section {{ margin-bottom: 25px; }}
  .section h2 {{
    color: #FFC107; font-size: 15px; text-transform: uppercase;
    letter-spacing: 1px; border-bottom: 1px solid #333;
    padding-bottom: 6px; margin-bottom: 12px;
  }}
  .grid {{ display: grid; grid-template-columns: 1fr 1fr; gap: 10px 30px; }}
  .stat {{ padding: 8px 0; }}
  .stat .label {{ color: #AAAAAA; font-size: 12px; }}
  .stat .value {{ font-size: 20px; font-weight: bold; }}
  .stat .unit {{ color: #AAAAAA; font-size: 13px; }}
  .modes {{
    background: #212121; border-radius: 6px;
    padding: 12px 16px; font-size: 14px; color: #FFD54F;
  }}
  .footer {{
    margin-top: 40px; padding-top: 15px;
    border-top: 1px solid #333; color: #666; font-size: 11px;
  }}
</style>
</head>
<body>

<h1>Reporte de Mision</h1>
<div class=""subtitle"">
  {(armTime != DateTime.MinValue ? armTime.ToString("dd/MM/yyyy HH:mm") : DateTime.Now.ToString("dd/MM/yyyy HH:mm"))}
  &nbsp;|&nbsp; GridFlight Mission Report
</div>

<div class=""section"">
  <h2>Vuelo</h2>
  <div class=""grid"">
    <div class=""stat"">
      <div class=""label"">Duracion</div>
      <div class=""value"">{mins}<span class=""unit"">m</span> {secs}<span class=""unit"">s</span></div>
    </div>
    <div class=""stat"">
      <div class=""label"">Distancia total</div>
      <div class=""value"">{distKm.ToString("F2", ci)}<span class=""unit""> km</span></div>
    </div>
    <div class=""stat"">
      <div class=""label"">Altitud maxima</div>
      <div class=""value"">{maxAltM.ToString("F1", ci)}<span class=""unit""> m</span></div>
    </div>
    <div class=""stat"">
      <div class=""label"">Velocidad maxima</div>
      <div class=""value"">{maxSpeedKmh.ToString("F1", ci)}<span class=""unit""> km/h</span></div>
    </div>
  </div>
</div>

<div class=""section"">
  <h2>Bateria</h2>
  <div class=""grid"">
    <div class=""stat"">
      <div class=""label"">Voltaje inicio</div>
      <div class=""value"">{startVolt.ToString("F1", ci)}<span class=""unit""> V</span></div>
    </div>
    <div class=""stat"">
      <div class=""label"">Voltaje final</div>
      <div class=""value"">{endVolt.ToString("F1", ci)}<span class=""unit""> V</span></div>
    </div>
    <div class=""stat"">
      <div class=""label"">Nivel inicio → final</div>
      <div class=""value"">{startBattPct}<span class=""unit"">%</span> → {endBattPct}<span class=""unit"">%</span></div>
    </div>
    <div class=""stat"">
      <div class=""label"">Consumo</div>
      <div class=""value"">{mahUsed.ToString("F0", ci)}<span class=""unit""> mAh</span></div>
    </div>
  </div>
</div>

<div class=""section"">
  <h2>Modos de vuelo utilizados</h2>
  <div class=""modes"">{modes}</div>
</div>

<div class=""footer"">
  Generado por GridFlight Mission Report v1.0 &mdash; {DateTime.Now:dd/MM/yyyy HH:mm:ss}
</div>

</body>
</html>";
        }

        // ── Icono documento renderizado con SkiaSharp ─────────────────────

        private static Image RenderReportIcon(int size)
        {
            try
            {
                using (var surface = SKSurface.Create(
                    new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Premul)))
                {
                    var canvas = surface.Canvas;
                    canvas.Clear(SKColors.Transparent);

                    float s = size;
                    float pad = s * 0.12f;

                    using (var paint = new SKPaint
                    {
                        Color       = new SKColor(255, 193, 7),
                        IsAntialias = true,
                        Style       = SKPaintStyle.Stroke,
                        StrokeWidth = s * 0.08f,
                        StrokeCap   = SKStrokeCap.Round,
                        StrokeJoin  = SKStrokeJoin.Round
                    })
                    {
                        // Página con esquina doblada
                        var page = new SKPath();
                        float foldSize = s * 0.2f;
                        page.MoveTo(pad, pad);
                        page.LineTo(s - pad - foldSize, pad);
                        page.LineTo(s - pad, pad + foldSize);
                        page.LineTo(s - pad, s - pad);
                        page.LineTo(pad, s - pad);
                        page.Close();
                        canvas.DrawPath(page, paint);

                        // Doblez
                        canvas.DrawLine(s - pad - foldSize, pad,
                                        s - pad - foldSize, pad + foldSize, paint);
                        canvas.DrawLine(s - pad - foldSize, pad + foldSize,
                                        s - pad, pad + foldSize, paint);

                        // Líneas de texto
                        paint.StrokeWidth = s * 0.06f;
                        float lineX1 = pad + s * 0.12f;
                        float lineX2 = s - pad - s * 0.12f;
                        canvas.DrawLine(lineX1, s * 0.45f, lineX2, s * 0.45f, paint);
                        canvas.DrawLine(lineX1, s * 0.58f, lineX2, s * 0.58f, paint);
                        canvas.DrawLine(lineX1, s * 0.71f, lineX2 * 0.7f, s * 0.71f, paint);
                    }

                    using (var image = surface.Snapshot())
                    using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                    {
                        var ms = new MemoryStream(data.ToArray());
                        return Image.FromStream(ms);
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
