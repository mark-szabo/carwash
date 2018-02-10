using System.ComponentModel.DataAnnotations;

namespace MSHU.CarWash.DomainModel
{
    public enum ServiceEnum
    {
        [Display(Description = "EXTERIOR")]
        KulsoMosas = 0,

        [Display(Description = "INTERIOR")]
        BelsoTakaritas = 1,

        [Display(Description = "EXTERIOR + INTERIOR")]
        KulsoMosasBelsoTakaritas = 2,

        [Display(Description = "EXTERIOR + INTERIOR + CARPET")]
        KulsoMosasBelsoTakaritasKarpittisztitas = 3,

        [Display(Description = "EXTERIOR (STEAM)")]
        KulsoMosasGozos = 4,

        [Display(Description = "INTERIOR (STEAM)")]
        BelsoTakaritasGozos = 5,

        [Display(Description = "EXTERIOR + INTERIOR (STEAM)")]
        KulsoMosasBelsoTakaritasGozos = 6
    }
}