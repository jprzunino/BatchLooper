using System.Globalization;

namespace BatchLooper.Core.Helpers
{
    public static class ConvertHelper
    {
        public static TTarget? Cast<TSource, TTarget>(this TSource? source)
        {
            if (source is null || (source is null && Nullable.GetUnderlyingType(typeof(TTarget)) is not null))
                return default;

            var type = Nullable.GetUnderlyingType(typeof(TTarget)) ?? typeof(TTarget);
            var result = (TTarget?)Convert.ChangeType(source, type, CultureInfo.InvariantCulture);

            return result;
        }

        public static TTarget? Cast<TSource, TTarget>(this TSource? source, TypeCode type)
        {
            if (source is null)
                return default;

            var result = (TTarget?)Convert.ChangeType(source, type, CultureInfo.InvariantCulture);

            return result;
        }

        public static object? Cast<TSource>(this TSource? source, Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            if (source is null || (source is null && Nullable.GetUnderlyingType(type) is not null))
                return default;

            type = Nullable.GetUnderlyingType(type) ?? type;
            var result = Convert.ChangeType(source, type, CultureInfo.InvariantCulture);

            return result;
        }
    }
}
