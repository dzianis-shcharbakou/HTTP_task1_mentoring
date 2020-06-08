using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace HTTP_core
{
    public class NodeComparer : IEqualityComparer<HtmlNode>
    {
        public bool Equals([AllowNull] HtmlNode x, [AllowNull] HtmlNode y)
        {
            return x.Attributes["href"].Value == y.Attributes["href"].Value ? true : false;
        }

        public int GetHashCode([DisallowNull] HtmlNode obj)
        {
            return obj.Attributes["href"].Value.GetHashCode() * 17;
        }
    }
}
