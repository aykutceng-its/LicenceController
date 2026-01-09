using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LicenceController.Core.Models
{
    public class Licence
    {
        public string CPUID { get; set; }
        public string DISKNO { get; set; }
        public string MAC { get; set; }
        public string CREATED { get; set; }
        public string EXPIRED { get; set; }
        public int DURATION { get; set; }
        public int TYPE { get; set; }
        public string SIGN { get; set; }
        public string UPDATED { get; set; }
        public Dictionary<string, bool> MODULES { get; set; } = new Dictionary<string, bool>();
    }
}
