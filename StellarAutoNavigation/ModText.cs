using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class ModTranslate
{
    static Dictionary<string, string> enusDict = new Dictionary<string, string>
    {
        { "Thruster Level Too Low","驱动引擎等级过低" },
        { "Mecha Energy Too Low","机甲能量过低" },
        { "Stellar Auto Navigation","星际自动导航" },
        { "Galaxy Auto Navigation","星系自动导航" },
        { "Dark Fog Hive Auto Navigation","黑雾巢穴自动导航" },
        { "Space Seed Auto Navigation","火种自动导航" },
        { "Dark Fog Communicator Auto Navigation","黑雾通讯器自动导航" },
        { "Cosmic Message Auto Navigation","宇宙讯息自动导航" },
        { "Navigation Mode Ended","导航结束" },
    };

    public static string LocalText(this string text)
    {
        if (!Localization.isZHCN)
        {
            return text;
        }
        else
        {
            string s;
            if(enusDict.TryGetValue(text, out s))
            {
                return s;
            }
            return text;
        }
    }
}