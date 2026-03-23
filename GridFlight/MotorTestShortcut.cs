using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MissionPlanner;
using MissionPlanner.Controls.BackstageView;
using MissionPlanner.GCSViews;
using MissionPlanner.Plugin;
using MissionPlanner.Utilities;

namespace GridFlight
{
    /// <summary>
    /// Adds a Motor Test shortcut button to the main toolbar, placed immediately
    /// after the GridFlight logo (MenuArduPilot).
    ///
    /// Clicking the button navigates to the Setup screen (HWConfig) and then
    /// activates the Motor Test backstage page if the vehicle is connected and
    /// parameters have been received.
    ///
    /// Why Insert() instead of Add():
    ///   The ElevationGraph plugin also calls Items.Add() from its own Loaded().
    ///   Using Insert(logoIndex + 1, ...) guarantees Motor Test always lands
    ///   immediately after the logo regardless of plugin load order.
    /// </summary>
    public class MotorTestShortcut : MissionPlanner.Plugin.Plugin
    {
        public override string Name    => "GridFlight - Motor Test Shortcut";
        public override string Version => "1.0";
        public override string Author  => "GridFlight";

        private ToolStripButton _btn;

        // Frecuencia del loop en Hz: suficiente para una respuesta visual rápida
        // sin desperdiciar CPU. Se usa para actualizar la visibilidad del botón.
        private const float _checkRateHz = 2f;

        public override bool Init()   => true;
        public override bool Exit()   => true;

        public override bool Loaded()
        {
            var mainMenu = Host.MainForm.Controls.Find("MainMenu", true);
            if (mainMenu.Length == 0 || !(mainMenu[0] is ToolStrip toolStrip))
                return false;

            _btn = new ToolStripButton();
            _btn.DisplayStyle = ToolStripItemDisplayStyle.Image;
            _btn.ToolTipText  = GridFlightProfile.IsMechanic
                ? "Motor Test (conectar vehículo primero)"
                : "Motor Test (Setup → Optional Hardware)";
            // Left margin gives breathing room from the logo; no excessive gap.
            _btn.Margin       = new Padding(8, 1, 0, 2);
            _btn.Image        = LoadEngineIcon();
            // Oculto por defecto; Loop() lo muestra solo cuando SITL está activo.
            _btn.Visible      = false;
            _btn.Click       += BtnMotorTest_Click;

            // Insert right after the logo so the button is always logo-adjacent.
            int logoIndex = toolStrip.Items.IndexOf(Host.MainForm.MenuArduPilot);
            if (logoIndex >= 0)
                toolStrip.Items.Insert(logoIndex + 1, _btn);
            else
                toolStrip.Items.Add(_btn);

            loopratehz = _checkRateHz;
            return true; // true = registrar en el loop activo de plugins
        }

        /// <summary>
        /// Actualiza la visibilidad del botón según si hay una simulación SITL activa.
        /// SITLSEND es internal static UdpClient (SITL.cs:53); cuando está conectado
        /// indica que el simulador está en marcha.
        /// Loop() corre en hilo de fondo; la actualización de UI se delega a BeginInvoke.
        /// </summary>
        public override bool Loop()
        {
            bool shouldShow;

            if (GridFlightProfile.IsMechanic)
            {
                // Mecánico: visible cuando hay vehículo conectado (real o SITL)
                shouldShow = MainV2.comPort.BaseStream.IsOpen;
            }
            else
            {
                // Piloto: visible solo durante simulación SITL (seguridad)
                shouldShow = MissionPlanner.GCSViews.SITL.SITLSEND != null
                             && MissionPlanner.GCSViews.SITL.SITLSEND.Client.Connected;
            }

            if (_btn.Visible != shouldShow)
                Host.MainForm.BeginInvoke((MethodInvoker)(() => _btn.Visible = shouldShow));

            return true;
        }

        private void BtnMotorTest_Click(object sender, EventArgs e)
        {
            // Navigate to Setup first; the pages are populated synchronously during Load.
            MainV2.View.ShowScreen("HWConfig");

            // Defer page activation to the next UI pump cycle so InitialSetup
            // has fully run HardwareConfig_Load() before we query its pages.
            Host.MainForm.BeginInvoke((MethodInvoker)TryActivateMotorTestPage);
        }

        /// <summary>
        /// Finds the Motor Test BackstageViewPage and activates it.
        /// If the vehicle is not connected the page won't be present (it is only
        /// added when isConnected &amp;&amp; gotAllParams), so we silently return —
        /// the user will see the default Setup page instead.
        /// </summary>
        private void TryActivateMotorTestPage()
        {
            var hwConfigScreen = MainV2.View.screens
                .FirstOrDefault(s => s.Name == "HWConfig");

            var setup = hwConfigScreen?.Control as InitialSetup;
            if (setup?.backstageView == null || setup.backstageView.Pages.Count == 0)
                return;

            BackstageViewPage motorTestPage = null;
            foreach (BackstageViewPage page in setup.backstageView.Pages)
            {
                if (page.Show && page.LinkText != null &&
                    page.LinkText.Equals("Motor Test", StringComparison.OrdinalIgnoreCase))
                {
                    motorTestPage = page;
                    break;
                }
            }

            if (motorTestPage != null)
                setup.backstageView.ActivatePage(motorTestPage);
        }

        /// <summary>
        /// Loads GridFlight/assets/engine.png from the application's running directory.
        /// Returns null if the file does not exist (button will show without an icon).
        /// </summary>
        private static Image LoadEngineIcon()
        {
            var path = Settings.GetRunningDirectory() +
                       Path.Combine("GridFlight", "assets", "engine.png");

            return File.Exists(path) ? Image.FromFile(path) : null;
        }
    }
}
