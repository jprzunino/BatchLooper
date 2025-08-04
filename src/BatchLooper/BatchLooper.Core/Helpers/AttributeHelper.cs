namespace BatchLooper.Core.Helpers
{
    public static class AttributeHelper
    {
        // This extension method is broken out so you can use a similar pattern with 
        // other MetaData elements in the future. This is your base method for each.
        public static TAttribute? GetAttribute<TAttribute>(this Enum value) where TAttribute : Attribute
        {
            var type = value.GetType();
            var memberInfo = type.GetMember(value.ToString());
            var fieldInfo = type.GetField(value.ToString());

            var attributes = memberInfo is not null && memberInfo.Any() ?
                memberInfo[0].GetCustomAttributes(typeof(TAttribute), false) :
                fieldInfo!.GetCustomAttributes(typeof(TAttribute), true);

            if (attributes is null || attributes.Length == 0)
                return null;

            var result = attributes.First().Cast<object, TAttribute>();

            return result;
        }
    }
}
