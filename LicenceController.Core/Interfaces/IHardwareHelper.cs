using System;
using System.Collections.Generic;
using System.Text;
using LicenceController.Core.Services;

namespace LicenceController.Core.Interfaces
{
    public interface IHardwareHelper
    {
        string GetCpuId();
        string GetDiskSerial();
        string GetMacAddress();
        string GetHardwareId(); // Hepsini birleştirip döner

        //CPUID 1,
        //MAC 2,
        //DISKNO 3,
        //TYPE 4,
        //DURATION 5,
        //CREATED 6,
        //EXPIRED 7,
        //SIGN
    }
}
