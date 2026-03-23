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

        private const string PasswordKey     = "GridFlight_MechanicPassword";
        private const string DefaultPassword = "0000";

        public override bool Init() => true;

        public override bool Loaded()
        {
            if (GridFlightProfile.IsFirstLaunch)
                ShowFirstLaunchDialog();

            // Si el perfil activo es Mecánico, pedir contraseña al arrancar
            if (GridFlightProfile.IsMechanic && !GridFlightProfile.IsFirstLaunch)
            {
                if (!RequestMechanicPassword())
                {
                    // Contraseña incorrecta → forzar perfil Piloto y reiniciar
                    GridFlightProfile.Set(GridFlightProfile.Pilot);
                    Application.Restart();
                    return false;
                }
            }

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
                // Pedir contraseña antes de permitir acceso a Mecánico
                if (!RequestMechanicPassword())
                    return;

                GridFlightProfile.Set(GridFlightProfile.Mechanic);
                form.Close();
                // Los plugins Pilot-only ya cargaron (default es Pilot).
                // Necesitamos reiniciar para que Init() los bloquee.
                //PromptRestart();
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

            var itemPilot = new ToolStripMenuItem(
                GridFlightProfile.IsPilot ? "● Perfil Piloto" : "   Perfil Piloto");
            if (GridFlightProfile.IsPilot)
                itemPilot.ForeColor = Color.FromArgb(255, 193, 7);
            itemPilot.Click += (s, e) => SwitchProfile(GridFlightProfile.Pilot);

            var itemMechanic = new ToolStripMenuItem(
                GridFlightProfile.IsMechanic ? "● Perfil Mecánico" : "   Perfil Mecánico");
            if (GridFlightProfile.IsMechanic)
                itemMechanic.ForeColor = Color.FromArgb(255, 193, 7);
            itemMechanic.Click += (s, e) => SwitchProfile(GridFlightProfile.Mechanic);

            profileBtn.DropDownItems.Add(itemPilot);
            profileBtn.DropDownItems.Add(itemMechanic);

            // Opción de cambiar contraseña (solo visible en perfil Mecánico)
            if (GridFlightProfile.IsMechanic)
            {
                profileBtn.DropDownItems.Add(new ToolStripSeparator());
                var itemPassword = new ToolStripMenuItem("Cambiar contraseña...");
                itemPassword.Click += (s, e) => ShowChangePasswordDialog();
                profileBtn.DropDownItems.Add(itemPassword);
            }

            toolStrip.Items.Add(profileBtn);
        }

        // ── Cambio de perfil ────────────────────────────────────────────

        private void SwitchProfile(string newProfile)
        {
            if (newProfile == GridFlightProfile.Current)
                return;

            // Si cambia a Mecánico, pedir contraseña primero
            if (newProfile == GridFlightProfile.Mechanic)
            {
                if (!RequestMechanicPassword())
                    return; // Contraseña incorrecta → no cambiar
            }

            GridFlightProfile.Set(newProfile);
            PromptRestart();
        }

        // ── Contraseña del mecánico ───────────────────────────────────────

        /// <summary>
        /// Obtiene la contraseña actual (del Settings o la default "0000").
        /// </summary>
        private static string GetMechanicPassword()
        {
            return Settings.Instance.GetString(PasswordKey, DefaultPassword);
        }

        /// <summary>
        /// Muestra un diálogo pidiendo la contraseña del mecánico.
        /// Permite hasta 3 intentos. Retorna true si la contraseña es correcta.
        /// </summary>
        private static bool RequestMechanicPassword()
        {
            var storedPassword = GetMechanicPassword();

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                var password = "";
                var result = ShowPasswordDialog(
                    "Acceso Mecánico",
                    attempt > 1
                        ? $"Contraseña incorrecta. Intento {attempt} de 3:"
                        : "Introduce la contraseña del perfil Mecánico:",
                    ref password);

                if (result != DialogResult.OK)
                    return false; // Canceló

                if (password == storedPassword)
                    return true; // Correcta
            }

            MessageBox.Show(
                "Demasiados intentos fallidos.\nAcceso denegado.",
                "GridFlight",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        /// <summary>
        /// Diálogo para cambiar la contraseña del mecánico.
        /// Pide la contraseña actual y la nueva (con confirmación).
        /// </summary>
        private static void ShowChangePasswordDialog()
        {
            var form = new Form
            {
                Text            = "GridFlight - Cambiar Contraseña",
                ClientSize      = new Size(350, 220),
                FormBorderStyle = FormBorderStyle.FixedSingle,
                StartPosition   = FormStartPosition.CenterScreen,
                MinimizeBox     = false,
                MaximizeBox     = false,
                TopMost         = true
            };

            var lblCurrent = new Label
            {
                Text = "Contraseña actual:", Font = new Font("Segoe UI", 9f),
                Location = new Point(15, 18), AutoSize = true
            };
            var txtCurrent = new TextBox
            {
                Location = new Point(175, 15), Size = new Size(155, 25),
                Font = new Font("Segoe UI", 9f), UseSystemPasswordChar = true,
                BackColor = Color.White, ForeColor = Color.Black, Tag = "custom"
            };

            var lblNew = new Label
            {
                Text = "Nueva contraseña:", Font = new Font("Segoe UI", 9f),
                Location = new Point(15, 55), AutoSize = true
            };
            var txtNew = new TextBox
            {
                Location = new Point(175, 52), Size = new Size(155, 25),
                Font = new Font("Segoe UI", 9f), UseSystemPasswordChar = true,
                BackColor = Color.White, ForeColor = Color.Black, Tag = "custom"
            };

            var lblConfirm = new Label
            {
                Text = "Confirmar nueva:", Font = new Font("Segoe UI", 9f),
                Location = new Point(15, 92), AutoSize = true
            };
            var txtConfirm = new TextBox
            {
                Location = new Point(175, 89), Size = new Size(155, 25),
                Font = new Font("Segoe UI", 9f), UseSystemPasswordChar = true,
                BackColor = Color.White, ForeColor = Color.Black, Tag = "custom"
            };

            var lblStatus = new Label
            {
                Text = "", ForeColor = Color.FromArgb(255, 80, 80),
                Font = new Font("Segoe UI", 8.5f),
                Location = new Point(15, 130), Size = new Size(315, 20)
            };

            var btnSave = new Button
            {
                Text = "Guardar", FlatStyle = FlatStyle.Flat,
                Location = new Point(115, 160), Size = new Size(100, 35)
            };
            var btnCancel = new Button
            {
                Text = "Cancelar", FlatStyle = FlatStyle.Flat,
                Location = new Point(225, 160), Size = new Size(100, 35)
            };

            btnCancel.Click += (s, e) => form.Close();
            btnSave.Click += (s, e) =>
            {
                if (txtCurrent.Text != GetMechanicPassword())
                {
                    lblStatus.Text = "Contraseña actual incorrecta.";
                    return;
                }
                if (string.IsNullOrEmpty(txtNew.Text))
                {
                    lblStatus.Text = "La nueva contraseña no puede estar vacía.";
                    return;
                }
                if (txtNew.Text != txtConfirm.Text)
                {
                    lblStatus.Text = "Las contraseñas no coinciden.";
                    return;
                }

                Settings.Instance[PasswordKey] = txtNew.Text;
                Settings.Instance.Save();
                MessageBox.Show("Contraseña actualizada correctamente.",
                    "GridFlight", MessageBoxButtons.OK, MessageBoxIcon.Information);
                form.Close();
            };

            form.Controls.AddRange(new Control[]
            {
                lblCurrent, txtCurrent, lblNew, txtNew,
                lblConfirm, txtConfirm, lblStatus, btnSave, btnCancel
            });

            ThemeManager.ApplyThemeTo(form);
            form.Shown += (ss, ee) => FixTextBoxColors(form);
            form.ShowDialog();
        }

        /// <summary>
        /// Muestra un diálogo simple de contraseña con campo de texto enmascarado.
        /// </summary>
        private static DialogResult ShowPasswordDialog(string title, string prompt, ref string password)
        {
            var form = new Form
            {
                Text            = "GridFlight - " + title,
                ClientSize      = new Size(350, 130),
                FormBorderStyle = FormBorderStyle.FixedSingle,
                StartPosition   = FormStartPosition.CenterScreen,
                MinimizeBox     = false,
                MaximizeBox     = false,
                TopMost         = true
            };

            var lbl = new Label
            {
                Text = prompt, Font = new Font("Segoe UI", 9f),
                Location = new Point(15, 15), Size = new Size(320, 20)
            };
            var txt = new TextBox
            {
                Location = new Point(15, 42), Size = new Size(315, 25),
                Font = new Font("Segoe UI", 10f), UseSystemPasswordChar = true,
                BackColor = Color.White, ForeColor = Color.Black, Tag = "custom"
            };
            var btnOk = new Button
            {
                Text = "Aceptar", FlatStyle = FlatStyle.Flat,
                Location = new Point(130, 80), Size = new Size(90, 33),
                DialogResult = DialogResult.OK
            };
            var btnCancel = new Button
            {
                Text = "Cancelar", FlatStyle = FlatStyle.Flat,
                Location = new Point(230, 80), Size = new Size(90, 33),
                DialogResult = DialogResult.Cancel
            };

            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;
            form.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });

            ThemeManager.ApplyThemeTo(form);
            form.Shown += (ss, ee) => FixTextBoxColors(form);
            var dialogResult = form.ShowDialog();
            password = txt.Text;
            return dialogResult;
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

        // ── Fix TextBox invisibles tras ThemeManager ──────────────────────

        private static void FixTextBoxColors(Control parent)
        {
            foreach (Control ctl in parent.Controls)
            {
                if (ctl is TextBox tb)
                {
                    tb.BackColor = Color.White;
                    tb.ForeColor = Color.Black;
                }
                if (ctl.HasChildren)
                    FixTextBoxColors(ctl);
            }
        }
    }
}
