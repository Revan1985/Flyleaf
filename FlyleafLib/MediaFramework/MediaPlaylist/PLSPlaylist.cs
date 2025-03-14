﻿using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace FlyleafLib.MediaFramework.MediaPlaylist;

public class PLSPlaylist
{
    [DllImport("kernel32")]
    private static extern long WritePrivateProfileString(string name, string key, string val, string filePath);
    [DllImport("kernel32")]
    private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

    public string path;

    public static List<PLSPlaylistItem> Parse(string filename)
    {
        List<PLSPlaylistItem> items = new();
        string res;
        int entries = 1000;

        if ((res = GetINIAttribute("playlist", "NumberOfEntries", filename)) != null)
            entries = int.Parse(res);

        for (int i=1; i<=entries; i++)
        {
            if ((res = GetINIAttribute("playlist", $"File{i}", filename)) == null)
                break;

            PLSPlaylistItem item = new() { Url = res };

            if ((res = GetINIAttribute("playlist", $"Title{i}", filename)) != null)
                item.Title = res;

            if ((res = GetINIAttribute("playlist", $"Length{i}", filename)) != null)
                item.Duration = int.Parse(res);

            items.Add(item);
        }

        return items;
    }

    public static string GetINIAttribute(string name, string key, string path)
    {
        StringBuilder sb = new(255);
        return GetPrivateProfileString(name, key, "", sb, 255, path) > 0
            ? sb.ToString() : null;
    }
}

public class PLSPlaylistItem
{
    public int      Duration    { get; set; }
    public string   Title       { get; set; }
    public string   Url         { get; set; }
}
