using Harmony;
using System.Reflection;
using UnityEngine;

namespace Solstice
{
    internal class Implementation
    {
        private const float INTERPOLATOR_STEPS = 12;
        private const string SAVE_FILE_NAME = "solstice-settings";

        private static readonly Interpolator interpolator = new Interpolator();
        private static readonly InterpolatedValue sunAngle = new InterpolatedValue(30, 47);
        private static readonly InterpolatedValue dawn = new InterpolatedValue(5, -7);
        private static readonly InterpolatedValue sunrise = new InterpolatedValue(6, -6.333f);
        private static readonly InterpolatedValue sunset = new InterpolatedValue(18, 6.333f);
        private static readonly InterpolatedValue dusk = new InterpolatedValue(19, 6.8f);

        private static readonly GUISettings guiSettings = new GUISettings();
        private static readonly Settings settings = new Settings();

        private static float[] originalKeyframeTimes;
        private static float originalMasterTimeKeyOffset;

        private static int forcedDay = -1;
        private static float strength = 1;
        private static Traverse traverseKeyframeTimes;

        internal static float BrightnessMultiplier
        {
            get; private set;
        }

        internal static int CycleLength
        {
            get => settings.cycleLength;
            private set => settings.cycleLength = value;
        }

        internal static int CycleOffset
        {
            get => settings.cycleOffset;
            private set => settings.cycleOffset = value;
        }

        internal static bool Enabled
        {
            get => settings.enabled;
            private set => settings.enabled = value;
        }

        internal static float TemperatureOffset
        {
            get; private set;
        }

        public static void OnLoad()
        {
            Log("Version {0}", Assembly.GetExecutingAssembly().GetName().Version);

            guiSettings.AddToCustomModeMenu(ModSettings.Position.BelowEnvironment);

            interpolator.Set(0, -0.492f);
            interpolator.Set(1, -0.415f);
            interpolator.Set(2, -0.216f);
            interpolator.Set(3, 0.008f);
            interpolator.Set(4, 0.264f);
            interpolator.Set(5, 0.44f);
            interpolator.Set(6, 0.5f);
            interpolator.Set(7, 0.443f);
            interpolator.Set(8, 0.264f);
            interpolator.Set(9, 0.02f);
            interpolator.Set(10, -0.225f);
            interpolator.Set(11, -0.419f);
            interpolator.Set(12, -0.492f);

            uConsole.RegisterCommand("solstice-day", SolsticeDay);
        }

        internal static void ApplySettings()
        {
            Enabled = guiSettings.Enabled;

            switch (guiSettings.CycleLength)
            {
                case 0:
                    settings.cycleLength = 60;
                    break;

                case 1:
                    CycleLength = 120;
                    break;

                case 2:
                    CycleLength = 180;
                    break;

                default:
                    CycleLength = 360;
                    break;
            }

            switch (guiSettings.StartDay)
            {
                case StartOffset.Random:
                    CycleOffset = (int)(Random.value * CycleLength);
                    break;

                case StartOffset.Winter:
                    CycleOffset = 0;
                    break;

                case StartOffset.Spring:
                    CycleOffset = (int)(CycleLength * 0.25);
                    break;

                case StartOffset.Summer:
                    CycleOffset = (int)(CycleLength * 0.5);
                    break;

                case StartOffset.Autumn:
                    CycleOffset = (int)(CycleLength * 0.75);
                    break;
            }

            Log("Enabled: " + Enabled + ", Cycle Length: " + CycleLength + ", Cycle Offset: " + CycleOffset);
            RestoreKeyframeTimes(GameManager.GetUniStorm());
            Update(GameManager.GetUniStorm());
        }

        internal static void Disable()
        {
            if (Enabled)
            {
                Enabled = false;
                RestoreKeyframeTimes(GameManager.GetUniStorm());
            }
        }

        internal static void Init(UniStormWeatherSystem uniStormWeatherSystem)
        {
            traverseKeyframeTimes = Traverse.Create(uniStormWeatherSystem).Field("m_TODKeyframeTimes");

            originalMasterTimeKeyOffset = uniStormWeatherSystem.m_MasterTimeKeyOffset;
            originalKeyframeTimes = (float[])traverseKeyframeTimes.GetValue<float[]>().Clone();
        }

        internal static void LoadData(string name)
        {
            string data = SaveGameSlots.LoadDataFromSlot(name, SAVE_FILE_NAME);
            Implementation.SetSettingsData(data);
        }

        internal static void Log(string message)
        {
            Debug.Log("[Solstice] " + message);
        }

        internal static void Log(string message, params object[] parameters)
        {
            string preformattedMessage = string.Format(message, parameters);
            Log(preformattedMessage);
        }

        internal static void SaveData(SaveSlotType gameMode, string name)
        {
            string data = Utils.SerializeObject(settings);
            SaveGameSlots.SaveDataToSlot(gameMode, SaveGameSystem.m_CurrentEpisode, SaveGameSystem.m_CurrentGameId, name, SAVE_FILE_NAME, data);
        }

        internal static void SolsticeDay()
        {
            int numParameters = uConsole.GetNumParameters();
            if (numParameters == 1)
            {
                forcedDay = uConsole.GetInt() % CycleLength;
                Update(GameManager.GetUniStorm());
            }
        }

        internal static void Update(UniStormWeatherSystem uniStormWeatherSystem)
        {
            if (!Enabled)
            {
                return;
            }

            int cycleDay = (uniStormWeatherSystem.m_DayCounter + CycleOffset) % CycleLength;
            if (forcedDay >= 0)
            {
                cycleDay = forcedDay;
            }

            strength = interpolator.GetValue(INTERPOLATOR_STEPS * cycleDay / CycleLength);
            BrightnessMultiplier = Mathf.Clamp(1.1f + strength, 0, 1);
            TemperatureOffset = Mathf.Clamp(10 * strength, float.NegativeInfinity, 0);
            ConfigureKeyframeTimes(uniStormWeatherSystem);

            Debug.Log("Day: " + cycleDay + " / " + CycleLength + "; strength: " + strength + "; BrightnessMultiplier: " + BrightnessMultiplier + "; TemperatureOffset: " + TemperatureOffset);
        }

        private static void ConfigureKeyframeTimes(UniStormWeatherSystem uniStormWeatherSystem)
        {
            uniStormWeatherSystem.m_MasterTimeKeyOffset = 0;

            float[] keyframeTimes = traverseKeyframeTimes.GetValue<float[]>();

            keyframeTimes[0] = dawn.Calculate(strength);
            keyframeTimes[1] = sunrise.Calculate(strength);
            keyframeTimes[2] = keyframeTimes[1] + 1;
            keyframeTimes[3] = 12;

            keyframeTimes[6] = dusk.Calculate(strength);
            keyframeTimes[5] = sunset.Calculate(strength);
            keyframeTimes[4] = keyframeTimes[5] - 1;

            uniStormWeatherSystem.m_SunAngle = sunAngle.Calculate(strength);
        }

        private static void RestoreKeyframeTimes(UniStormWeatherSystem uniStormWeatherSystem)
        {
            float[] keyframeTimes = traverseKeyframeTimes.GetValue<float[]>();
            for (int i = 0; i < keyframeTimes.Length; i++)
            {
                keyframeTimes[i] = originalKeyframeTimes[i];
            }

            uniStormWeatherSystem.m_MasterTimeKeyOffset = originalMasterTimeKeyOffset;
        }

        private static void SetSettingsData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                Disable();
            }
            else
            {
                JsonUtility.FromJsonOverwrite(data, settings);
                Update(GameManager.GetUniStorm());
            }

            Log("Enabled: " + Enabled + ", Cycle Length: " + settings.cycleLength + ", Cycle Offset: " + settings.cycleOffset);
        }
    }

    internal class Settings
    {
        public int cycleLength;
        public int cycleOffset;
        public bool enabled;
    }
}