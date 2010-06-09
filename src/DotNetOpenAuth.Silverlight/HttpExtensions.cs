using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Browser;

namespace DotNetOpenAuth.Silverlight {
    /* This was taken from here:
     * http://aspadvice.com/blogs/ssmith/archive/2008/01/16/Silverlight-Hyperlink.aspx
     * and then tweaker with the help of Reflector
     */
    public static class SilverlightExtensions {

        public static Dictionary<string, string> ParseQueryString(string query) {
            return ParseQueryString(query, Encoding.UTF8);
        }

        public static Dictionary<string, string> ParseQueryString(string query, Encoding encoding) {
            if (query == null) {
                throw new ArgumentNullException("query");
            }
            if (encoding == null) {
                throw new ArgumentNullException("encoding");
            }
            if ((query.Length > 0) && (query[0] == '?')) {
                query = query.Substring(1);
            }

            return FillFromString(query, true, encoding);
        }

        internal static Dictionary<string, string> FillFromString(string s, bool urlencoded, Encoding encoding) {
            var dictionary = new Dictionary<string, string>();
            int num = (s != null) ? s.Length : 0;
            for (int i = 0; i < num; i++) {
                int startIndex = i;
                int num4 = -1;
                while (i < num) {
                    if (s != null) {
                        char ch = s[i];
                        switch (ch) {
                            case '=':
                                if (num4 < 0) {
                                    num4 = i;
                                }
                                break;
                            case '&':
                                break;
                        }
                    }
                    i++;
                }
                string str = null;
                string str2 = null;
                if (num4 >= 0) {
                    if (s != null) {
                        str = s.Substring(startIndex, num4 - startIndex);
                        str2 = s.Substring(num4 + 1, (i - num4) - 1);
                    }
                } else {
                    if (s != null) str2 = s.Substring(startIndex, i - startIndex);
                }
                if (urlencoded) {
                    dictionary.Add(HttpUtility.UrlDecode(str/*, encoding*/), HttpUtility.UrlDecode(str2/*, encoding*/));
                } else {
                    if (str != null) dictionary.Add(str, str2);
                }
                if (s != null)
                    if ((i == (num - 1)) && (s[i] == '&')) {
                        dictionary.Add(null, string.Empty);
                    }
            }
            return dictionary;
        }

    }
}
