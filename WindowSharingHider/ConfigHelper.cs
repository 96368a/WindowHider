using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class ConfigHelper
{
    private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");

    // 从配置文件中加载选中的进程名
    public static HashSet<string> LoadSelectedProcessNames()
    {
        if (File.Exists(ConfigFilePath))
        {
            var lines = File.ReadAllLines(ConfigFilePath);
            return new HashSet<string>(lines);
        }
        return new HashSet<string>();
    }

    // 将选中的进程名保存到配置文件
    public static void SaveSelectedProcessNames(HashSet<string> selectedProcessNames)
    {
        File.WriteAllLines(ConfigFilePath, selectedProcessNames);
    }
}
