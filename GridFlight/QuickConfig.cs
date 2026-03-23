using Accord.Math;
using DotSpatial.Data.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MissionPlanner.Utilities;
using Settings = MissionPlanner.Utilities.Settings;
using System.Collections;
using IronPython.Compiler.Ast;

namespace MissionPlanner.GridFlight
{
    internal class QuickConfig
    {
        private List<string> paramsShown;
        private string name;
        
        public QuickConfig(string name, List<string> paramsShown)
        {
            this.name = name;
            this.paramsShown = paramsShown;
        }

        public string getName()
        {
            return name;
        }

        public override string ToString() => name;
            

        public void setName(string name)
        {
            this.name = name.Trim();
        }

        public List<string> getParams()
        {
            return paramsShown;
        }

        public void setParams(List<string> newParams)
        {
            paramsShown = newParams;
        }

        public static bool SaveQuickConfig(QuickConfig qc)
        {
            string key = "quickconfig_" + System.Net.WebUtility.UrlEncode(qc.getName()).Replace("+", "_");
            if (Settings.Instance[key + "_name"] != null)
            {
                return false;
            }
            Settings.Instance[key + "_name"] = qc.getName();
            Settings.Instance.SetList(key + "_params", qc.getParams());
            Settings.Instance.AppendList("quickconfig_names", qc.getName());
            return true;
        }

        public static QuickConfig LoadQuickConfig(string name)
        {
            string key = "quickconfig_" + System.Net.WebUtility.UrlEncode(name).Replace("+", "_");
            List<string> parms = Settings.Instance.GetList(key + "_params").ToList();
            return new QuickConfig(name, parms);
        }

        public static List<QuickConfig> AllQuickConfigs()
        {
            IEnumerable<string> names = Settings.Instance.GetList("quickconfig_names");
            return names.Select(n => LoadQuickConfig(n)).ToList();
        }

        public static void EraseQuickConfig(string name)
        {
            string key = "quickconfig_" + System.Net.WebUtility.UrlEncode(name).Replace("+", "_");
            Settings.Instance.Remove(key + "_name");
            Settings.Instance.Remove(key + "_params");
            Settings.Instance.RemoveList("quickconfig_names", name);
        }


    }
}
