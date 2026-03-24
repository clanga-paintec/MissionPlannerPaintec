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
using fastJSON;

namespace MissionPlanner.GridFlight
{
    internal class QuickConfig
    {
        private List<string> paramsShown;
        private string name;
        private List<string> displayView;
        
        public QuickConfig(string name, List<string> paramsShown, List<string> displayView)
        {
            this.name = name;
            this.paramsShown = paramsShown;
            this.displayView = displayView;
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

        public List<string> getDisplayView()
        {
            return displayView;
        }

        public void setDisplayView(List<string> displayView)
        {
            this.displayView = displayView;
        }
        public static bool SaveQuickConfig(QuickConfig qc)
        {
            string key = "quickconfig_" + System.Net.WebUtility.UrlEncode(qc.getName()).Replace("+", "_");
            List<string> tabs = qc.getDisplayView();
            if (Settings.Instance[key + "_name"] != null)
            {
                return false;
            }
            Settings.Instance[key + "_name"] = qc.getName();
            Settings.Instance.SetList(key + "_params", qc.getParams());
            Settings.Instance.SetList(key + "_tabs", tabs);
            Settings.Instance.AppendList("quickconfig_names", qc.getName());
            return true;
        }

        public static QuickConfig LoadQuickConfig(string name)
        {
            string key = "quickconfig_" + System.Net.WebUtility.UrlEncode(name).Replace("+", "_");
            List<string> parms = Settings.Instance.GetList(key + "_params").ToList();
            List<string> tabs = Settings.Instance.GetList(key + "_tabs").ToList();
            return new QuickConfig(name, parms, tabs);
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
