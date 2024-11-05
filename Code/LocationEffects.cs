using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Extensions;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace StardewValleyExpanded
{
    public static class LocationEffectsCommon
    {
        public class Holder
        {
            public Color ambientLightColor = Color.White;
            public List<Vector2> baubles;
            public List<WeatherDebris> weatherDebris;
        }

        private static ConditionalWeakTable<GameLocation, Holder> data = new();

        public static Holder GetExtData(this GameLocation location)
        {
            return data.GetOrCreateValue(location);
        }
        public static void UpdateWoodsLighting(GameLocation loc)
        {
            if (Game1.currentLocation != loc)
                return;
            var data = loc.GetExtData();

            int fade_start_time = Utility.ConvertTimeToMinutes(Game1.getStartingToGetDarkTime(loc));
            int fade_end_time = Utility.ConvertTimeToMinutes(Game1.getModeratelyDarkTime(loc));
            int light_fade_start_time = Utility.ConvertTimeToMinutes(Game1.getModeratelyDarkTime(loc));
            int light_fade_end_time = Utility.ConvertTimeToMinutes(Game1.getTrulyDarkTime(loc));
            float num = (float)Utility.ConvertTimeToMinutes(Game1.timeOfDay) + (float)Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameMinute;
            float lerp = Utility.Clamp((num - (float)fade_start_time) / (float)(fade_end_time - fade_start_time), 0f, 1f);
            float light_lerp = Utility.Clamp((num - (float)light_fade_start_time) / (float)(light_fade_end_time - light_fade_start_time), 0f, 1f);
            Game1.ambientLight.R = (byte)Utility.Lerp((int)data.ambientLightColor.R, (int)Game1.outdoorLight.R, lerp);
            Game1.ambientLight.G = (byte)Utility.Lerp((int)data.ambientLightColor.G, (int)Game1.outdoorLight.G, lerp);
            Game1.ambientLight.B = (byte)Utility.Lerp((int)data.ambientLightColor.B, (int)Game1.outdoorLight.B, lerp);
            Game1.ambientLight.A = (byte)Utility.Lerp((int)data.ambientLightColor.A, (int)Game1.outdoorLight.A, lerp);
            Color light_color = Color.Black;
            light_color.A = (byte)Utility.Lerp(255f, 0f, light_lerp);
            foreach (LightSource light in Game1.currentLightSources.Values)
            {
                if (light.lightContext.Value == LightSource.LightContext.MapLight)
                {
                    light.color.Value = light_color;
                }
            }
        }
    }

    [HarmonyPatch(typeof(GameLocation), "resetLocalState")]
    public static class GameLocationWoodsEffectPatch1
    {
        public static void Prefix(GameLocation __instance)
        {
            if (//__instance.Name!="FarmHouse" &&
                __instance.Name != "Custom_JunimoWoods" && __instance.Name != "Custom_SpriteSpring2")
                return;
            var data = __instance.GetExtData();

            __instance.ignoreOutdoorLighting.Value = false;
            __instance.ignoreDebrisWeather.Value = true;
            data.ambientLightColor = new Color(150, 120, 50);

            LocationEffectsCommon.UpdateWoodsLighting(__instance);

            Random r = Utility.CreateDaySaveRandom();
            int numberOfBaubles = 25 + r.Next(0, 75);
            data.baubles = new List<Vector2>();
            for (int i = 0; i < numberOfBaubles; i++)
            {
                data.baubles.Add(new Vector2(Game1.random.Next(0, __instance.map.DisplayWidth), Game1.random.Next(0, __instance.map.DisplayHeight)));
            }

            Season season = __instance.GetSeason();
            if (season != Season.Winter)
            {
                data.weatherDebris = new List<WeatherDebris>();
                int spacing = 192;
                int leafType = 1;
                if (season == Season.Fall)
                {
                    leafType = 2;
                }
                for (int j = 0; j < numberOfBaubles; j++)
                {
                    data.weatherDebris.Add(new WeatherDebris(new Vector2(j * spacing % Game1.graphics.GraphicsDevice.Viewport.Width + Game1.random.Next(spacing), j * spacing / Game1.graphics.GraphicsDevice.Viewport.Width * spacing % Game1.graphics.GraphicsDevice.Viewport.Height + Game1.random.Next(spacing)), leafType, (float)Game1.random.Next(15) / 500f, (float)Game1.random.Next(-10, 0) / 50f, (float)Game1.random.Next(10) / 50f));
                }
            }
        }
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.UpdateWhenCurrentLocation))]
    public static class GameLocationWoodsEffectPatch2
    {
        public static void Postfix(GameLocation __instance, GameTime time)
        {
            if (//__instance.Name != "FarmHouse" &&
                __instance.Name != "Custom_JunimoWoods" && __instance.Name != "Custom_SpriteSpring2")
                return;
            var data = __instance.GetExtData();

            LocationEffectsCommon.UpdateWoodsLighting(__instance);

            if (data.baubles != null)
            {
                for (int i = 0; i < data.baubles.Count; i++)
                {
                    Vector2 v = default(Vector2);
                    v.X = data.baubles[i].X - Math.Max(0.4f, Math.Min(1f, (float)i * 0.01f)) - (float)((double)((float)i * 0.01f) * Math.Sin(Math.PI * 2.0 * (double)time.TotalGameTime.Milliseconds / 8000.0));
                    v.Y = data.baubles[i].Y + Math.Max(0.5f, Math.Min(1.2f, (float)i * 0.02f));
                    if (v.Y > (float)__instance.map.DisplayHeight || v.X < 0f)
                    {
                        v.X = Game1.random.Next(0, __instance.map.DisplayWidth);
                        v.Y = -64f;
                    }
                    data.baubles[i] = v;
                }
            }
            if (data.weatherDebris == null)
            {
                return;
            }
            foreach (WeatherDebris weatherDebri in data.weatherDebris)
            {
                weatherDebri.update();
            }
            Game1.updateDebrisWeatherForMovement(data.weatherDebris);
        }
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.drawAboveAlwaysFrontLayer))]
    public static class GameLocationWoodsEffectPatch3
    {
        public static void Postfix(GameLocation __instance, SpriteBatch b)
        {
            if (//__instance.Name != "FarmHouse" &&
                __instance.Name != "Custom_JunimoWoods" && __instance.Name != "Custom_SpriteSpring2")
                return;
            var data = __instance.GetExtData();

            if (data.baubles != null)
            {
                for (int i = 0; i < data.baubles.Count; i++)
                {
                    b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, data.baubles[i]), new Microsoft.Xna.Framework.Rectangle(346 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(i * 25)) % 600.0) / 150 * 5, 1971, 5, 5), Color.White, (float)i * ((float)Math.PI / 8f), Vector2.Zero, 4f, SpriteEffects.None, 1f);
                }
            }
            if (data.weatherDebris == null || __instance.currentEvent != null)
            {
                return;
            }
            foreach (WeatherDebris weatherDebri in data.weatherDebris)
            {
                weatherDebri.draw(b);
            }
        }
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.getFootstepSoundReplacement))]
    public static class GameLocationSandyBadlandsPatch
    {
        public static void Postfix(GameLocation __instance, ref string __result)
        {
            if (__instance.Name == "Custom_CrimsonBadlands" || __instance.Name == "Custom_IridiumQuarry" ||
                __instance.Name == "WitchSwamp" || __instance.Name == "Custom_ForbiddenMaze" ||
                __instance.Name == "Custom_HenchmanBackyard")
                __result = "sandyStep";
        }
    }
}
