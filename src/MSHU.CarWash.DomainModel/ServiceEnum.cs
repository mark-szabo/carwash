using System.ComponentModel.DataAnnotations;

namespace MSHU.CarWash.DomainModel
{
    public enum ServiceEnum
    {
        [Display(Description = "External")]
        KulsoMosas = 0,

        [Display(Description = "Internal ")]
        BelsoTakaritas = 1,

        [Display(Description = "External + internal")]
        KulsoMosasBelsoTakaritas = 2,

        [Display(Description = "External + internal + carpet")]
        KulsoMosasBelsoTakaritasKarpittisztitas = 3,

        [Display(Description = "External (steam)")]
        KulsoMosasGozos = 4,

        [Display(Description = "Internal (steam)")]
        BelsoTakaritasGozos = 5,

        [Display(Description = "External + internal (steam)")]
        KulsoMosasBelsoTakaritasGozos = 6
    }
}