using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace PocFwIpApp.utils
{
    public static class MiscExtensions
    {

        public static void SetContent(this Hyperlink hyperlink, String textContent)
        {
            hyperlink.Inlines.Clear();
            hyperlink.Inlines.Add(textContent);
        }

    }
}
