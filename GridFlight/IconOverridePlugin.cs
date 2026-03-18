using System.Drawing;
using System.IO;
using MissionPlanner;
using MissionPlanner.Plugin;
using MissionPlanner.Utilities;

namespace GridFlight
{
    /// <summary>
    /// Reemplaza los iconos del toolbar de Mission Planner con los de GridFlight.
    ///
    /// Usa el ciclo Loop() (disparo único) para garantizar que los iconos se
    /// aplican DESPUÉS de que todos los plugins (incluido ModernThemePlugin)
    /// hayan terminado su Loaded(). Esto evita que el ThemeManager pise los
    /// iconos personalizados al aplicar su tema.
    ///
    /// Principio Open/Closed: se extiende el comportamiento visual sin modificar
    /// MainV2.cs ni los recursos embebidos originales de ArduPilot.
    /// </summary>
    public class IconOverridePlugin : Plugin
    {
        public override string Name    => "GridFlight - Icon Override";
        public override string Version => "1.0";
        public override string Author  => "GridFlight";

        private bool _applied;

        public override bool Init() => true;

        public override bool Loaded()
        {
            loopratehz = 0.2f; // 1 vez cada 5s, solo necesitamos el primer tick
            return true;       // Habilitar Loop
        }

        public override bool Loop()
        {
            if (_applied) return true;
            _applied = true;

            var icons = new GridFlightMenuIcons();
            MainV2.displayicons = icons;

            MainV2.instance.BeginInvoke((System.Action)(() =>
            {
                var form = MainV2.instance;
                form.MenuFlightData.Image    = icons.fd;
                form.MenuFlightPlanner.Image = icons.fp;
                form.MenuInitConfig.Image    = icons.initsetup;
                form.MenuSimulation.Image    = icons.sim;
                form.MenuConfigTune.Image    = icons.config_tuning;
                form.MenuConnect.Image       = icons.connect;
                form.MenuHelp.Image          = icons.help;

                if (icons.bg != null)
                    form.MainMenu.BackgroundImage = icons.bg;
            }));

            return true;
        }

        public override bool Exit() => true;
    }

    /// <summary>
    /// Implementación de <see cref="MainV2.menuicons"/> que carga los iconos
    /// ámbar de GridFlight desde GridFlight/assets/.
    ///
    /// Fallback: si un archivo no existe en assets, devuelve el recurso embebido
    /// original de Mission Planner.
    /// </summary>
    public class GridFlightMenuIcons : MainV2.menuicons
    {
        private static readonly string _assetsDir =
            Path.Combine(Settings.GetRunningDirectory(), "GridFlight", "assets");

        private static Image Load(string fileName, Image fallback)
        {
            var path = Path.Combine(_assetsDir, fileName);
            return File.Exists(path) ? Image.FromFile(path) : fallback;
        }

        public override Image fd =>
            Load("dark_flightdata_icon.png",
                 global::MissionPlanner.Properties.Resources.dark_flightdata_icon);

        public override Image fp =>
            Load("dark_flightplan_icon.png",
                 global::MissionPlanner.Properties.Resources.dark_flightplan_icon);

        public override Image initsetup =>
            Load("dark_initialsetup_icon.png",
                 global::MissionPlanner.Properties.Resources.dark_initialsetup_icon);

        public override Image config_tuning =>
            Load("dark_tuningconfig_icon.png",
                 global::MissionPlanner.Properties.Resources.dark_tuningconfig_icon);

        public override Image sim =>
            Load("dark_simulation_icon.png",
                 global::MissionPlanner.Properties.Resources.dark_simulation_icon);

        public override Image terminal =>
            Load("dark_terminal_icon.png",
                 global::MissionPlanner.Properties.Resources.dark_terminal_icon);

        public override Image help =>
            Load("dark_help_icon.png",
                 global::MissionPlanner.Properties.Resources.dark_help_icon);

        public override Image donate =>
            Load("dark_donate_icon.png",
                 global::MissionPlanner.Properties.Resources.donate);

        public override Image connect =>
            Load("dark_connect_icon.png",
                 global::MissionPlanner.Properties.Resources.dark_connect_icon);

        public override Image disconnect =>
            Load("dark_disconnect_icon.png",
                 global::MissionPlanner.Properties.Resources.dark_disconnect_icon);

        public override Image bg =>
            Load("dark_icon_background.png", null);

        public override Image wizard =>
            Load("dark_wizard_icon.png",
                 global::MissionPlanner.Properties.Resources.wizardicon);
    }
}
