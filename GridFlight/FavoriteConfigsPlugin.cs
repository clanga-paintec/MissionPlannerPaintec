using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MissionPlanner;
using MissionPlanner.Controls;
using MissionPlanner.Plugin;
using MissionPlanner.Utilities;
using SkiaSharp;

namespace GridFlight
{
    /// <summary>
    /// Plugin de "Configuraciones Favoritas" para el perfil Piloto.
    ///
    /// Añade un botón en la barra de herramientas que abre un gestor modal
    /// para guardar, cargar, eliminar e importar configuraciones de parámetros
    /// de drones en formato .param.
    ///
    /// Almacena los archivos en GridFlight/configs/ bajo el directorio de
    /// ejecución de MissionPlanner.
    ///
    /// Solo se carga en el perfil Pilot (Init() verifica el perfil).
    /// </summary>
    public class FavoriteConfigsPlugin : MissionPlanner.Plugin.Plugin
    {
        public override string Name    => "GridFlight - Favorite Configurations";
        public override string Version => "1.0";
        public override string Author  => "GridFlight";

        public override bool Init() => true;

        public override bool Loaded()
        {
            AddToolbarButton();
            return false; // Sin loop periódico
        }

        public override bool Exit() => true;

        // ── Botón en toolbar ────────────────────────────────────────────

        private void AddToolbarButton()
        {
            var mainMenu = Host.MainForm.Controls.Find("MainMenu", true);
            if (mainMenu.Length == 0 || !(mainMenu[0] is ToolStrip toolStrip))
                return;

            var btn = new ToolStripButton
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText  = "Configuraciones Favoritas",
                Margin       = new Padding(4, 1, 0, 2),
                Image        = RenderStarIcon(24)
            };
            btn.Click += (s, e) => ShowConfigManagerDialog();

            // Insertar después del logo
            int logoIndex = toolStrip.Items.IndexOf(Host.MainForm.MenuArduPilot);
            if (logoIndex >= 0)
                toolStrip.Items.Insert(logoIndex + 1, btn);
            else
                toolStrip.Items.Add(btn);
        }

        // ── Diálogo gestor de configuraciones ───────────────────────────

        private void ShowConfigManagerDialog()
        {
            var configsDir = GridFlightProfile.ConfigsDirectory;

            var form = new Form
            {
                Text            = "Configuraciones Favoritas",
                ClientSize      = new Size(500, 430),
                FormBorderStyle = FormBorderStyle.FixedSingle,
                StartPosition   = FormStartPosition.CenterScreen,
                MinimizeBox     = false,
                MaximizeBox     = false
            };

            // ── Título ──
            var lblTitle = new Label
            {
                Text     = "Configuraciones Guardadas",
                Font     = new Font("Segoe UI", 11f, FontStyle.Bold),
                Location = new Point(15, 12),
                AutoSize = true
            };

            // ── Lista ──
            var listBox = new ListBox
            {
                Location = new Point(15, 42),
                Size     = new Size(355, 330),
                Font     = new Font("Segoe UI", 9.5f)
            };

            // ── Botones (columna derecha) ──
            int btnX = 385, btnW = 100, btnH = 36;

            var btnSave = new Button
            {
                Text      = "Guardar",
                Location  = new Point(btnX, 42),
                Size      = new Size(btnW, btnH),
                FlatStyle = FlatStyle.Flat
            };

            var btnLoad = new Button
            {
                Text      = "Cargar",
                Location  = new Point(btnX, 86),
                Size      = new Size(btnW, btnH),
                FlatStyle = FlatStyle.Flat
            };

            var btnDelete = new Button
            {
                Text      = "Eliminar",
                Location  = new Point(btnX, 130),
                Size      = new Size(btnW, btnH),
                FlatStyle = FlatStyle.Flat
            };

            var btnImport = new Button
            {
                Text      = "Importar...",
                Location  = new Point(btnX, 194),
                Size      = new Size(btnW, btnH),
                FlatStyle = FlatStyle.Flat
            };

            var lblStatus = new Label
            {
                Text     = "",
                Location = new Point(15, 385),
                Size     = new Size(470, 35),
                Font     = new Font("Segoe UI", 8.5f)
            };

            // ── Poblar lista ──
            Action refreshList = () =>
            {
                listBox.Items.Clear();
                if (Directory.Exists(configsDir))
                {
                    foreach (var f in Directory.GetFiles(configsDir, "*.param").OrderBy(f => f))
                        listBox.Items.Add(Path.GetFileNameWithoutExtension(f));
                }
            };
            refreshList();

            // ── Guardar configuración actual ──
            btnSave.Click += (s, e) =>
            {
                if (!MainV2.comPort.BaseStream.IsOpen)
                {
                    lblStatus.Text = "No conectado. Conecta un vehículo primero.";
                    return;
                }

                var configName = "";
                if (InputBox.Show("Guardar Configuración",
                        "Nombre para esta configuración:",
                        ref configName) != DialogResult.OK
                    || string.IsNullOrWhiteSpace(configName))
                    return;

                // Sanitizar nombre de archivo
                foreach (var c in Path.GetInvalidFileNameChars())
                    configName = configName.Replace(c, '_');

                var filePath = Path.Combine(configsDir, configName + ".param");

                if (File.Exists(filePath))
                {
                    var overwrite = MessageBox.Show(
                        "La configuración '" + configName + "' ya existe.\n¿Sobrescribir?",
                        "Confirmar",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    if (overwrite != DialogResult.Yes) return;
                }

                // Convertir MAVLinkParamList a Hashtable
                Dictionary<string, double> paramDict = MainV2.comPort.MAV.param;
                var hashtable = new Hashtable();
                foreach (var kv in paramDict)
                    hashtable[kv.Key] = kv.Value;

                ParamFile.SaveParamFile(filePath, hashtable);
                lblStatus.Text = "Guardado: " + configName;
                refreshList();
            };

            // ── Cargar configuración ──
            btnLoad.Click += (s, e) =>
            {
                if (listBox.SelectedItem == null)
                {
                    lblStatus.Text = "Selecciona una configuración primero.";
                    return;
                }
                if (!MainV2.comPort.BaseStream.IsOpen)
                {
                    lblStatus.Text = "No conectado. Conecta un vehículo primero.";
                    return;
                }

                var name = listBox.SelectedItem.ToString();
                var filePath = Path.Combine(configsDir, name + ".param");

                var fileParams = ParamFile.loadParamFile(filePath);
                Dictionary<string, double> currentParams = MainV2.comPort.MAV.param;

                // ParamCompare con dgv=null aplica directamente vía setParam()
                var compareForm = new ParamCompare(null, currentParams, fileParams);
                ThemeManager.ApplyThemeTo(compareForm);
                compareForm.ShowDialog();

                if (compareForm.DialogResult == DialogResult.OK)
                    lblStatus.Text = "Aplicado: " + name;
            };

            // ── Eliminar configuración ──
            btnDelete.Click += (s, e) =>
            {
                if (listBox.SelectedItem == null)
                {
                    lblStatus.Text = "Selecciona una configuración primero.";
                    return;
                }

                var name = listBox.SelectedItem.ToString();
                var confirm = MessageBox.Show(
                    "¿Eliminar la configuración '" + name + "'?",
                    "Confirmar eliminación",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm == DialogResult.Yes)
                {
                    File.Delete(Path.Combine(configsDir, name + ".param"));
                    lblStatus.Text = "Eliminado: " + name;
                    refreshList();
                }
            };

            // ── Importar desde archivo externo ──
            btnImport.Click += (s, e) =>
            {
                using (var ofd = new OpenFileDialog
                {
                    Filter           = ParamFile.FileMask,
                    RestoreDirectory = true
                })
                {
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        var destName = Path.GetFileNameWithoutExtension(ofd.FileName);
                        var dest = Path.Combine(configsDir, destName + ".param");
                        File.Copy(ofd.FileName, dest, true);
                        lblStatus.Text = "Importado: " + destName;
                        refreshList();
                    }
                }
            };

            form.Controls.AddRange(new Control[]
            {
                lblTitle, listBox, btnSave, btnLoad,
                btnDelete, btnImport, lblStatus
            });

            ThemeManager.ApplyThemeTo(form);
            form.ShowDialog();
        }

        // ── Icono estrella renderizado con SkiaSharp ────────────────────

        private static Image RenderStarIcon(int size)
        {
            try
            {
                using (var surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Premul)))
                {
                    var canvas = surface.Canvas;
                    canvas.Clear(SKColors.Transparent);

                    // Estrella de 5 puntas
                    var path = new SKPath();
                    float cx = size / 2f, cy = size / 2f;
                    float outer = size / 2f - 1f;
                    float inner = outer * 0.38f;

                    for (int i = 0; i < 5; i++)
                    {
                        double outerAngle = Math.PI / 2 + i * 2 * Math.PI / 5;
                        double innerAngle = outerAngle + Math.PI / 5;

                        float ox = cx + outer * (float)Math.Cos(outerAngle);
                        float oy = cy - outer * (float)Math.Sin(outerAngle);
                        float ix = cx + inner * (float)Math.Cos(innerAngle);
                        float iy = cy - inner * (float)Math.Sin(innerAngle);

                        if (i == 0)
                            path.MoveTo(ox, oy);
                        else
                            path.LineTo(ox, oy);

                        path.LineTo(ix, iy);
                    }
                    path.Close();

                    using (var paint = new SKPaint
                    {
                        Color       = new SKColor(255, 193, 7), // Ámbar
                        IsAntialias = true,
                        Style       = SKPaintStyle.Fill
                    })
                    {
                        canvas.DrawPath(path, paint);
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
