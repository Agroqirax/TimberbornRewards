#nullable enable
using System;
using System.Reflection;
using Timberborn.GameCycleSystem;
using Timberborn.HazardousWeatherSystem;
using Timberborn.Localization;
using Timberborn.WeatherSystem;
using UnityEngine;

namespace Agroqirax.Rewards
{
    /// <summary>
    /// A reward that adjusts the duration of either the Temperate or Hazardous
    /// season of the current cycle by a fixed number of days.
    ///
    /// Positive <see cref="Amount"/> lengthens the season; negative shortens it.
    /// Duration is clamped to a minimum of 1 day so the season is never skipped.
    ///
    /// After mutating the season duration, <c>GameCycleService._cycleDurationInDays</c>
    /// is patched via reflection so the game's day counter ends the cycle at the
    /// correct day rather than the originally-rolled total.
    /// </summary>
    public class WeatherReward : IReward
    {
        private const string WeatherLocKey   = "CycleReward.Weather";
        private const string TemperateLocKey = "CycleReward.Weather.Temperate";
        private const string HazardousLocKey = "CycleReward.Weather.Hazardous";

        private const int MinDurationDays = 1;

        // ---- Reflection cache --------------------------------------------------

        private static readonly PropertyInfo TemperateDurationProp =
            typeof(TemperateWeatherDurationService)
                .GetProperty("TemperateWeatherDuration",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "[CycleReward] Could not reflect TemperateWeatherDuration property.");

        private static readonly PropertyInfo HazardousDurationProp =
            typeof(HazardousWeatherService)
                .GetProperty("HazardousWeatherDuration",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "[CycleReward] Could not reflect HazardousWeatherDuration property.");

        private static readonly FieldInfo CycleDurationInDaysField =
            typeof(GameCycleService)
                .GetField("_cycleDurationInDays",
                    BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "[CycleReward] Could not reflect GameCycleService._cycleDurationInDays field.");

        // ------------------------------------------------------------------------

        private readonly TemperateWeatherDurationService _temperateService;
        private readonly HazardousWeatherService         _hazardousService;
        private readonly GameCycleService                _gameCycleService;

        public WeatherType Season { get; }
        public int         Amount { get; }

        public string? IconPath => Season == WeatherType.Temperate
            ? "ui/images/core/ico-weather-temperate"
            : "ui/images/core/ico-weather-badtide";

        public WeatherReward(
            TemperateWeatherDurationService temperateService,
            HazardousWeatherService         hazardousService,
            GameCycleService                gameCycleService,
            WeatherType                     season,
            int                             amount)
        {
            if (amount == 0)
                throw new ArgumentException("Amount must be non-zero.", nameof(amount));

            _temperateService = temperateService;
            _hazardousService = hazardousService;
            _gameCycleService = gameCycleService;
            Season = season;
            Amount = amount;
        }

        public string GetDisplayName(ILoc loc)
        {
            // Format Amount as an explicitly signed integer: "+3" or "-5".
            string signedAmount = Amount > 0 ? $"+{Amount}" : Amount.ToString();
            string seasonName   = loc.T(Season == WeatherType.Temperate ? TemperateLocKey : HazardousLocKey);
            return loc.T(WeatherLocKey, signedAmount, seasonName);
        }

        public void Apply()
        {
            int oldTotal = (int)CycleDurationInDaysField.GetValue(_gameCycleService);

            if (Season == WeatherType.Temperate)
                ApplyToTemperate();
            else
                ApplyToHazardous();

            // Recompute from authoritative sources so the cached value is correct
            // even when the clamp fires.
            int newTotal = _temperateService.TemperateWeatherDuration
                         + _hazardousService.HazardousWeatherDuration;
            CycleDurationInDaysField.SetValue(_gameCycleService, newTotal);

            Debug.Log($"[CycleReward] Cycle length {oldTotal} -> {newTotal} days " +
                      $"(temperate {_temperateService.TemperateWeatherDuration}, " +
                      $"hazardous {_hazardousService.HazardousWeatherDuration}).");
        }

        // -----------------------------------------------------------------------

        private void ApplyToTemperate()
        {
            int current = _temperateService.TemperateWeatherDuration;
            int next    = Math.Max(MinDurationDays, current + Amount);
            SetPropertyValue(TemperateDurationProp, _temperateService, next);
            Debug.Log($"[CycleReward] Temperate duration {current} -> {next} days (delta {Amount:+#;-#;0}).");
        }

        private void ApplyToHazardous()
        {
            int current = _hazardousService.HazardousWeatherDuration;
            int next    = Math.Max(MinDurationDays, current + Amount);
            SetPropertyValue(HazardousDurationProp, _hazardousService, next);
            Debug.Log($"[CycleReward] Hazardous duration {current} -> {next} days (delta {Amount:+#;-#;0}).");
        }

        private static void SetPropertyValue(PropertyInfo prop, object target, int value)
        {
            MethodInfo? setter = prop.GetSetMethod(nonPublic: true);
            if (setter != null)
            {
                setter.Invoke(target, new object[] { value });
                return;
            }

            string backingFieldName = $"<{prop.Name}>k__BackingField";
            FieldInfo? field = prop.DeclaringType!
                .GetField(backingFieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            if (field != null)
            {
                field.SetValue(target, value);
                return;
            }

            Debug.LogError(
                $"[CycleReward] Could not set '{prop.DeclaringType!.Name}.{prop.Name}' -- " +
                "neither a private setter nor a backing field was found. " +
                "Weather reward had no effect.");
        }
    }
}
