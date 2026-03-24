using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MissionPlanner;
using ArduPilotCommon = MissionPlanner.ArduPilot.Common;
using MissionPlanner.Controls;
using MissionPlanner.Plugin;
using MissionPlanner.Utilities;

namespace GridFlight
{
    /// <summary>
    /// Controla la selección de modos de vuelo en el tab Actions de FlightData:
    ///
    ///   - Reordena el grid del tab Actions para el layout GridFlight.
    ///   - Filtra el ComboBox CMB_modes para mostrar solo los modos permitidos.
    ///   - Para mecánicos: añade un segundo ComboBox (lista completa).
    ///
    /// Activo para TODOS los perfiles.
    /// </summary>
    public class FlightModePlugin : MissionPlanner.Plugin.Plugin
    {
        public override string Name    => "GridFlight - Flight Mode Controls";
        public override string Version => "1.1";
        public override string Author  => "GridFlight";

        // =====================================================================
        //  MODOS OCULTOS — Editar aquí para cambiar qué modos ve el piloto.
        //
        //  Cualquier modo en esta lista NO aparecerá en el desplegable principal
        //  de CMB_modes. Los nombres deben coincidir exactamente con los valores
        //  devueltos por ArduPilot.Common.getModesList() (case-insensitive).
        //
        //  Ejemplo: para ocultar también "Guided", añadir "Guided" a la lista.
        // =====================================================================
        private static readonly string[] HiddenModes =
        {
            "Acro",
            "FBWA",
            "FBWB",
            "AVOID_ADSB",
            "QAcro",
            "Thermal",
            "Loiter To QLand",
            "AUTOLAND",
            "INITIALISING",
        };

        private ComboBox _cmbFullList;

        public override bool Init() => true; // Activo para todos los perfiles

        public override bool Loaded()
        {
            var fd = Host.MainForm.FlightData;
            if (fd == null) return false;

            var panels = fd.Controls.Find("tableLayoutPanel1", true);
            if (panels.Length == 0 || !(panels[0] is TableLayoutPanel panel)) return false;

            var modesArr = fd.Controls.Find("CMB_modes", true);
            if (modesArr.Length == 0 || !(modesArr[0] is ComboBox cmbModes)) return false;

            // --- 1. Reordenar grid (upstream -> GridFlight layout) ---
            RearrangeGrid(panel);

            // --- 2. Filtro de modos en CMB_modes (todos los perfiles) ---
            cmbModes.Click += CmbModes_FilterClick;

            return false;
        }

        public override bool Exit() => true;

        /// <summary>
        /// Reordena los controles del tableLayoutPanel1 desde las posiciones
        /// upstream a las posiciones GridFlight usando Remove + Add atómico
        /// dentro de SuspendLayout para evitar superposiciones.
        /// </summary>
        private static void RearrangeGrid(TableLayoutPanel panel)
        {
            // Posiciones deseadas (GridFlight layout)
            var moves = new (string name, int col, int row)[]
            {
                ("BUT_SendMSG",          4, 2),
                ("BUT_abortland",        4, 1),
                ("modifyandSetLoiterRad", 4, 4),
                ("BUT_clear_track",      4, 0),
                ("BUT_resumemis",        4, 3),
                ("modifyandSetAlt",      3, 4),
                ("modifyandSetSpeed",    2, 4),
            };

            panel.SuspendLayout();

            // Paso 1: Quitar todos los controles a mover (libera sus celdas)
            var batch = new (Control ctrl, int col, int row)[moves.Length];
            for (int i = 0; i < moves.Length; i++)
            {
                var found = panel.Controls.Find(moves[i].name, false);
                if (found.Length > 0)
                {
                    batch[i] = (found[0], moves[i].col, moves[i].row);
                    panel.Controls.Remove(found[0]);
                }
            }

            // Paso 2: Re-añadir en posiciones destino (celdas ya vacías)
            foreach (var (ctrl, col, row) in batch)
            {
                if (ctrl != null)
                    panel.Controls.Add(ctrl, col, row);
            }

            panel.ResumeLayout(true);
        }

        /// <summary>
        /// Handler aditivo sobre CMB_modes.Click. Se ejecuta DESPUÉS del handler
        /// original (que carga todos los modos) y re-filtra el DataSource para
        /// excluir los modos definidos en <see cref="HiddenModes"/>.
        /// </summary>
        private void CmbModes_FilterClick(object sender, EventArgs e)
        {
            var cmb = (ComboBox)sender;
            string current = cmb.Text;

            cmb.DataSource = GridFlightProfile.IsPilot ?
                ArduPilotCommon.getModesList(MainV2.comPort.MAV.cs.firmware)
                .Where(kvp => !HiddenModes.Contains(kvp.Value, StringComparer.OrdinalIgnoreCase))
                .ToList() :
                ArduPilotCommon.getModesList(MainV2.comPort.MAV.cs.firmware);
            cmb.ValueMember   = "Key";
            cmb.DisplayMember = "Value";
            cmb.Text          = current;
        }
    }
}
