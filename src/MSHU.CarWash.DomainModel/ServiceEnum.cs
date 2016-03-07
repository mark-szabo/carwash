using System.ComponentModel.DataAnnotations;

namespace MSHU.CarWash.DomainModel
{
    public enum ServiceEnum
    {
        [Display(Description = "KÜLSŐ MOSÁS")]
        KulsoMosas = 0,

        [Display(Description = "BELSŐ TAKARÍTÁS")]
        BelsoTakaritas = 1,

        [Display(Description = "KÜLSŐ MOSÁS + BELSŐ TAKARÍTÁS")]
        KulsoMosasBelsoTakaritas = 2,

        [Display(Description = "KÜLSŐ MOSÁS + BELSŐ TAKARÍTÁS + KÁRPITTISZTÍTÁS")]
        KulsoMosasBelsoTakaritasKarpittisztitas = 3
    }
}