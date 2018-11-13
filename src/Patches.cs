using Harmony;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using UnityEngine;

namespace Solstice
{
    [HarmonyPatch(typeof(StatsManager), "Reset")]
    internal class StatsManager_Reset
    {
        internal static void Postfix()
        {
            if (!GameManager.InCustomMode())
            {
                Implementation.Disable();
            }
        }
    }

    [HarmonyPatch(typeof(SaveGameSystem), "RestoreGlobalData")]
    internal class SaveGameSystemPatch_RestoreGlobalData
    {
        internal static void Postfix(string name)
        {
            Implementation.LoadData(name);
        }
    }

    [HarmonyPatch(typeof(SaveGameSystem), "SaveGlobalData")]
    internal class SaveGameSystemPatch_SaveGlobalData
    {
        public static void Postfix(SaveSlotType gameMode, string name)
        {
            Implementation.SaveData(gameMode, name);
        }
    }

    [HarmonyPatch(typeof(UniStormWeatherSystem), "Init")]
    internal class UniStormWeatherSystem_Init
    {
        internal static void Postfix(UniStormWeatherSystem __instance)
        {
            Implementation.Init(__instance);
        }
    }

    [HarmonyPatch(typeof(UniStormWeatherSystem), "Update")]
    internal class UniStormWeatherSystem_Update
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Call)
                {
                    continue;
                }

                MethodInfo methodInfo = codes[i].operand as MethodInfo;
                if (methodInfo != null && methodInfo.Name == "UpdateSunTransform")
                {
                    codes[i - 1].opcode = OpCodes.Nop;
                    codes[i].opcode = OpCodes.Nop;
                    break;
                }
            }

            return codes;
        }
    }

    [HarmonyPatch(typeof(UniStormWeatherSystem), "SetMoonPhase")]
    internal class UniStormWeatherSystem_SetMoonPhase
    {
        internal static void Prefix(UniStormWeatherSystem __instance)
        {
            Implementation.Update(__instance);
        }
    }

    [HarmonyPatch(typeof(TODStateConfig), "SetBlended")]
    internal class UniStormWeatherSystem_SetNormalizedTime
    {
        internal static void Postfix(TODStateConfig __instance)
        {
            if (GameManager.GetUniStorm().IsNightOrNightBlend() || !Implementation.Enabled)
            {
                return;
            }

            float brightnessMultiplier = Implementation.BrightnessMultiplier;
            __instance.m_SunLightIntensity *= brightnessMultiplier;
            __instance.m_SkyBloomIntensity *= brightnessMultiplier;
            __instance.m_SkyFogColor *= brightnessMultiplier;
            __instance.m_FogColor *= brightnessMultiplier;
        }
    }

    [HarmonyPatch(typeof(UniStormWeatherSystem), "UpdateSunTransform")]
    internal class UniStormWeatherSystem_UpdateSunTransform
    {
        private static readonly float REFEERENCE_ANGLE = 45;
        private static readonly float REFERENCE_HEIGHT = Mathf.Sin(Mathf.Deg2Rad * REFEERENCE_ANGLE);

        internal static bool Prefix(UniStormWeatherSystem __instance)
        {
            if (!Implementation.Enabled)
            {
                return true;
            }

            Transform transform = __instance.m_SunLight.transform;

            transform.forward = new Vector3(0, -REFERENCE_HEIGHT, REFERENCE_HEIGHT);
            transform.Rotate(Vector3.up, __instance.m_NormalizedTime * 360f - 180f);

            float offset = REFERENCE_HEIGHT - Mathf.Sin(Mathf.Deg2Rad * __instance.m_SunAngle);
            transform.LookAt(new Vector3(transform.forward.x, transform.forward.y + offset, transform.forward.z));

            return false;
        }
    }

    [HarmonyPatch(typeof(Weather), "GenerateTempHigh")]
    internal class Weather_GenerateTempHigh
    {
        internal static void Postfix(Weather __instance)
        {
            if (!Implementation.Enabled)
            {
                return;
            }

            Traverse traverse = Traverse.Create(__instance).Field("m_TempHigh");
            float tempHigh = traverse.GetValue<float>();
            traverse.SetValue(tempHigh + Implementation.TemperatureOffset);
        }
    }

    [HarmonyPatch(typeof(Weather), "GenerateTempLow")]
    internal class Weather_GenerateTempLow
    {
        internal static void Postfix(Weather __instance)
        {
            if (!Implementation.Enabled)
            {
                return;
            }

            Traverse traverse = Traverse.Create(__instance).Field("m_TempLow");
            float tempLow = traverse.GetValue<float>();
            traverse.SetValue(tempLow + Implementation.TemperatureOffset);
        }
    }
}