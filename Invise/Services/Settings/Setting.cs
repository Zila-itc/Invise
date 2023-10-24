using System;
using System.IO;
using Invise.Model;
using System.Text.Json;
using Invise.ViewModel;
using System.Text.Json.Nodes;
using System.Collections.Generic;

namespace Invise.Services.Settings;
public class Setting : BaseViewModel
{
    private string _settingsJsonPath = Directory.GetCurrentDirectory() + @"\settings.json";

    private List<InviseProfile> _inviseProfiles;
    public List<InviseProfile> InviseProfiles
    {
        get => _inviseProfiles;
        set => Set(ref _inviseProfiles, value);
    }

    public Setting()
    {
        if (!LoadSettings()) { SetDefaultSettings(); }
    }

    private void SetDefaultSettings() { InviseProfiles = new(); }
    public void ParseJson(JsonNode json)
    {
        if (json[nameof(InviseProfiles)] != null)
            InviseProfiles = JsonSerializer.Deserialize<List<InviseProfile>>(json[nameof(InviseProfiles)]);
        else
            InviseProfiles = new List<InviseProfile>();
    }

    public void SaveSettings()
    {
        using StreamWriter writer = new(_settingsJsonPath);
        var doc = JsonSerializer.Serialize(this);
        writer.Write(doc);
        writer.Close();
    }

    private bool LoadSettings()
    {
        try
        {
            using StreamReader reader = new(_settingsJsonPath);
            var json = reader.ReadToEnd();
            ParseJson(JsonNode.Parse(json));
            reader.Close();
            return true;
        }
        catch (Exception) { return false; }
    }
}
