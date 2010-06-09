using System;
using System.Linq;

namespace DotNetOpenAuth.Silverlight {
    /*
     * This was taken from here:
     * http://www.dolittle.com/blogs/einar/archive/2008/01/13/missing-enum-getvalues-when-doing-silverlight-for-instance.aspx
     * and then ReSharpened
     */
    public static class EnumHelper {
        public static T[] GetValues<T>() {
            Type enumType = typeof(T);

            if (!enumType.IsEnum) {
                throw new ArgumentException("Type '" + enumType.Name + "' is not an enum");
            }

            var fields = from field in enumType.GetFields()
                         where field.IsLiteral
                         select field;

            return fields.Select(field => field.GetValue(enumType)).Select(value => (T) value).ToArray();
        }

        public static object[] GetValues(Type enumType) {
            if (!enumType.IsEnum) {
                throw new ArgumentException("Type '" + enumType.Name + "' is not an enum");
            }

            var fields = from field in enumType.GetFields()
                         where field.IsLiteral
                         select field;

            return fields.Select(field => field.GetValue(enumType)).ToArray();
        }

    }
}
