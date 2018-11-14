using ModSettings;
using System.Reflection;

namespace Solstice
{
    public class GUISettings : ModSettingsBase
    {
        [Section("Solstice")]

        [Name("Enabled")]
        [Description("If enabled the length of the day will cycle from short to long and back to short.")]
        public bool Enabled = true;

        [Name("Cycle Length")]
        [Description("The length of a full cycle in in-game time.")]
        [Choice("2 Months", "4 Months", "6 Months", "12 Months")]
        public int CycleLength = 1;

        [Name("Start Day")]
        [Description("Where in the cycle the game starts.\nWinter: days are short and cold\nSpring: days are getting longer and warmer\nSummer: days are long and warm\nAutumn: days are getting shorter and colder")]
        public StartOffset StartDay = StartOffset.Random;

        protected override void OnChange(FieldInfo field, object oldValue, object newValue)
        {
            if (field.Name == "Enabled")
            {
                bool visible = (bool)newValue;
                this.SetFieldVisible(typeof(GUISettings).GetField("CycleLength"), visible);
                this.SetFieldVisible(typeof(GUISettings).GetField("StartDay"), visible);
            }
        }

        protected override void OnConfirm()
        {
            Implementation.ApplySettings();
        }
    }

    public enum StartOffset
    {
        Random = 0,
        Winter = 1,
        Spring = 2,
        Summer = 3,
        Autumn = 4,
    }
}