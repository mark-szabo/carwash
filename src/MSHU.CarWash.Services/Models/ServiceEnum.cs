using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Web;

namespace MSHU.CarWash.Services.Models
{
    public enum ServiceEnum
    {
        [Description("KÜLSŐ MOSÁS")]
        KulsoMosas = 0,

        [Description("BELSŐ TAKARÍTÁS")]
        BelsoTakaritas = 1,

        [Description("KÜLSŐ MOSÁS + BELSŐ TAKARÍTÁS")]
        KulsoMosasBelsoTakaritas = 2,

        [Description("KÜLSŐ MOSÁS + BELSŐ TAKARÍTÁS + KÁRPITTISZTÍTÁS")]
        KulsoMosasBelsoTakaritasKarpittisztitas = 3
    }
}