using BepInEx.Configuration;
using System;

namespace Oxygen.Configuration
{
    internal interface IHostConfigEntry
    {
        string Key { get; }
        string SerializeLocal();
        string SerializeEffective();
        void RestoreLocal(string value);
        object Parse(string value);
        void SetOverride(object value);
        void ClearOverride();
        void UseDefault();
        Action CaptureOverride();
    }

    // The override is held in memory; receiving a host value never writes the local file.
    internal sealed class HostConfigEntry<T> : IHostConfigEntry
    {
        internal ConfigEntry<T> Entry { get; }
        private bool overridden;
        private T hostValue;
        internal T Value => overridden ? hostValue : Entry.Value;
        internal T LocalValue { get => Entry.Value; set => Entry.Value = value; }
        public string Key => Entry.Definition.Section + "\n" + Entry.Definition.Key;

        internal HostConfigEntry(ConfigEntry<T> entry) { Entry = entry; }
        public string SerializeLocal() => Entry.GetSerializedValue();
        public string SerializeEffective() => TomlTypeConverter.ConvertToString(Value, typeof(T));
        public void RestoreLocal(string value) => Entry.Value = (T)Parse(value);
        public object Parse(string value)
        {
            var parsed = (T)TomlTypeConverter.ConvertToValue(value, typeof(T));
            if (typeof(T).IsEnum && !Enum.IsDefined(typeof(T), parsed))
                throw new FormatException($"Invalid enum value for {Key}.");
            if (parsed is float number && (float.IsNaN(number) || float.IsInfinity(number)))
                throw new FormatException($"Invalid number for {Key}.");
            if (Entry.Description.AcceptableValues != null && !Entry.Description.AcceptableValues.IsValid(parsed))
                throw new FormatException($"Value outside allowed range for {Key}.");
            return parsed;
        }
        public void SetOverride(object value) { hostValue = (T)value; overridden = true; }
        public void ClearOverride() { overridden = false; hostValue = default; }
        public void UseDefault() => SetOverride(Entry.DefaultValue);
        public Action CaptureOverride()
        {
            bool previousOverridden = overridden;
            T previousValue = hostValue;
            return () => { overridden = previousOverridden; hostValue = previousValue; };
        }
    }
}
