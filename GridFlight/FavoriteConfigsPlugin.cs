using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common.CommandTrees.ExpressionBuilder;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BruTile.Wmts.Generated;
using MissionPlanner;
using MissionPlanner.Controls;
using MissionPlanner.GridFlight;
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

        
        // -- Configuraciones predefinidas --

        private static ArrayList defaultConfigs = new ArrayList();

        private static List<string> VTOL = new List<string>
        {
            "3",
            "5",
            "airspeed",
            "groundspeed",
            "alt",
            "DistToHome",
            "battery_voltage",
            "battery_usedmah",
            "verticalspeed",
            "distTraveled",
            "current",
            "altasl",
            "ter_alt",
            "timeInAirMinSec",
            "wind_vel",
            "gimballng",
            "gimballat",
            "tot",
            "toh"

        };

        private static List<string> DRON = new List<string>
        {
            "3",
            "5",
            "ter_curalt",
            "alt",
            "DistToHome",
            "battery_voltage",
            "current",
            "verticalspeed",
            "distTraveled",
            "battery_usedmah",
            "altasl",
            "ter_alt",
            "timeInAirMinSec",
            "wind_vel",
            "wind_dir"
        };
        
        public override bool Init() => true;
        
        public override bool Loaded()
        {
            defaultConfigs.Add(VTOL);
            defaultConfigs.Add(DRON);
            AddToolbarButton();
            return false; // Sin loop periódico
        }

        public override bool Exit() => true;

        // -- Cambiar Quick Tab menu --------------------------------------

        private void ApplyQuickTabPreset(List<string> fields)
        {
            Settings.Instance["quickViewColms"] = fields.ElementAt(0);
            Settings.Instance["quickViewRows"] = fields.ElementAt(1);

            for(int i = 2; i < fields.Count; i++)
            {
                Settings.Instance["quickView" + (i - 2)] = fields.ElementAt(i);
            }

            var fd = MainV2.instance?.FlightData;
            if (fd == null) return;

            fd.Invoke((Action)(() =>
            {
                fd.Activate();
            }));
        }

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
            

            // Insertar después del logo
            int logoIndex = toolStrip.Items.IndexOf(Host.MainForm.MenuArduPilot);
            if (logoIndex >= 0)
                toolStrip.Items.Insert(logoIndex + 1, btn);
            else
                toolStrip.Items.Add(btn);
            
            btn.Click += Btn_Click;
        }

        private void Btn_Click(object sender, EventArgs e)
        {
            ShowConfigManagerDialog();
        }

        // -- Agregar una configuración -----------------------------------

        private bool SaveCustomConfig(string name)
        {
            List<string> newParams = new List<string>();
            string colms = Settings.Instance["quickViewColms"];
            string rows = Settings.Instance["quickViewRows"];
            newParams.Add(colms);
            newParams.Add(rows);
            int qvCounter = 0;

            for(int i = 2; i < Settings.Instance.Count; i++)
            {
                if (Settings.Instance["quickView" +  i] != null)
                {
                    newParams.Add(Settings.Instance["quickView" + qvCounter]);
                    qvCounter++;
                }
            }

            QuickConfig newConfig = new QuickConfig(name, newParams);
            if (QuickConfig.SaveQuickConfig(newConfig))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // ── Diálogo gestor de configuraciones ───────────────────────────
        
        private void ShowConfigManagerDialog()
        {
            int formSizeX = 500;
            int formSizeY = 430;
            Size btnSize = new Size(90, 25);

            var form = new Form
            {
                Text            = "Configuraciones Favoritas",
                ClientSize      = new Size(formSizeX, formSizeY),
                FormBorderStyle = FormBorderStyle.FixedSingle,
                StartPosition   = FormStartPosition.CenterParent,
                MinimizeBox     = false,
                MaximizeBox     = false
            };

            // ── Título ──
            var lblTitle = new Label
            {
                Text     = "Configuraciones Prederteminadas",
                Font     = new Font("Segoe UI", 11f, FontStyle.Bold),
                Location = new Point(15, 12),
                AutoSize = true
            };

            // -- Configuración VTOL --

            var btnVTOL = new PictureBox
            {
                Image = File.Exists(Path.Combine(Settings.GetRunningDirectory(), "GridFlight", "assets", "01_01.png")) ?
                        new Bitmap(Path.Combine(Settings.GetRunningDirectory(), "GridFlight", "assets", "01_01.png")) :
                        new Bitmap(Path.Combine(Settings.GetRunningDirectory(), "Resources", "01_01.png")),
                Anchor = AnchorStyles.Top,
                Location = new Point((formSizeX/defaultConfigs.Count)/2 - 25 , formSizeY/4),
                Size = new Size(80,80),
                SizeMode = PictureBoxSizeMode.Zoom

            };

            var nameVTOL = new Label
            {
                Text = "VTOL",
                Anchor = AnchorStyles.Bottom,
                Location = new Point(btnVTOL.Location.X + btnVTOL.Width/4, btnVTOL.Location.Y + btnVTOL.Height + 5),
            };

            btnVTOL.Click += (s, e) =>
            {
                ApplyQuickTabPreset(VTOL);
                form.Close();
            };

            // -- Configuración DRON --

            var btnDRON = new PictureBox
            {
                Image = File.Exists(Path.Combine(Settings.GetRunningDirectory(), "GridFlight", "assets", "01_05.png")) ?
                        new Bitmap(Path.Combine(Settings.GetRunningDirectory(), "GridFlight", "assets", "01_05.png")) :
                        new Bitmap(Path.Combine(Settings.GetRunningDirectory(), "Resources", "01_05.png")),
                Anchor = AnchorStyles.Top,
                Location = new Point(formSizeX / defaultConfigs.Count + (formSizeX / defaultConfigs.Count) / 2 - 25, formSizeY / 4),
                Size = new Size(80, 80),
                SizeMode = PictureBoxSizeMode.Zoom
            };

            btnDRON.Click += (s, e) =>
            {
                ApplyQuickTabPreset(DRON);
                form.Close();
            };

            var nameDRON = new Label
            {
                Text = "DRON",
                Anchor = AnchorStyles.Bottom,
                Location = new Point(btnDRON.Location.X + btnDRON.Width/4, btnDRON.Location.Y + btnDRON.Height + 5),
            };

            //Lista de configuraciones
            var listConfig = new ListBox
            {
                Location = new Point(25, 270),
                Size = new Size(450,100),
                DataSource = QuickConfig.AllQuickConfigs(),
                HorizontalScrollbar = true
            };
            // -- Botón de guardado --
            var saveBtn = new Button
            {
                Text = "Guardar config",
                Location = new Point(185,380),
                Size = btnSize,
                FlatStyle = FlatStyle.Flat
            };

            saveBtn.Click += (s, e) =>
            {
                AskForName();
                form.Close();
            };

            // -- Botón de Carga --
            var loadBtn = new Button
            {
                Text = "Cargar config",
                Location = new Point(285, 380),
                Size = btnSize,
                FlatStyle = FlatStyle.Flat
            };

            loadBtn.Click += (s, e) =>
            {
                if (listConfig.SelectedItem is QuickConfig selected)
                {
                    ApplyQuickTabPreset(selected.getParams());
                    form.Close();
                }
            };

            // -- Botón de Borrado --
            var eraseBtn = new Button
            {
                Text = "Borrar config",
                Location = new Point(385,380),
                Size = btnSize,
                FlatStyle = FlatStyle.Flat
            };

            eraseBtn.Click += (s, e) =>
            {
                if (listConfig.SelectedItem is QuickConfig selected)
                {
                    ConfigErase(selected.getName());
                    form.Close();
                }
            };

            form.Controls.Add(lblTitle);
            form.Controls.Add(btnVTOL);
            form.Controls.Add(nameVTOL);
            form.Controls.Add(btnDRON);
            form.Controls.Add(nameDRON);
            form.Controls.Add(listConfig);
            form.Controls.Add(saveBtn);
            form.Controls.Add(loadBtn);
            form.Controls.Add(eraseBtn);
            ThemeManager.ApplyThemeTo(form);
            form.ShowDialog();
        }
        
        // -- Introducir nombre al guardar config --
        private void AskForName()
        {
            var form = new Form
            {
                Text = "Introducir Nombre",
                ClientSize = new Size(300, 200),
                FormBorderStyle = FormBorderStyle.FixedSingle,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false
            };

            var lblText = new Label
            {
                Text = "Introduce el nombre de la configuración",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Location = new Point(10, 35),
                AutoSize = true
            };

            var textBox = new TextBox
            {
                Location = new Point(75,110),
                Size = new Size(150,20),
                AutoSize = true
            };

            var btnAccept = new Button
            {
                Text = "Aceptar",
                Location = new Point(95,160),
                Size = new Size(90, 25),
                FlatStyle = FlatStyle.Flat
            };

            btnAccept.Click += (s, e) =>
            {
                if(SaveCustomConfig(textBox.Text)){
                    form.Close();
                }

                else
                {
                    ConfigExist();
                }

            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(195,160),
                Size = new Size(90, 25),
                FlatStyle = FlatStyle.Flat
            };

            btnCancel.Click += (s, e) =>
            {
                form.Close();
            };

            form.Controls.Add(lblText);
            form.Controls.Add(textBox);
            form.Controls.Add(btnAccept);
            form.Controls.Add(btnCancel);
            ThemeManager.ApplyThemeTo(form);
            form.ShowDialog();
        }

        // -- Pestaña de error si ya existe --
        private void ConfigExist()
        {
            var form = new Form
            {
                ClientSize = new Size(100, 80),
                FormBorderStyle = FormBorderStyle.FixedSingle,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false
            };

            var errorText = new Label
            {
                Text = "Ese nombre ya existe",
                Location = new Point(20,40)
            };

            form.Controls.Add(errorText);
            ThemeManager.ApplyThemeTo(form);
            form.ShowDialog();
        }

        // -- Pestaña de confirmación de borrado --
        private void ConfigErase(string name)
        {
            var form = new Form
            {
                ClientSize = new Size(200, 80),
                FormBorderStyle = FormBorderStyle.FixedSingle,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false
            };

            var questionLbl = new Label
            {
                Text = "¿Seguro que quieres borrarlo?",
                Location = new Point(25,30),
                AutoSize = true
            };


            var btnYes = new Button
            {
                Text = "Si",
                Location = new Point(20, 60),
                Size = new Size(30, 20),
                FlatStyle = FlatStyle.Flat
            };

            btnYes.Click += (s, e) =>
            {
                QuickConfig.EraseQuickConfig(name);
                form.Close();
            };

            var btnNo = new Button
            {
                Text = "No",
                Location = new Point(50, 60),
                Size = new Size(30, 20),
                FlatStyle = FlatStyle.Flat
            };


            btnNo.Click += (s, e) =>
            {
                form.Close();
            };

            form.Controls.Add(questionLbl);
            form.Controls.Add(btnYes);
            form.Controls.Add(btnNo);
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
