using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml.Serialization;

namespace BatchLooper.Core.Helpers
{
    public static class EnumHelper
    {
        public static string GetDescription<TEnum>(this TEnum enumValue) where TEnum : Enum
        {
            var attribute = enumValue.GetAttribute<DescriptionAttribute>();
            var result = attribute?.Description?.Trim() ?? nameof(enumValue);

            return result;
        }

        public static string GetXmlEnum<TEnum>(this TEnum value) where TEnum : Enum
        {
            var attribute = value.GetAttribute<XmlEnumAttribute>();
            var result = attribute?.Name ?? nameof(value);

            return result;
        }

        /// <summary>
        /// Gets all items for an enum value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static IEnumerable<TEnum> GetAllItems<TEnum>(this Enum value) where TEnum : Enum
        {
            foreach (object item in Enum.GetValues(typeof(TEnum)))
                yield return (TEnum)item;
        }

        /// <summary>
        /// Gets all items for an enum type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static IEnumerable<TEnum> GetAllItems<TEnum>() where TEnum : struct, Enum
        {
            foreach (object item in Enum.GetValues(typeof(TEnum)))
                yield return (TEnum)item;
        }

        /// <summary>
        /// Determines whether the enum value contains a specific value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="request">The request.</param>
        /// <returns>
        ///     <c>true</c> if value contains the specified value; otherwise, <c>false</c>.
        /// </returns>
        /// <example>
        /// <code>
        /// EnumExample dummy = EnumExample.Combi;
        /// if (dummy.Contains<EnumExample>(EnumExample.ValueA))
        /// {
        ///     Console.WriteLine("dummy contains EnumExample.ValueA");
        /// }
        /// </code>
        /// </example>
        public static bool Contains<TValue>(this Enum value, TValue request) where TValue : struct
        {
            int valueAsInt = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            int requestAsInt = Convert.ToInt32(request, CultureInfo.InvariantCulture);

            var result = requestAsInt == (valueAsInt & requestAsInt);
            return result;
        }

        public static bool Contains<TEnum, TVal>([NotNull] this TVal value)
           where TEnum : struct, Enum
        {
            ArgumentNullException.ThrowIfNull(value, nameof(value));

            var lstValues = Enum.GetValues<TEnum>()
                .ToList();

            var equalDescription = lstValues.Any(x => x.GetDescription().Equals(value.ToString()));
            var equalXmlEnum = lstValues.Any(x => x.GetXmlEnum().Equals(value.ToString()));
            var equalValue = lstValues
                .Select(s => s.Cast<TEnum, TVal>())
                .Any(x => x != null && x.Equals(value));

            var result = equalDescription || equalXmlEnum || equalValue;

            return result;
        }

        public static TEnum ToEnum<TEnum, TValue>([NotNull] this TValue value) where TEnum : struct, Enum
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (!value.Contains<TEnum, TValue>()) throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid enum value.");

            var lstValues = Enum.GetValues<TEnum>()
                .ToList();

            TEnum result = default;

            if (Enum.TryParse(value.ToString(), true, out TEnum casted))
                result = casted;

            if (result.Equals(default(TEnum)) && lstValues.Any(x => value.CompareByAttribute(x)))
            {
                result = lstValues
                .FirstOrDefault(x => value.CompareByAttribute(x));
            }

            return result;
        }

        private static bool CompareByAttribute<TEnum, TValue>([NotNull] this TValue value, TEnum enumeration, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
            where TEnum : Enum
        {
            ArgumentNullException.ThrowIfNull(value);

            var result = enumeration.GetDescription().Equals(value.ToString(), comparison)
                  || enumeration.GetXmlEnum().Equals(value.ToString(), comparison);

            return result;
        }
    }
}
