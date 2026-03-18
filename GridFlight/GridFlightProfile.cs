using System;
using System.IO;
using log4net;
using MissionPlanner.Utilities;

namespace GridFlight
{
    /// <summary>
    /// Helper estático compartido para el sistema de perfiles GridFlight.
    /// Almacena el perfil activo en Settings.Instance (config.xml).
    /// Los cambios de perfil requieren reinicio de la aplicación porque
    /// Init() es el único punto de control en el ciclo de vida de plugins.
    /// </summary>
    public static class GridFlightProfile
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(GridFlightProfile));

        public const string SettingsKey = "GridFlight_Profile";
        public const string Pilot = "Pilot";
        public const string Mechanic = "Mechanic";

        /// <summary>
        /// Perfil activo. Por defecto "Pilot" si no se ha configurado o si
        /// ocurre cualquier error al leer Settings.
        /// </summary>
        public static string Current
        {
            get
            {
                try
                {
                    return Settings.Instance.GetString(SettingsKey, Pilot);
                }
                catch (Exception ex)
                {
                    log.Error("GridFlightProfile: failed to read profile, defaulting to Pilot", ex);
                    return Pilot;
                }
            }
        }

        /// <summary>
        /// True solo si el perfil es explícitamente "Mechanic".
        /// Cualquier otro valor (incluyendo errores) se trata como Pilot.
        /// </summary>
        public static bool IsMechanic => Current == Mechanic;
        public static bool IsPilot => !IsMechanic;

        /// <summary>
        /// True en el primer arranque (no existe la key en config.xml).
        /// </summary>
        public static bool IsFirstLaunch =>
            !Settings.Instance.ContainsKey(SettingsKey);

        /// <summary>
        /// Persiste el nuevo perfil en config.xml.
        /// </summary>
        public static void Set(string profile)
        {
            Settings.Instance[SettingsKey] = profile;
            Settings.Instance.Save();
        }

        /// <summary>
        /// Directorio donde se guardan las configuraciones favoritas (.param).
        /// Se crea automáticamente si no existe.
        /// </summary>
        public static string ConfigsDirectory
        {
            get
            {
                var dir = Path.Combine(
                    Settings.GetRunningDirectory(),
                    "GridFlight", "configs");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return dir;
            }
        }
    }
}
