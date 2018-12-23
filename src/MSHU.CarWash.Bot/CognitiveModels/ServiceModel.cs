using MSHU.CarWash.ClassLibrary.Enums;

namespace MSHU.CarWash.Bot.CognitiveModels
{
    internal class ServiceModel : CognitiveModel
    {
        public ServiceModel(CognitiveModel model)
        {
            Type = model.Type;
            Text = model.Text;
        }

        public ServiceType Service { get; set; }
    }
}
