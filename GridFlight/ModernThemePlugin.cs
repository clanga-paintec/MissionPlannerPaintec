using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;
using log4net;
using MissionPlanner;
using MissionPlanner.Controls;
using MissionPlanner.Plugin;
using MissionPlanner.Utilities;

namespace GridFlight
{
    /// <summary>
    /// Plugin de tema visual moderno para GridFlight.
    ///
    /// Aplica un "lavado de cara" estético a toda la interfaz de Mission Planner
    /// sin modificar funcionalidad, botones ni eventos existentes. Todo el código
    /// vive en este único archivo; basta con eliminarlo para revertir al aspecto
    /// original.
    ///
    /// Estrategia de theming en dos fases:
    ///   1. Sobreescribe los campos estáticos de <see cref="ThemeManager"/> para que
    ///      cualquier control tematizado en el futuro (diálogos, vistas dinámicas)
    ///      herede los nuevos colores automáticamente.
    ///   2. Recorre recursivamente todos los controles del formulario principal para
    ///      aplicar estilos extras que ThemeManager no cubre: FlatStyle, renderers
    ///      de ToolStrip, bordes modernos y tipografía Segoe UI.
    ///
    /// Adicionalmente carga la fuente Material Symbols Rounded en una
    /// <see cref="PrivateFontCollection"/> para uso futuro de iconos vectoriales.
    /// </summary>
    public class ModernThemePlugin : MissionPlanner.Plugin.Plugin
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ModernThemePlugin));

        public override string Name => "GridFlight - Modern Amber Theme";
        public override string Version => "1.1";
        public override string Author => "GridFlight";

        #region Palette

        /// <summary>
        /// Paleta de colores del tema moderno GridFlight (Edición Ámbar).
        /// Tema oscuro profesional con acento dorado/ámbar.
        /// </summary>
        private static class Palette
        {
            // ── Fondos ─────────────────────────────────────────────────────
            public static readonly Color Background = Color.FromArgb(24, 24, 24);   // #181818
            public static readonly Color Surface = Color.FromArgb(33, 33, 33);      // #212121
            public static readonly Color SurfaceElevated = Color.FromArgb(45, 45, 45); // #2D2D2D
            public static readonly Color SurfaceInput = Color.FromArgb(122, 122, 122);  // #7A7A7A

            // ── Acentos ────────────────────────────────────────────────────
            public static readonly Color Primary = Color.FromArgb(255, 193, 7);  // #FFC107 Amber
            public static readonly Color PrimaryDark = Color.FromArgb(211, 158, 0); // #D39E00 Amber Oscuro
            public static readonly Color Accent = Color.FromArgb(255, 213, 79); // #FFD54F Amber Claro

            // ── Texto ──────────────────────────────────────────────────────
            public static readonly Color TextPrimary = Color.FromArgb(245, 245, 245); // #F5F5F5
            public static readonly Color TextSecondary = Color.FromArgb(170, 170, 170); // #AAAAAA
            public static readonly Color TextOnPrimary = Color.FromArgb(33, 33, 33); // #212121 Texto oscuro sobre fondo amarillo (mejor accesibilidad)

            // ── Botones ────────────────────────────────────────────────────
            public static readonly Color ButtonDisabled = Color.FromArgb(55, 55, 55);   // #373737

            // ── Bordes ─────────────────────────────────────────────────────
            public static readonly Color Border = Color.FromArgb(60, 60, 60);   // #3C3C3C
        }

        #endregion

        #region Material Icons

        /// <summary>
        /// Mapeo de nombres legibles a códigos Unicode de Material Symbols Rounded.
        /// Uso: <c>new Font(ModernThemePlugin.IconFontFamily, 18f)</c> +
        /// <c>MaterialIcons.Home</c> como texto del control.
        /// Referencia completa de codepoints:
        /// https://fonts.google.com/icons?icon.set=Material+Symbols
        /// </summary>
        public static class MaterialIcons
        {
            // ── Navegación ─────────────────────────────────────────────────
            public static readonly string Home = "\uE88A";
            public static readonly string Menu = "\uE5D2";
            public static readonly string ArrowBack = "\uE5C4";
            public static readonly string ArrowForward = "\uE5C8";
            public static readonly string Close = "\uE5CD";
            public static readonly string ExpandMore = "\uE5CF";
            public static readonly string ExpandLess = "\uE5CE";
            public static readonly string ChevronRight = "\uE5CC";

            // ── Vuelo / Dron ───────────────────────────────────────────────
            public static readonly string FlightTakeoff = "\uE914";
            public static readonly string FlightLand = "\uE904";
            public static readonly string Flight = "\uE539";
            public static readonly string Map = "\uE55B";
            public static readonly string MyLocation = "\uE55C";
            public static readonly string GpsFixed = "\uE1B3";
            public static readonly string Explore = "\uE87A";
            public static readonly string Terrain = "\uE564";
            public static readonly string Speed = "\uE9E4";
            public static readonly string Height = "\uEA16";

            // ── Configuración y herramientas ───────────────────────────────
            public static readonly string Settings = "\uE8B8";
            public static readonly string Build = "\uF8A1";
            public static readonly string Tune = "\uE429";
            public static readonly string Upload = "\uE2C6";
            public static readonly string Download = "\uE2C4";
            public static readonly string Sync = "\uE627";
            public static readonly string Refresh = "\uE5D5";
            public static readonly string Save = "\uE161";
            public static readonly string Delete = "\uE872";
            public static readonly string Edit = "\uE3C9";

            // ── Estado e información ───────────────────────────────────────
            public static readonly string Info = "\uE88E";
            public static readonly string Warning = "\uE002";
            public static readonly string Error = "\uE000";
            public static readonly string CheckCircle = "\uE86C";
            public static readonly string Battery = "\uE1A4";
            public static readonly string Signal = "\uE1D8";
            public static readonly string Wifi = "\uE63E";

            // ── Misceláneos ────────────────────────────────────────────────
            public static readonly string Fullscreen = "\uE5D0";
            public static readonly string ZoomIn = "\uE8FF";
            public static readonly string ZoomOut = "\uE900";
            public static readonly string Layers = "\uE53B";
            public static readonly string Timeline = "\uE922";
            public static readonly string Dashboard = "\uE871";
            public static readonly string Photo = "\uE410";
            public static readonly string Videocam = "\uE04B";
        }

        #endregion

        #region Font Fields

        private static PrivateFontCollection _fontCollection;

        /// <summary>
        /// Familia tipográfica de Material Symbols Rounded cargada en memoria.
        /// Null si la fuente no pudo cargarse. Verificar antes de usar.
        /// </summary>
        public static FontFamily IconFontFamily { get; private set; }

        private static Font _modernFont;
        private static Font _modernFontBold;

        /// <summary>
        /// Renderer compartido para todos los ToolStrip del formulario.
        /// Se reutiliza una sola instancia para evitar allocations innecesarias.
        /// </summary>
        private static ToolStripProfessionalRenderer _toolStripRenderer;

        #endregion

      #region Plugin Lifecycle

        public override bool Init() => true;

        public override bool Loaded()
        {
            try
            {
                LoadIconFont();
                InitializeModernFonts();
                _toolStripRenderer = new ModernToolStripRenderer();
                
                // Desactivar AutoScale para evitar redimensionado no deseado
                Host.MainForm.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None; 
                OverrideThemeManagerColors();

                // Fase 1: re-aplicar ThemeManager con los nuevos colores.
                ThemeManager.ApplyThemeTo(Host.MainForm);

                // Fase 2: recorrido recursivo propio para estilos extras
                ApplyModernExtras(Host.MainForm);

                // Fase 3: tipografía moderna segura
                if (_modernFont != null)
                {
                    Host.MainForm.SuspendLayout();
                    
                    // ELIMINADO: Host.MainForm.Font = _modernFont;
                    // NUEVO: Aplicamos la fuente solo a los elementos que muestran texto, 
                    // ignorando los contenedores (Forms, Panels, UserControls) que rompen el layout.
                    ApplyFontToLeaves(Host.MainForm, _modernFont);

                    Host.MainForm.ResumeLayout(false);
                }

                log.Info("ModernThemePlugin: Tema Ámbar aplicado (Edición GridFlight 1.1).");
            }
            catch (Exception ex)
            {
                log.Error("ModernThemePlugin: Error al aplicar el tema moderno.", ex);
            }

            return false;
        }

        public override bool Exit()
        {
            _modernFont?.Dispose();
            _modernFontBold?.Dispose();
            _fontCollection?.Dispose();

            return true;
        }

        /// <summary>
        /// Aplica la fuente SOLO a controles visuales finales, evitando cambiar 
        /// la fuente de los contenedores para que WinForms no destruya el layout de SITL.
        /// </summary>
        private void ApplyFontToLeaves(Control root, Font font)
        {
            if (root == null || root.IsDisposed) return;

            // Aplicar fuente solo si es un elemento visual de texto (evitamos Form, Panel, UserControl, TabControl)
            if (root is Label || root is Button || root is CheckBox || 
                root is RadioButton || root is ToolStrip || root is DataGridView || 
                root is TextBox || root is ComboBox)
            {
                root.Font = font;
            }

            // Continuar la recursión por todos los hijos
            foreach (Control child in root.Controls)
            {
                ApplyFontToLeaves(child, font);
            }
        }

        #endregion

        #region Theme Manager Color Override

        /// <summary>
        /// Sobreescribe los campos estáticos de <see cref="ThemeManager"/> con la
        /// paleta moderna. Esto garantiza que cualquier diálogo o vista nueva que
        /// Mission Planner tematice en el futuro usará nuestros colores.
        ///
        /// Los colores del HUD se dejan intactos deliberadamente: cumplen una
        /// función de seguridad durante el vuelo (distinguir cielo de tierra)
        /// y no deben alterarse por razones estéticas.
        /// </summary>
        private void OverrideThemeManagerColors()
        {
            // Fondos
            ThemeManager.BGColor = Palette.Background;
            ThemeManager.ControlBGColor = Palette.Surface;
            ThemeManager.BGColorTextBox = Palette.SurfaceInput;

            // Texto
            ThemeManager.TextColor = Palette.TextPrimary;
            ThemeManager.RTBForeColor = Palette.TextPrimary;
            ThemeManager.UnselectedTextColour = Palette.TextSecondary;

            // Botones (MyButton usa gradiente top/bot)
            ThemeManager.ButBG = Palette.Primary;
            ThemeManager.ButBGBot = Palette.PrimaryDark;
            ThemeManager.ButBorder = Palette.Border;
            ThemeManager.ButtonTextColor = Palette.TextOnPrimary;
            ThemeManager.ButtonTextColorNotEnabled = Palette.TextSecondary;
            ThemeManager.ColorNotEnabled = Palette.ButtonDisabled;
            ThemeManager.ColorMouseOver = Palette.Accent;
            ThemeManager.ColorMouseDown = Palette.PrimaryDark;

            // Banners (ej. encabezados de sección en Config/Setup)
            ThemeManager.BannerColor1 = Palette.Background;
            ThemeManager.BannerColor2 = Palette.Primary;

            // BackstageView (panel lateral de Setup/Config)
            ThemeManager.BSVButtonAreaBGColor = Palette.Background;

            // Barras de progreso
            ThemeManager.ProgressBarColorTop = Palette.Primary;
            ThemeManager.ProgressBarColorBot = Palette.PrimaryDark;
            ThemeManager.ProgressBarOutlineColor = Palette.Border;
            ThemeManager.HorizontalPBValueColor = Palette.Primary;

            // Indicador PPM en pestaña Flight Modes
            ThemeManager.CurrentPPMBackground = Palette.Primary;

            // ZedGraph (gráficos de telemetría, logs, etc.)
            ThemeManager.ZedGraphChartFill = Palette.Surface;
            ThemeManager.ZedGraphPaneFill = Palette.Background;
            ThemeManager.ZedGraphLegendFill = Palette.SurfaceElevated;
        }

        #endregion

        #region Font Loading

        /// <summary>
        /// Carga Material Symbols Rounded desde el archivo TTF desplegado en el
        /// directorio de ejecución usando <see cref="PrivateFontCollection"/>.
        /// La fuente queda disponible en memoria sin necesidad de instalarla
        /// en el sistema operativo.
        /// </summary>
        private void LoadIconFont()
        {
            var fontPath = Path.Combine(
                Settings.GetRunningDirectory(),
                "GridFlight", "assets",
                "MaterialSymbolsRounded-VariableFont_FILL,GRAD,opsz,wght.ttf");

            if (!File.Exists(fontPath))
            {
                log.Warn($"ModernThemePlugin: Fuente de iconos no encontrada en {fontPath}");
                return;
            }

            try
            {
                _fontCollection = new PrivateFontCollection();
                _fontCollection.AddFontFile(fontPath);

                if (_fontCollection.Families.Length > 0)
                {
                    IconFontFamily = _fontCollection.Families[0];
                    log.Info($"ModernThemePlugin: Fuente '{IconFontFamily.Name}' cargada.");
                }
            }
            catch (Exception ex)
            {
                log.Error("ModernThemePlugin: Error al cargar fuente de iconos.", ex);
            }
        }

        /// <summary>
        /// Inicializa las fuentes Segoe UI para la interfaz.
        /// Fallback a la fuente genérica sans-serif si Segoe UI no está disponible.
        /// </summary>
        private void InitializeModernFonts()
        {
            const string preferredFont = "Segoe UI";
            const float fontSize = 9f;

            try
            {
                _modernFont = new Font(preferredFont, fontSize, FontStyle.Regular);
                _modernFontBold = new Font(preferredFont, fontSize, FontStyle.Bold);
            }
            catch
            {
                _modernFont = new Font(FontFamily.GenericSansSerif, fontSize, FontStyle.Regular);
                _modernFontBold = new Font(FontFamily.GenericSansSerif, fontSize, FontStyle.Bold);
            }
        }

        #endregion

        #region Recursive Modern Extras

        /// <summary>
        /// Recorre recursivamente todos los controles a partir de <paramref name="root"/>
        /// y aplica estilos modernos que <see cref="ThemeManager"/> no cubre:
        /// FlatStyle en botones, renderers de ToolStrip, bordes limpios, etc.
        /// Respeta <see cref="PreventThemingAttribute"/> de Mission Planner.
        /// </summary>
        private void ApplyModernExtras(Control root)
        {
            if (root == null || root.IsDisposed) return;
            if (root.GetType().IsDefined(typeof(PreventThemingAttribute), false)) return;

            try
            {
                StyleControlExtras(root);
            }
            catch (Exception ex)
            {
                log.Debug($"ModernThemePlugin: No se pudo estilizar {root.GetType().Name}: {ex.Message}");
            }

            // Aplicar renderer al ContextMenuStrip asociado si existe.
            if (root.ContextMenuStrip != null)
                root.ContextMenuStrip.Renderer = _toolStripRenderer;

            foreach (Control child in root.Controls)
            {
                ApplyModernExtras(child);
            }
        }

        /// <summary>
        /// Aplica estilos extras a un control individual según su tipo.
        /// Solo modifica propiedades visuales: FlatStyle, bordes, renderers.
        /// Nunca toca Text, Enabled, Visible, Click ni funcionalidad alguna.
        /// </summary>
        /// <param name="ctl">Control a estilizar.</param>
        private void StyleControlExtras(Control ctl)
        {
            // Tag "custom" = Mission Planner espera que este control conserve sus colores.
            if (ctl.Tag is string tag && tag == "custom") return;

            // El orden importa: MyButton hereda de Button, así que va primero.
            switch (ctl)
            {
                // ── MyButton (control custom con gradiente propio) ──────────
                // Ya fue estilizado por ThemeManager; no se necesita FlatStyle.
                case MyButton _:
                    break;

                // ── Button estándar de WinForms ────────────────────────────
                case Button btn:
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 0;
                    btn.FlatAppearance.MouseOverBackColor = Palette.PrimaryDark;
                    btn.FlatAppearance.MouseDownBackColor = Palette.PrimaryDark;
                    btn.BackColor = Palette.Primary;
                    btn.ForeColor = Palette.TextOnPrimary;
                    break;

                // ── CheckBox ───────────────────────────────────────────────
                case CheckBox chk:
                    chk.FlatStyle = FlatStyle.Flat;
                    chk.FlatAppearance.BorderColor = Palette.Primary;
                    chk.FlatAppearance.CheckedBackColor = Palette.Primary;
                    chk.FlatAppearance.MouseOverBackColor = Palette.SurfaceElevated;
                    break;

                // ── RadioButton ────────────────────────────────────────────
                case RadioButton rb:
                    rb.FlatStyle = FlatStyle.Flat;
                    rb.FlatAppearance.BorderColor = Palette.Primary;
                    rb.FlatAppearance.CheckedBackColor = Palette.Primary;
                    rb.FlatAppearance.MouseOverBackColor = Palette.SurfaceElevated;
                    break;

                // ── ComboBox ───────────────────────────────────────────────
                case ComboBox cmb:
                    cmb.FlatStyle = FlatStyle.Flat;
                    break;

                // ── Panel ──────────────────────────────────────────────────
                case Panel panel:
                    if (panel.BorderStyle == BorderStyle.Fixed3D)
                        panel.BorderStyle = BorderStyle.FixedSingle;
                    break;

                // ── GroupBox ───────────────────────────────────────────────
                case GroupBox grp:
                    grp.FlatStyle = FlatStyle.Flat;
                    break;

                // ── DataGridView ───────────────────────────────────────────
                case DataGridView dgv:
                    StyleDataGridView(dgv);
                    break;

                // ── ToolStrip (barras de herramientas) ─────────────────────
                case MenuStrip ms:
                    ms.Renderer = _toolStripRenderer;
                    break;

                case StatusStrip ss:
                    ss.Renderer = _toolStripRenderer;
                    break;

                case ToolStrip ts:
                    ts.Renderer = _toolStripRenderer;
                    break;
            }
        }

        /// <summary>
        /// Estilo moderno para DataGridView: cabeceras oscuras, selección teal,
        /// bordes horizontales sutiles y sin bordes exteriores.
        /// </summary>
        private void StyleDataGridView(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = Palette.Border;

            dgv.ColumnHeadersDefaultCellStyle.BackColor = Palette.Background;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Palette.TextPrimary;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Palette.Background;

            dgv.RowHeadersDefaultCellStyle.BackColor = Palette.Background;
            dgv.RowHeadersDefaultCellStyle.ForeColor = Palette.TextPrimary;

            dgv.DefaultCellStyle.BackColor = Palette.SurfaceElevated;
            dgv.DefaultCellStyle.ForeColor = Palette.TextPrimary;
            dgv.DefaultCellStyle.SelectionBackColor = Palette.Primary;
            dgv.DefaultCellStyle.SelectionForeColor = Palette.TextOnPrimary;

            dgv.AlternatingRowsDefaultCellStyle.BackColor = Palette.Surface;
            dgv.AlternatingRowsDefaultCellStyle.ForeColor = Palette.TextPrimary;
        }

        #endregion

        #region Custom ToolStrip Renderer

        /// <summary>
        /// Renderer personalizado para ToolStrip, MenuStrip y StatusStrip.
        /// Reemplaza los gradientes de Windows XP/Vista con fondos planos
        /// y colores consistentes con el tema moderno.
        /// </summary>
        private class ModernToolStripRenderer : ToolStripProfessionalRenderer
        {
            public ModernToolStripRenderer()
                : base(new ModernColorTable()) { }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using (var brush = new SolidBrush(Palette.Background))
                {
                    e.Graphics.FillRectangle(brush, e.AffectedBounds);
                }
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                var bounds = new Rectangle(Point.Empty, e.Item.Size);
                var color = e.Item.Selected ? Palette.SurfaceElevated : Palette.Background;

                using (var brush = new SolidBrush(color))
                {
                    e.Graphics.FillRectangle(brush, bounds);
                }
            }

            protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
            {
                var bounds = new Rectangle(Point.Empty, e.Item.Size);
                var color = e.Item.Selected || e.Item.Pressed
                    ? Palette.SurfaceElevated
                    : Palette.Background;

                using (var brush = new SolidBrush(color))
                {
                    e.Graphics.FillRectangle(brush, bounds);
                }
            }

            protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
            {
                var bounds = new Rectangle(Point.Empty, e.Item.Size);
                var color = e.Item.Selected || e.Item.Pressed
                    ? Palette.SurfaceElevated
                    : Palette.Background;

                using (var brush = new SolidBrush(color))
                {
                    e.Graphics.FillRectangle(brush, bounds);
                }
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = Palette.TextPrimary;
                base.OnRenderItemText(e);
            }

            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                int y = e.Item.Height / 2;
                using (var pen = new Pen(Palette.Border))
                {
                    e.Graphics.DrawLine(pen, 0, y, e.Item.Width, y);
                }
            }

            /// <summary>
            /// Suprime el borde del ToolStrip para un look limpio sin líneas exteriores.
            /// </summary>
            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                // Intencionalmente vacío: sin borde exterior.
            }
        }

        /// <summary>
        /// Tabla de colores que alimenta al <see cref="ModernToolStripRenderer"/>.
        /// Cada propiedad sobreescrita elimina los gradientes nativos de Windows
        /// y los reemplaza con colores planos del tema.
        /// </summary>
        private class ModernColorTable : ProfessionalColorTable
        {
            // ── Fondo de ToolStrip ─────────────────────────────────────────
            public override Color ToolStripGradientBegin => Palette.Background;
            public override Color ToolStripGradientMiddle => Palette.Background;
            public override Color ToolStripGradientEnd => Palette.Background;
            public override Color ToolStripBorder => Palette.Border;
            public override Color ToolStripDropDownBackground => Palette.Surface;

            // ── Fondo de MenuStrip ─────────────────────────────────────────
            public override Color MenuStripGradientBegin => Palette.Background;
            public override Color MenuStripGradientEnd => Palette.Background;
            public override Color MenuBorder => Palette.Border;

            // ── Ítems de menú ──────────────────────────────────────────────
            public override Color MenuItemSelected => Palette.SurfaceElevated;
            public override Color MenuItemBorder => Palette.Border;
            public override Color MenuItemSelectedGradientBegin => Palette.SurfaceElevated;
            public override Color MenuItemSelectedGradientEnd => Palette.SurfaceElevated;
            public override Color MenuItemPressedGradientBegin => Palette.PrimaryDark;
            public override Color MenuItemPressedGradientEnd => Palette.PrimaryDark;

            // ── Margen de imágenes ─────────────────────────────────────────
            public override Color ImageMarginGradientBegin => Palette.Background;
            public override Color ImageMarginGradientMiddle => Palette.Background;
            public override Color ImageMarginGradientEnd => Palette.Background;

            // ── Separadores ────────────────────────────────────────────────
            public override Color SeparatorDark => Palette.Border;
            public override Color SeparatorLight => Palette.Surface;

            // ── Botones de ToolStrip ───────────────────────────────────────
            public override Color ButtonSelectedBorder => Palette.Primary;
            public override Color ButtonSelectedHighlight => Palette.SurfaceElevated;
            public override Color ButtonSelectedGradientBegin => Palette.SurfaceElevated;
            public override Color ButtonSelectedGradientEnd => Palette.SurfaceElevated;
            public override Color ButtonPressedHighlight => Palette.PrimaryDark;
            public override Color ButtonPressedGradientBegin => Palette.PrimaryDark;
            public override Color ButtonPressedGradientEnd => Palette.PrimaryDark;
            public override Color ButtonCheckedGradientBegin => Palette.Primary;
            public override Color ButtonCheckedGradientEnd => Palette.Primary;
            public override Color ButtonCheckedHighlight => Palette.Primary;

            // ── Overflow ───────────────────────────────────────────────────
            public override Color OverflowButtonGradientBegin => Palette.Background;
            public override Color OverflowButtonGradientMiddle => Palette.Background;
            public override Color OverflowButtonGradientEnd => Palette.Background;

            // ── StatusStrip ────────────────────────────────────────────────
            public override Color StatusStripGradientBegin => Palette.Background;
            public override Color StatusStripGradientEnd => Palette.Background;
        }

        #endregion
    }
}
