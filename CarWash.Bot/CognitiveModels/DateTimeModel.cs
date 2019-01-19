using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace CarWash.Bot.CognitiveModels
{
    internal class DateTimeModel : CognitiveModel
    {
        public DateTimeModel(CognitiveModel model)
        {
            Type = model.Type;
            Text = model.Text;
        }

        public TimexProperty Timex { get; set; }
    }
}
