using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using MissionPlanner;
using MissionPlanner.Plugin;
using MissionPlanner.Utilities;

namespace GridFlight
{
    /// <summary>
    /// Aplica la identidad visual de GridFlight al arranque de Mission Planner.
    ///
    /// Responsabilidades:
    ///   1. Si logo2.png existe en el directorio de ejecución, reemplaza el logo
    ///      de ArduPilot en la barra de menú y lo escala proporcionalmente a la
    ///      altura real del botón para evitar recortes.
    ///   2. Redirige el clic del logo a gridflight.tech en vez de ardupilot.org.
    ///   3. Si Gridflight-Icon.png existe, lo aplica como ícono de la ventana
    ///      (barra de título y barra de tareas).
    ///
    /// Por qué aquí y no en MainV2.cs:
    ///   MenuArduPilot es un campo public (MainV2.Designer.cs:265), por lo que
    ///   es accesible desde el plugin sin tocar código fuente de ArduPilot.
    ///   Loaded() corre en el hilo UI después de que MainV2 está completamente
    ///   inicializado, garantizando que Height ya tiene su valor final.
    /// </summary>
    public class BrandingPlugin : MissionPlanner.Plugin.Plugin
    {
        public override string Name    => "GridFlight - Branding";
        public override string Version => "1.0";
        public override string Author  => "GridFlight";

        // Mantiene el ícono en memoria para toda la vida de la aplicación.
        // Icon.FromHandle no toma ownership del HICON; sin esta referencia estática
        // el GC podría colectar el objeto y dejar el handle colgado.
        private static Icon _appIcon;

        public override bool Init() => true;

        public override bool Loaded()
        {
            
            ApplyLogo();
            RedirectLogoUrl();
            ApplyWindowIcon();
            return false; // Sin loop periódico
        }

        public override bool Exit() => true;

        /// <summary>
        /// Escala Program.Logo2 (logo2.png) proporcionalmente a la altura de
        /// MenuArduPilot y lo asigna como imagen del botón.
        /// </summary>
        private void ApplyLogo()
        {
            if (Program.Logo2 == null) return;


            var btn         = Host.MainForm.MenuArduPilot;
            int targetHeight = btn.Height > 0 ? btn.Height : 31;
            int targetWidth  = (int)Math.Round(Program.Logo2.Width * (targetHeight / (double)Program.Logo2.Height));

            btn.Image  = new Bitmap(Program.Logo2, targetWidth, targetHeight);
            btn.Width  = targetWidth;
            // Margen izquierdo (18 px) separa el logo de los botones de navegación;
            // margen derecho (24 px) da respiro visual antes de los controles de conexión.
            btn.Margin = new Padding(18, 0, 24, 0);
        }

        /// <summary>
        /// Carga Gridflight-Icon.ico y lo aplica como ícono de la ventana principal
        /// (barra de título y barra de tareas de Windows).
        ///
        /// Se carga el .ico directamente con new Icon(path) en lugar del patrón
        /// GetHicon() → FromHandle(). Ese patrón era frágil: el HICON quedaba inválido
        /// al salir del bloque using porque GDI liberaba el bitmap subyacente antes
        /// de que Windows consumiera el handle. Con new Icon(path) el runtime .NET
        /// gestiona el ciclo de vida del ícono correctamente.
        /// La referencia estática _appIcon evita que el GC descarte el objeto mientras
        /// la aplicación está en ejecución.
        /// </summary>
        private void ApplyWindowIcon()
        {
            var path = Settings.GetRunningDirectory() +
                       Path.Combine("GridFlight", "assets", "Gridflight-Icon.ico");

            if (!File.Exists(path)) return;

            _appIcon = new Icon(path);
            Host.MainForm.Icon = _appIcon;
        }

        /// <summary>
        /// Elimina el handler ardupilot.org registrado en Designer.cs y adjunta
        /// uno nuevo que abre gridflight.tech.
        ///
        /// ToolStripItem almacena sus event handlers en el EventHandlerList interno
        /// de System.ComponentModel.Component, indexado por la clave estática privada
        /// "EventClick". Esta estructura lleva estable desde .NET 2.0.
        ///
        /// Si la reflexión falla en alguna versión futura del framework, el catch
        /// asegura que el comportamiento degradado sea abrir ambas URLs (ardupilot.org
        /// y gridflight.tech) en lugar de lanzar una excepción no controlada.
        /// </summary>
        private void RedirectLogoUrl()
        {
            var btn = Host.MainForm.MenuArduPilot;

            try
            {
                var clickKey = typeof(ToolStripItem)
                    .GetField("EventClick", BindingFlags.NonPublic | BindingFlags.Static)
                    ?.GetValue(null);

                var eventsField = typeof(Component)
                    .GetField("events", BindingFlags.NonPublic | BindingFlags.Instance);

                if (eventsField?.GetValue(btn) is EventHandlerList list && clickKey != null)
                {
                    var existing = list[clickKey];
                    if (existing != null)
                        list.RemoveHandler(clickKey, existing);
                }
            }
            catch (Exception)
            {
                // Reflexión falló — handler original (ardupilot.org) permanece activo.
                // Ambas URLs se abrirán. No es crítico; continuamos sin interrumpir.
            }

            btn.Click += (_, __) =>
            {
                try { System.Diagnostics.Process.Start("https://www.gridflight.tech/"); }
                catch { }
            };
        }
    }
}
