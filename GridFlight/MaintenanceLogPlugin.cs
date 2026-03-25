using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using log4net;
using MissionPlanner;
using MissionPlanner.Plugin;
using MissionPlanner.Utilities;
using Newtonsoft.Json;
using SkiaSharp;

namespace GridFlight
{
    /// <summary>
    /// Plugin de registro de mantenimiento para GridFlight.
    ///
    /// Permite registrar, visualizar y eliminar entradas de mantenimiento
    /// realizadas sobre el dron. Almacena el historial en un archivo JSON
    /// local dentro de GridFlight/configs/.
    ///
    /// Solo se carga en el perfil Mecánico (Init() verifica el perfil).
    /// El piloto no necesita registrar mantenimientos.
    /// </summary>
    public class MaintenanceLogPlugin : MissionPlanner.Plugin.Plugin
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MaintenanceLogPlugin));

        public override string Name    => "GridFlight - Maintenance Log";
        public override string Version => "1.0";
        public override string Author  => "GridFlight";

        private static string LogFilePath => Path.Combine(
            GridFlightProfile.ConfigsDirectory, "maintenanceLog.json");

        public override bool Init() => GridFlightProfile.IsMechanic;

        public override bool Loaded()
        {
            AddToolbarButton();
            return false; // Sin loop periódico
        }

        public override bool Exit() => true;

        // ── Modelo de datos ───────────────────────────────────────────────

        private class MaintenanceEntry
        {
            public string Date        { get; set; }
            public string Technician  { get; set; }
            public string Description { get; set; }
        }

        // ── Persistencia JSON ─────────────────────────────────────────────

        private static List<MaintenanceEntry> LoadEntries()
        {
            try
            {
                if (!File.Exists(LogFilePath))
                    return new List<MaintenanceEntry>();

                var json = File.ReadAllText(LogFilePath);
                return JsonConvert.DeserializeObject<List<MaintenanceEntry>>(json)
                       ?? new List<MaintenanceEntry>();
            }
            catch (Exception ex)
            {
                log.Error("MaintenanceLog: failed to load entries", ex);
                return new List<MaintenanceEntry>();
            }
        }

        private static void SaveEntries(List<MaintenanceEntry> entries)
        {
            try
            {
                var json = JsonConvert.SerializeObject(entries, Formatting.Indented);
                File.WriteAllText(LogFilePath, json);
            }
            catch (Exception ex)
            {
                log.Error("MaintenanceLog: failed to save entries", ex);
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
                ToolTipText  = "Registro de Mantenimiento",
                Text = "MAINTENANCE",
                TextImageRelation = TextImageRelation.ImageAboveText,
                Font = new Font("Segoe UI", 8f),
                Margin       = new Padding(4, 1, 0, 2),
                Image        = RenderWrenchIcon(24)
            };
            btn.Click += (s, e) => ShowMaintenanceDialog();

            int logoIndex = toolStrip.Items.IndexOf(Host.MainForm.MenuArduPilot);
            if (logoIndex >= 0)
                toolStrip.Items.Insert(logoIndex + 1, btn);
            else
                toolStrip.Items.Add(btn);
        }

        // ── Diálogo principal ─────────────────────────────────────────────

        private void ShowMaintenanceDialog()
        {
            var entries = LoadEntries();

            var form = new Form
            {
                Text            = "Registro de Mantenimiento",
                ClientSize      = new Size(600, 480),
                FormBorderStyle = FormBorderStyle.FixedSingle,
                StartPosition   = FormStartPosition.CenterScreen,
                MinimizeBox     = false,
                MaximizeBox     = false
            };

            // ── Título ──
            var lblTitle = new Label
            {
                Text     = "Historial de Mantenimiento",
                Font     = new Font("Segoe UI", 11f, FontStyle.Bold),
                Location = new Point(15, 12),
                AutoSize = true
            };

            // ── ListView ──
            var listView = new ListView
            {
                Location      = new Point(15, 42),
                Size          = new Size(570, 280),
                View          = View.Details,
                FullRowSelect = true,
                GridLines     = true,
                Font          = new Font("Segoe UI", 9f)
            };
            listView.Columns.Add("Fecha", 100);
            listView.Columns.Add("Técnico", 120);
            listView.Columns.Add("Descripción", 330);

            Action refreshList = () =>
            {
                listView.Items.Clear();
                foreach (var entry in entries)
                {
                    var item = new ListViewItem(entry.Date);
                    item.SubItems.Add(entry.Technician);
                    item.SubItems.Add(entry.Description);
                    listView.Items.Add(item);
                }
            };
            refreshList();

            // ── Campos de entrada ──
            int inputY = 335;

            var lblDate = new Label
            {
                Text     = "Fecha:",
                Font     = new Font("Segoe UI", 9f),
                Location = new Point(15, inputY + 3),
                AutoSize = true
            };
            var txtDate = new TextBox
            {
                Text      = DateTime.Now.ToString("yyyy-MM-dd"),
                Location  = new Point(70, inputY),
                Size      = new Size(90, 25),
                Font      = new Font("Segoe UI", 9f),
                BackColor = Color.White,
                ForeColor = Color.Black,
                Tag       = "custom"
            };

            var lblTech = new Label
            {
                Text     = "Técnico:",
                Font     = new Font("Segoe UI", 9f),
                Location = new Point(175, inputY + 3),
                AutoSize = true
            };
            var txtTech = new TextBox
            {
                Location  = new Point(240, inputY),
                Size      = new Size(120, 25),
                Font      = new Font("Segoe UI", 9f),
                BackColor = Color.White,
                ForeColor = Color.Black,
                Tag       = "custom"
            };

            var lblDesc = new Label
            {
                Text     = "Descripción:",
                Font     = new Font("Segoe UI", 9f),
                Location = new Point(15, inputY + 38),
                AutoSize = true
            };
            var txtDesc = new TextBox
            {
                Location  = new Point(105, inputY + 35),
                Size      = new Size(380, 25),
                Font      = new Font("Segoe UI", 9f),
                BackColor = Color.White,
                ForeColor = Color.Black,
                Tag       = "custom"
            };

            // ── Botones ──
            int btnY = inputY + 75;

            var btnAdd = new Button
            {
                Text      = "Añadir",
                Location  = new Point(15, btnY),
                Size      = new Size(100, 35),
                FlatStyle = FlatStyle.Flat
            };

            var btnDelete = new Button
            {
                Text      = "Eliminar",
                Location  = new Point(125, btnY),
                Size      = new Size(100, 35),
                FlatStyle = FlatStyle.Flat
            };

            var lblStatus = new Label
            {
                Text     = "",
                Location = new Point(240, btnY + 8),
                Size     = new Size(345, 25),
                Font     = new Font("Segoe UI", 8.5f)
            };

            // ── Eventos ──
            btnAdd.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtDesc.Text))
                {
                    lblStatus.Text = "La descripción no puede estar vacía.";
                    return;
                }

                entries.Insert(0, new MaintenanceEntry
                {
                    Date        = txtDate.Text.Trim(),
                    Technician  = txtTech.Text.Trim(),
                    Description = txtDesc.Text.Trim()
                });
                SaveEntries(entries);
                txtDesc.Text   = "";
                lblStatus.Text = "Entrada añadida.";
                refreshList();
            };

            btnDelete.Click += (s, e) =>
            {
                if (listView.SelectedIndices.Count == 0)
                {
                    lblStatus.Text = "Selecciona una entrada primero.";
                    return;
                }

                int idx = listView.SelectedIndices[0];
                var confirm = MessageBox.Show(
                    "¿Eliminar esta entrada de mantenimiento?",
                    "Confirmar",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm == DialogResult.Yes)
                {
                    entries.RemoveAt(idx);
                    SaveEntries(entries);
                    lblStatus.Text = "Entrada eliminada.";
                    refreshList();
                }
            };

            form.Controls.AddRange(new Control[]
            {
                lblTitle, listView,
                lblDate, txtDate, lblTech, txtTech, lblDesc, txtDesc,
                btnAdd, btnDelete, lblStatus
            });

            ThemeManager.ApplyThemeTo(form);
            form.Shown += (ss, ee) => FixTextBoxColors(form);
            form.ShowDialog();
        }

        // ── Fix para TextBox invisibles tras ThemeManager ─────────────────

        /// <summary>
        /// ThemeManager.ApplyThemeTo() sobreescribe incondicionalmente el
        /// BackColor de todos los TextBox (no respeta Tag="custom").
        /// Este método re-aplica colores visibles después del tema.
        /// </summary>
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

        // ── Icono llave renderizado con SkiaSharp ─────────────────────────

        private static Image RenderWrenchIcon(int size)
        {
            try
            {
                using (var surface = SKSurface.Create(
                    new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Premul)))
                {
                    var canvas = surface.Canvas;
                    canvas.Clear(SKColors.Transparent);

                    float s = size;

                    using (var paint = new SKPaint
                    {
                        Color       = new SKColor(255, 193, 7),
                        IsAntialias = true,
                        Style       = SKPaintStyle.Stroke,
                        StrokeWidth = s * 0.09f,
                        StrokeCap   = SKStrokeCap.Round,
                        StrokeJoin  = SKStrokeJoin.Round
                    })
                    {
                        // Mango de la llave (diagonal)
                        canvas.DrawLine(s * 0.2f, s * 0.8f, s * 0.55f, s * 0.45f, paint);

                        // Cabeza de la llave (arco abierto)
                        var headRect = new SKRect(s * 0.35f, s * 0.08f, s * 0.92f, s * 0.65f);
                        using (var path = new SKPath())
                        {
                            path.AddArc(headRect, 200, 250);
                            canvas.DrawPath(path, paint);
                        }
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
