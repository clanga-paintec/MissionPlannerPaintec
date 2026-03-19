using System;
using System.Drawing;
using System.Windows.Forms;
using MissionPlanner;
using MissionPlanner.Plugin;
using MissionPlanner.Utilities;

namespace GridFlight
{
    /// <summary>
    /// Plugin de selección de perfil GridFlight.
    ///
    /// - En el primer arranque muestra un diálogo modal para elegir perfil.
    /// - Añade un ToolStripDropDownButton al toolbar para cambiar de perfil.
    /// - Los cambios de perfil requieren reinicio (Init() es el único punto
    ///   de control para habilitar/deshabilitar plugins).
    ///
    /// Este plugin carga en AMBOS perfiles (Init() siempre retorna true).
    /// </summary>
    public class ProfileSelectorPlugin : MissionPlanner.Plugin.Plugin
    {
        public override string Name    => "GridFlight - Profile Selector";
        public override string Version => "1.0";
        public override string Author  => "GridFlight";

        public override bool Init() => true;

        public override bool Loaded()
        {
            if (GridFlightProfile.IsFirstLaunch)
                ShowFirstLaunchDialog();

            AddProfileToolbarButton();
            return false; // Sin loop periódico
        }

        public override bool Exit() => true;

        // ── Diálogo de primer arranque ──────────────────────────────────

        private void ShowFirstLaunchDialog()
        {
            var form = new Form
            {
                Text            = "GridFlight - Seleccionar Perfil",
                ClientSize      = new Size(400, 310),
                FormBorderStyle = FormBorderStyle.FixedSingle,
                StartPosition   = FormStartPosition.CenterScreen,
                MinimizeBox     = false,
                MaximizeBox     = false,
                TopMost         = true
            };

            var lblTitle = new Label
            {
                Text      = "Bienvenido a GridFlight",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 193, 7),
                Location  = new Point(20, 15),
                AutoSize  = true
            };

            var lblDesc = new Label
            {
                Text     = "Selecciona tu perfil. Puedes cambiarlo después\n" +
                           "desde la barra de herramientas (requiere reinicio).",
                Font     = new Font("Segoe UI", 9f),
                Location = new Point(20, 50),
                Size     = new Size(360, 40)
            };

            var btnPilot = new Button
            {
                Text      = "PILOTO",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                Size      = new Size(360, 50),
                Location  = new Point(20, 100),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 193, 7),
                ForeColor = Color.FromArgb(33, 33, 33)
            };

            var lblPilotDesc = new Label
            {
                Text     = "Experiencia GridFlight completa: tema ámbar, atajos,\n" +
                           "configuraciones favoritas, menús simplificados.",
                Font     = new Font("Segoe UI", 8f),
                Location = new Point(20, 155),
                Size     = new Size(360, 30)
            };

            var btnMechanic = new Button
            {
                Text      = "MECANICO",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                Size      = new Size(360, 50),
                Location  = new Point(20, 195),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(55, 55, 55),
                ForeColor = Color.FromArgb(245, 245, 245)
            };

            var lblMechanicDesc = new Label
            {
                Text     = "MissionPlanner completo con branding GridFlight:\n" +
                           "tema ámbar, test de motores, gestión de parámetros.",
                Font     = new Font("Segoe UI", 8f),
                Location = new Point(20, 250),
                Size     = new Size(360, 30)
            };

            btnPilot.Click += (s, e) =>
            {
                GridFlightProfile.Set(GridFlightProfile.Pilot);
                form.Close();
            };

            btnMechanic.Click += (s, e) =>
            {
                GridFlightProfile.Set(GridFlightProfile.Mechanic);
                form.Close();
                // Los plugins Pilot-only ya cargaron (default es Pilot).
                // Necesitamos reiniciar para que Init() los bloquee.
                PromptRestart();
            };

            form.Controls.AddRange(new Control[]
                { lblTitle, lblDesc, btnPilot, lblPilotDesc, btnMechanic, lblMechanicDesc });

            ThemeManager.ApplyThemeTo(form);
            form.ShowDialog();
        }

        // ── Botón de perfil en toolbar ──────────────────────────────────

        private void AddProfileToolbarButton()
        {
            var mainMenu = Host.MainForm.Controls.Find("MainMenu", true);
            if (mainMenu.Length == 0 || !(mainMenu[0] is ToolStrip toolStrip))
                return;

            var profileBtn = new ToolStripDropDownButton
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Text         = GridFlightProfile.IsPilot ? "PILOTO" : "MECANICO",
                ToolTipText  = "Perfil GridFlight: " + GridFlightProfile.Current,
                Font         = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor    = Color.FromArgb(255, 193, 7),
                Margin       = new Padding(4, 1, 0, 2)
            };

            var itemPilot = new ToolStripMenuItem("Perfil Piloto")
            {
                Checked = GridFlightProfile.IsPilot
            };
            itemPilot.Click += (s, e) => SwitchProfile(GridFlightProfile.Pilot);

            var itemMechanic = new ToolStripMenuItem("Perfil Mecánico")
            {
                Checked = GridFlightProfile.IsMechanic
            };
            itemMechanic.Click += (s, e) => SwitchProfile(GridFlightProfile.Mechanic);

            profileBtn.DropDownItems.Add(itemPilot);
            profileBtn.DropDownItems.Add(itemMechanic);

            toolStrip.Items.Add(profileBtn);
        }

        // ── Cambio de perfil ────────────────────────────────────────────

        private void SwitchProfile(string newProfile)
        {
            if (newProfile == GridFlightProfile.Current)
                return;

            GridFlightProfile.Set(newProfile);
            PromptRestart();
        }

        private static void PromptRestart()
        {
            var result = MessageBox.Show(
                "Perfil cambiado a " + GridFlightProfile.Current + ".\n\n" +
                "GridFlight necesita reiniciarse para aplicar el cambio.\n" +
                "¿Reiniciar ahora?",
                "GridFlight - Cambio de Perfil",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
                Application.Restart();
        }
    }
}
