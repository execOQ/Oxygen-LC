using BepInEx.Configuration;
using Oxygen.Configuration;
using System;
using System.Globalization;
using System.IO;

class Program
{
    enum FillMode { DoorsClosed, OnShip, Off }
    static int checks;
    static void Assert(bool condition, string message)
    { if (!condition) throw new Exception(message); checks++; }

    static void Main()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "integration.cfg");
        if (File.Exists(path)) File.Delete(path);
        var file = new ConfigFile(path, false) { SaveOnConfigSet = false };
        var text = new HostConfigEntry<string>(file.Bind("Host", "Moons", "", ""));
        var number = new HostConfigEntry<float>(file.Bind("Host", "Drain", 0.0075f, ""));
        var mode = new HostConfigEntry<FillMode>(file.Bind("Host", "Fill", FillMode.DoorsClosed, ""));
        var enabled = new HostConfigEntry<bool>(file.Bind("Host", "Enabled", true, ""));
        var price = new HostConfigEntry<int>(file.Bind("Host", "Price", 70, ""));
        text.LocalValue = "Луна🌕@0.01; Quotes\" and \\slashes\nNewline";
        Assert((string)text.Parse(text.SerializeLocal()) == text.LocalValue, "Real TOML string escaping failed.");
        text.SetOverride("HostMoon@0.1");
        Assert((string)text.Parse(text.SerializeEffective()) == "HostMoon@0.1", "Effective value serialization failed.");
        Assert(text.LocalValue.Contains("Луна"), "Host override changed the local config.");
        var restore = text.CaptureOverride(); text.SetOverride("Other@2"); restore();
        Assert(text.Value == "HostMoon@0.1", "Override capture/restore failed.");
        text.ClearOverride();
        Assert(text.Value.Contains("Луна"), "Disconnect failed to restore the local value.");
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
            number.LocalValue = 0.125f;
            Assert((float)number.Parse(number.SerializeLocal()) == 0.125f, "Float TOML is culture-sensitive.");
        }
        finally { CultureInfo.CurrentCulture = culture; }
        Assert((FillMode)mode.Parse("Off") == FillMode.Off, "Real enum parsing failed.");
        Assert((bool)enabled.Parse(enabled.SerializeLocal()), "Real bool parsing failed.");
        Assert((int)price.Parse(price.SerializeLocal()) == 70, "Real integer parsing failed.");
        int changes = 0;
        price.Entry.SettingChanged += (_, _) => changes++;
        price.LocalValue = 500; price.RestoreLocal("70");
        Assert(price.Value == 70 && changes == 2, "Real ConfigEntry rollback/events failed.");
        file.Save();
        string original = File.ReadAllText(path);
        price.SetOverride(999); text.SetOverride("Remote@5");
        file.Save();
        Assert(File.ReadAllText(path) == original, "Received host overrides were persisted to disk.");
        var loaded = new ConfigFile(path, false) { SaveOnConfigSet = false };
        Assert(loaded.Bind("Host", "Moons", "", "").Value == text.LocalValue, "Config round-trip lost escaped/Unicode text.");
        Console.WriteLine($"Passed {checks} checks against real BepInEx configuration APIs.");
    }
}
