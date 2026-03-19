using Accord.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MissionPlanner.GridFlight
{
    internal class QuickConfig
    {
        private string[] paramsShown;
        private string name;
        
        public QuickConfig(string name, string[] paramsShown)
        {
            this.name = name;
            this.paramsShown = paramsShown;
        }

        public string getName()
        {
            return name;
        }
            

        public void setName(string name)
        {
            this.name = name.Trim();
        }

        public string[] getParams()
        {
            return paramsShown;
        }

    }
}
