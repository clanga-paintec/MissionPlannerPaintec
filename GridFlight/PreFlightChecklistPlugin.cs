using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using log4net;
using MissionPlanner;
using MissionPlanner.Plugin;
using MissionPlanner.Utilities;
using SkiaSharp;

namespace GridFlight
{
    /// <summary>
    /// Plugin de Pre-Flight Checklist para GridFlight.
    ///
    /// - Habilita el tab PreFlight en FlightData para ambos perfiles.
    /// - Añade un botón de acceso rápido al toolbar (icono clipboard ámbar).
    /// - Aprovecha la infraestructura existente de MissionPlanner
    ///   (CheckListControl + CheckListItem + checklistDefault.xml).
    ///
    /// Activo para AMBOS perfiles (la seguridad pre-vuelo es universal).
    /// </summary>
    public class PreFlightChecklistPlugin : MissionPlanner.Plugin.Plugin
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(PreFlightChecklistPlugin));

        public override string Name    => "GridFlight - Pre-Flight Checklist";
        public override string Version => "1.0";
        public override string Author  => "GridFlight";

        public override bool Init()
        {
            // Garantizar que el tab PreFlight sea visible para ambos perfiles.
            // Patrón idéntico a HideSetupMenuItemsPlugin (flags en Init).
            MainV2.DisplayConfiguration.displayPreFlightTab = true;
            MainV2.DisplayConfiguration.displayPreFlightTabEdit = true;
            return true;
        }

        public override bool Loaded()
        {
            DeployDefaultChecklist();
            AddToolbarButton();
            return false; // Sin loop periódico
        }

        public override bool Exit() => true;

        // ── Despliegue de checklist default en runtime ────────────────────

        /// <summary>
        /// Copia GridFlight/configs/checklistGridFlight.xml a
        /// UserDataDirectory/checklist.xml SOLO si el usuario no tiene
        /// uno personalizado. Esto evita tocar archivos del código fuente
        /// de MissionPlanner (Open/Closed). CheckListControl.LoadConfig()
        /// prioriza checklist.xml sobre checklistDefault.xml.
        /// </summary>
        private static void DeployDefaultChecklist()
        {
            try
            {
                var userChecklist = Settings.GetUserDataDirectory() + "checklist.xml";
                if (File.Exists(userChecklist))
                    return; // Respetar customizaciones del usuario

                var source = Path.Combine(
                    Settings.GetRunningDirectory(),
                    "GridFlight", "configs", "checklistGridFlight.xml");

                if (!File.Exists(source))
                {
                    log.Warn("PreFlightChecklist: GridFlight checklist XML not found at " + source);
                    return;
                }

                File.Copy(source, userChecklist);
                log.Info("PreFlightChecklist: deployed GridFlight default checklist");
            }
            catch (Exception ex)
            {
                log.Error("PreFlightChecklist: failed to deploy default checklist", ex);
            }
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
                ToolTipText  = "Pre-Flight Checklist",
                Margin       = new Padding(4, 1, 0, 2),
                Image        = RenderChecklistIcon(18),
                TextImageRelation = TextImageRelation.ImageAboveText,
                Text = "CHECKLIST",
                Font = new Font("Segoe UI", 8f)
            };
            btn.Click += BtnPreFlight_Click;

            // Insertar después del logo (consistente con otros plugins)
            int logoIndex = toolStrip.Items.IndexOf(Host.MainForm.MenuArduPilot);
            if (logoIndex >= 0)
                toolStrip.Items.Insert(logoIndex + 1, btn);
            else
                toolStrip.Items.Add(btn);
        }

        // ── Navegación a tab PreFlight ────────────────────────────────────

        private void BtnPreFlight_Click(object sender, EventArgs e)
        {
            // Navegar a FlightData primero (el tab PreFlight solo existe ahí)
            MainV2.View.ShowScreen("FlightData");

            // Diferir la selección de tab al siguiente ciclo de UI para que
            // FlightData tenga tiempo de reconstruir su tabControlactions.
            Host.MainForm.BeginInvoke((MethodInvoker)TrySelectPreFlightTab);
        }

        private void TrySelectPreFlightTab()
        {
            var fd = Host.MainForm.FlightData;
            if (fd == null) return;

            var tabs = fd.Controls.Find("tabControlactions", true);
            if (tabs.Length == 0 || !(tabs[0] is TabControl tabControl))
                return;

            foreach (TabPage tab in tabControl.TabPages)
            {
                if (tab.Name == "tabPagePreFlight")
                {
                    tabControl.SelectedTab = tab;
                    break;
                }
            }
        }

        // ── Icono clipboard renderizado con SkiaSharp ─────────────────────

        /// <summary>
        /// Renderiza un icono de clipboard con checkmark en ámbar GridFlight.
        /// Patrón idéntico a FavoriteConfigsPlugin (estrella SkiaSharp).
        /// </summary>
        private static Image RenderChecklistIcon(int size)
        {
            try
            {
                using (var surface = SKSurface.Create(
                    new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Premul)))
                {
                    var canvas = surface.Canvas;
                    canvas.Clear(SKColors.Transparent);

                    float s = size;
                    float pad = s * 0.1f;

                    // Clipboard body
                    using (var paint = new SKPaint
                    {
                        Color       = new SKColor(255, 193, 7), // Ámbar
                        IsAntialias = true,
                        Style       = SKPaintStyle.Stroke,
                        StrokeWidth = s * 0.08f
                    })
                    {
                        var body = new SKRect(pad, s * 0.2f, s - pad, s - pad);
                        canvas.DrawRoundRect(body, s * 0.08f, s * 0.08f, paint);

                        // Clip tab (rectángulo superior central)
                        float tabW = s * 0.35f;
                        float tabH = s * 0.15f;
                        float tabX = (s - tabW) / 2f;
                        var clipTab = new SKRect(tabX, pad, tabX + tabW, pad + tabH);
                        canvas.DrawRoundRect(clipTab, s * 0.04f, s * 0.04f, paint);
                    }

                    // Checkmark
                    using (var paint = new SKPaint
                    {
                        Color       = new SKColor(255, 193, 7),
                        IsAntialias = true,
                        Style       = SKPaintStyle.Stroke,
                        StrokeWidth = s * 0.1f,
                        StrokeCap   = SKStrokeCap.Round,
                        StrokeJoin  = SKStrokeJoin.Round
                    })
                    {
                        var checkPath = new SKPath();
                        checkPath.MoveTo(s * 0.28f, s * 0.55f);
                        checkPath.LineTo(s * 0.45f, s * 0.72f);
                        checkPath.LineTo(s * 0.72f, s * 0.38f);
                        canvas.DrawPath(checkPath, paint);
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
