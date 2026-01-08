using System;
using System.Collections.Generic;
using System.Text;

namespace LicenceController.Core.Models
{
    public class LicenceCacheEntry
    {
        public string Data { get; set; } = null;
        public string Signature { get; set; } = null;
        public string? ModulesJson { get; set; }
    }
}
