using System;

namespace SoulReaverEditor
{
    internal static class ZoneNamer
    {
        private static readonly PrefixName[] Prefixes =
        {
            new PrefixName("cityout", "Ruined City / Dumahim"),
            new PrefixName("clfsubb", "Sanctuary / Overland"),
            new PrefixName("clfpil", "Sanctuary / Pillars"),
            new PrefixName("conectg", "Warp Gate / Connectors"),
            new PrefixName("conect", "World Connector"),
            new PrefixName("pistair", "Sunlight Glyph Altar"),
            new PrefixName("pisaira", "Sunlight Glyph Altar"),
            new PrefixName("pisairb", "Sunlight Glyph Altar"),
            new PrefixName("pistunl", "Sunlight Glyph Altar"),
            new PrefixName("piston", "Sunlight Glyph Altar"),
            new PrefixName("suntnlb", "Sunlight Glyph Altar"),
            new PrefixName("suntnl", "Sunlight Glyph Altar"),
            new PrefixName("intvaly", "Sunlight Glyph Altar"),
            new PrefixName("litrnda", "Sunlight Glyph Altar"),
            new PrefixName("litrndb", "Sunlight Glyph Altar"),
            new PrefixName("litetun", "Sunlight Glyph Altar"),
            new PrefixName("litein", "Sunlight Glyph Altar"),
            new PrefixName("sunrm", "Sunlight Glyph Altar"),
            new PrefixName("cathy", "Silenced Cathedral"),
            new PrefixName("tower", "Silenced Cathedral"),
            new PrefixName("train", "Silenced Cathedral / Train Route"),
            new PrefixName("soundg", "Sound Glyph Altar"),
            new PrefixName("aluka", "Drowned Abbey / Rahabim"),
            new PrefixName("nighta", "Necropolis / Melchiah"),
            new PrefixName("nightb", "Necropolis / Melchiah"),
            new PrefixName("mrlock", "Necropolis / Melchiah"),
            new PrefixName("tompil", "Tomb of the Sarafan"),
            new PrefixName("tomb", "Tomb of the Sarafan"),
            new PrefixName("add", "Tomb / Abbey Connector"),
            new PrefixName("oracle", "Oracle's Cave"),
            new PrefixName("chrono", "Chronoplast"),
            new PrefixName("skinnr", "Ruined City / Dumahim"),
            new PrefixName("city", "Ruined City / Dumahim"),
            new PrefixName("under", "Underworld"),
            new PrefixName("huba", "Sanctuary of the Clans"),
            new PrefixName("hubb", "Sanctuary of the Clans"),
            new PrefixName("out", "Overland / Lake of the Dead"),
            new PrefixName("cliff", "Overland / Lake of the Dead"),
            new PrefixName("pillar", "Pillars of Nosgoth"),
            new PrefixName("pillars", "Pillars of Nosgoth"),
            new PrefixName("stone", "Stone Glyph Altar"),
            new PrefixName("fire", "Fire Glyph / Fire Forge"),
            new PrefixName("chapel", "Sanctuary / Chapel"),
            new PrefixName("bonus", "Bonus / Test Room"),
            new PrefixName("fill", "Connector / Filler Room"),
            new PrefixName("gasrm", "Glyph / Gas Room"),
            new PrefixName("gastuna", "Glyph / Gas Tunnel"),
            new PrefixName("gastunb", "Glyph / Gas Tunnel"),
            new PrefixName("filtuna", "Glyph / Filter Tunnel"),
            new PrefixName("filtunb", "Glyph / Filter Tunnel"),
            new PrefixName("htorm", "Glyph / Optional Room"),
            new PrefixName("htotun", "Glyph / Optional Tunnel"),
            new PrefixName("boss", "Boss / Story Room")
        };

        public static string NormalizeRoomName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            string value = raw.Trim().ToLowerInvariant();
            int comma = value.IndexOf(',');
            if (comma >= 0) value = value.Substring(0, comma);
            int zero = value.IndexOf('\0');
            if (zero >= 0) value = value.Substring(0, zero);
            return value.Trim();
        }

        public static string FriendlyZone(string roomName)
        {
            string name = NormalizeRoomName(roomName);
            foreach (PrefixName prefix in Prefixes)
            {
                if (name.StartsWith(prefix.Prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return prefix.Zone;
                }
            }
            return "Unknown / Unmapped";
        }

        public static string DisplayName(string roomName)
        {
            string clean = NormalizeRoomName(roomName);
            if (string.IsNullOrEmpty(clean)) return "(unnamed)";
            return FriendlyZone(clean) + " - " + clean;
        }

        public static string PortalSuffix(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            string value = raw.Trim().ToLowerInvariant();
            int comma = value.IndexOf(',');
            if (comma < 0 || comma + 1 >= value.Length) return "";
            return value.Substring(comma + 1).Trim();
        }

        public static string DisplayPortalTarget(string raw)
        {
            string room = NormalizeRoomName(raw);
            string display = DisplayName(room);
            string suffix = PortalSuffix(raw);
            if (!string.IsNullOrEmpty(suffix))
            {
                display += " (target " + suffix + ")";
            }
            return display;
        }

        private sealed class PrefixName
        {
            public readonly string Prefix;
            public readonly string Zone;

            public PrefixName(string prefix, string zone)
            {
                Prefix = prefix;
                Zone = zone;
            }
        }
    }
}
