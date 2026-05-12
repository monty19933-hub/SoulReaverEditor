using System;
using System.Collections.Generic;

namespace SoulReaverEditor
{
    internal static class ObjectNamer
    {
        private static readonly Dictionary<string, string> Exact = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "campath", "Camera Path / Trigger Path" },
            { "marker", "Placement Marker / Script Marker" },
            { "raziel", "Raziel Loader Anchor / Player Start (unsafe to move)" },
            { "soul", "Soul Pickup" },
            { "portal", "Room Portal / Stream Boundary" },
            { "wportal", "Warp Portal / Gate Portal" },
            { "warpg", "Warp Gate" },
            { "wrpface", "Warp Gate Face" },
            { "wrpdoor", "Warp Gate Door" },
            { "sluagh", "Sluagh Enemy" },
            { "vwraith", "Vampire Wraith Enemy" },
            { "ronin", "Human / Ronin Enemy" },
            { "hunter", "Human Hunter Enemy" },
            { "skinner", "Skinner / Dumahim Enemy" },
            { "morlock", "Morlock Enemy" },
            { "aluka", "Rahabim Vampire Enemy" },
            { "eaggot", "Egg Creature / Hatchling" },
            { "eaggots", "Egg Creature Group" },
            { "eaggotr", "Egg Creature / Reaver Variant" },
            { "eaggotu", "Egg Creature / Upright Variant" },
            { "eggsac", "Egg Sac" },
            { "saggot", "Small Egg Creature" },
            { "saggots", "Small Egg Creature Group" },
            { "wcegg", "Wall-Clinging Egg" },
            { "eggfx", "Egg Effect" },
            { "splob", "Spectral Blob Effect (not regular soul pickup)" },
            { "soulb", "Soul Pickup Variant B" },
            { "spldum", "Spectral / Dumahim Effect" },
            { "spldus", "Spectral Dust Effect" },
            { "dust", "Dust Effect" },
            { "dusta", "Dust Effect Variant" },
            { "smist", "Spectral Mist" },
            { "updraft", "Updraft / Air Current" },
            { "healthu", "Health Upgrade" },
            { "hndtrch", "Hand Torch Weapon" },
            { "lstaff", "Large Staff Weapon" },
            { "cstaff", "Cathedral Staff Weapon" },
            { "hubstaff", "Clan Staff Weapon" },
            { "lukpoon", "Harpoon / Spear Weapon" },
            { "flameb", "Blue Flame / Flame Effect" },
            { "flamec", "Candle Flame / Flame Effect" },
            { "flamed", "Decorative Flame" },
            { "flamegs", "Glyph Statue Flame" },
            { "flameh", "Hanging Flame" },
            { "flameq", "Quest Flame" },
            { "flamesk", "Skull Flame" },
            { "flamesl", "Soul Flame" },
            { "flament", "Flame Emitter" },
            { "blaze", "Fire / Blaze Hazard" },
            { "gfire", "Glyph Fire" },
            { "gwater", "Glyph Water" },
            { "gforce", "Glyph Force Field" },
            { "fkefrce", "Fake Force Field" },
            { "sudoor", "Sunlight Door" },
            { "sugate", "Sunlight Gate" },
            { "sugatea", "Sunlight Gate Variant" },
            { "dblgate", "Double Gate" },
            { "srdoor", "Soul Reaver Door" },
            { "skdoor", "Skinner/Dumahim Door" },
            { "catdoor", "Cathedral Door" },
            { "oradoor", "Oracle Door" },
            { "ocdoorf", "Oracle Cave Door" },
            { "doorh", "Heavy Door" },
            { "hatch", "Hatch / Floor Door" },
            { "chpdrl", "Chapel Door Left" },
            { "chpdrr", "Chapel Door Right" },
            { "dbridge", "Drawbridge / Bridge" },
            { "pshblk", "Push Block" },
            { "pshblka", "Push Block Variant" },
            { "opshblk", "Open Push Block" },
            { "firblk", "Fire Block" },
            { "flamblk", "Flame Block" },
            { "nigblk", "Necropolis Block" },
            { "nbblk", "Necropolis Block Variant" },
            { "stnblk", "Stone Block" },
            { "fblock", "Falling / Floor Block" },
            { "frcblk", "Force Block" },
            { "oablk", "Oracle Block" },
            { "tubblka", "Tube Block" },
            { "wallcr", "Wall Crawler / Wall Crack" },
            { "walsoul", "Wall Soul" },
            { "walbosb", "Wall Boss Object" },
            { "walflap", "Wall Flap" },
            { "plswtch", "Pillar Switch" },
            { "lngswch", "Long Switch" },
            { "lever", "Lever Switch" },
            { "valve", "Valve" },
            { "crank", "Crank" },
            { "gcrank", "Glyph Crank" },
            { "mmcrank", "Mechanism Crank" },
            { "pweel", "Puzzle Wheel" },
            { "winder", "Winder Mechanism" },
            { "c_diala", "Chronoplast Dial A" },
            { "c_dialb", "Chronoplast Dial B" },
            { "c_dialc", "Chronoplast Dial C" },
            { "eye", "Eye Mechanism" },
            { "eyef", "Eye Mechanism Front" },
            { "eyeglo", "Glowing Eye" },
            { "orface", "Oracle Face" },
            { "ocpill", "Oracle Pillar" },
            { "lensrck", "Lens Rack" },
            { "outgear", "Outdoor Gear" },
            { "scfan", "Cathedral Fan" },
            { "scslbdr", "Cathedral Sliding Border" },
            { "schook", "Cathedral Hook" },
            { "bellsc", "Cathedral Bell" },
            { "sndptsc", "Sound Puzzle Point C" },
            { "sndptsd", "Sound Puzzle Point D" },
            { "catpipe", "Cathedral Pipe" },
            { "pipestk", "Pipe Stack" },
            { "ventsc", "Cathedral Vent" },
            { "venta", "Vent" },
            { "tubsca", "Tube / Cathedral Scenery A" },
            { "tubscb", "Tube / Cathedral Scenery B" },
            { "banner", "Banner" },
            { "banralu", "Rahabim Banner" },
            { "banrcty", "City Banner" },
            { "banrraz", "Raziel Banner" },
            { "banrwal", "Wall Banner" },
            { "flagcty", "City Flag" },
            { "flagron", "Human/Ronin Flag" },
            { "flagraz", "Raziel Clan Flag" },
            { "flagskn", "Dumahim/Skinner Flag" },
            { "flagwal", "Wall Flag" },
            { "flagalu", "Rahabim Flag" },
            { "flagall", "All Clans Flag" },
            { "flagkai", "Kain Flag" },
            { "stdcath", "Cathedral Standard" },
            { "stdskin", "Dumahim/Skinner Standard" },
            { "stdorac", "Oracle Standard" },
            { "stdtomb", "Tomb Standard" },
            { "stdaluk", "Rahabim Standard" },
            { "urnx", "Urn X" },
            { "urny", "Urn Y" },
            { "urnz", "Urn Z" },
            { "crow", "Crow / Bird" },
            { "raven", "Raven / Bird" },
            { "mound", "Dirt Mound" },
            { "prong", "Prong / Spike Object" },
            { "beam", "Beam" },
            { "obelisb", "Obelisk" },
            { "bkalph", "Block Alphabet / Puzzle Symbol" },
            { "plugz", "Plug Z" },
            { "dplug", "Drain Plug" },
            { "tranhst", "Transparent Host / Trigger Host" },
            { "hlamp", "Hanging Lamp" },
            { "rooflap", "Roof Lap / Roof Piece" },
            { "stnaff", "Stone Affordance / Stone Feature" },
            { "stnglsa", "Stone Glass A" },
            { "stnglsb", "Stone Glass B" },
            { "stnglsc", "Stone Glass C" },
            { "stnglsd", "Stone Glass D" },
            { "stnglse", "Stone Glass E" },
            { "stnglsf", "Stone Glass F" },
            { "stnglsg", "Stone Glass G" },
            { "stnglsh", "Stone Glass H" },
            { "comblst", "Combat List / Combat Trigger" },
            { "comcath", "Cathedral Combat Trigger" },
            { "boldasc", "Decorative Boulder / Debris C" },
            { "boldasd", "Decorative Boulder / Debris D" },
            { "boldasf", "Decorative Boulder / Debris F" },
            { "expldc", "Explosion / Debris Cloud" },
            { "ccover", "Cover / Collision Cover" },
            { "dvillhb", "Villager / Human Body B" },
            { "dvillhc", "Villager / Human Body C" },
            { "vlgra", "Villager A" },
            { "vlgrb", "Villager B" },
            { "dron", "Drone Enemy" },
            { "dronb", "Drone Enemy B" },
            { "wrshp", "Worshipper / Worship Object" },
            { "glfhost", "Glyph Host / Glyph Target" },
            { "boshost", "Boss Host / Boss Trigger" }
        };

        private static readonly PrefixName[] Prefixes =
        {
            new PrefixName("cam", "Camera / Cutscene Control"),
            new PrefixName("flag", "Flag / Cloth Prop"),
            new PrefixName("banr", "Banner / Cloth Prop"),
            new PrefixName("flame", "Flame / Light Effect"),
            new PrefixName("std", "Standard / Hanging Banner"),
            new PrefixName("door", "Door"),
            new PrefixName("gate", "Gate"),
            new PrefixName("sugate", "Sunlight Gate"),
            new PrefixName("psh", "Pushable Puzzle Object"),
            new PrefixName("blk", "Block / Puzzle Block"),
            new PrefixName("urn", "Breakable Urn / Prop"),
            new PrefixName("snd", "Sound Puzzle Object"),
            new PrefixName("cat", "Cathedral Mechanism / Prop"),
            new PrefixName("cathy", "Cathedral Object"),
            new PrefixName("oc", "Oracle Cave Object"),
            new PrefixName("or", "Oracle Object"),
            new PrefixName("al", "Rahabim / Drowned Abbey Object"),
            new PrefixName("wal", "Wall Object"),
            new PrefixName("spl", "Spectral Effect"),
            new PrefixName("dust", "Dust Effect"),
            new PrefixName("egg", "Egg / Hatchling Object"),
            new PrefixName("eagg", "Egg Creature"),
            new PrefixName("sagg", "Small Egg Creature"),
            new PrefixName("tub", "Tube / Pipe Mechanism"),
            new PrefixName("vent", "Vent"),
            new PrefixName("stn", "Stone Object"),
            new PrefixName("glyph", "Glyph Object"),
            new PrefixName("g", "Glyph / Elemental Object")
        };

        public static string Normalize(string code)
        {
            if (string.IsNullOrEmpty(code)) return "";
            int zero = code.IndexOf('\0');
            if (zero >= 0) code = code.Substring(0, zero);
            return code.Trim().TrimEnd('_').ToLowerInvariant();
        }

        public static string FriendlyName(string code)
        {
            string clean = Normalize(code);
            if (string.IsNullOrEmpty(clean)) return "(unnamed object)";

            string exact;
            if (Exact.TryGetValue(clean, out exact)) return exact;

            foreach (PrefixName prefix in Prefixes)
            {
                if (clean.StartsWith(prefix.Prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return prefix.Name;
                }
            }

            return "Unmapped Object";
        }

        public static string DisplayName(string code)
        {
            string clean = Normalize(code);
            if (string.IsNullOrEmpty(clean)) return "(unnamed object)";
            return FriendlyName(clean) + " - " + clean;
        }

        public static string PlacementNote(string code)
        {
            string clean = Normalize(code);
            if (clean == "splob")
            {
                return "splob is a spectral blob/effect object. It is not the normal soul pickup; large moves can upset streaming or effect setup.";
            }
            if (clean == "raziel")
            {
                return "Source research shows the Underworld loader searches the loaded room for the 'raziel' intro, marks it used, then relocates the existing player instance to that intro before connected rooms finish preloading. Moving only this anchor can desync startup cameras, portals, and stream state, so keep it fixed until those companion records are mapped.";
            }
            if (clean == "portal" || clean == "wportal" || clean.StartsWith("cam", StringComparison.OrdinalIgnoreCase))
            {
                return "This is a stream/camera/control object. Move it only in tiny test steps until the companion data is fully mapped.";
            }
            return null;
        }

        private sealed class PrefixName
        {
            public readonly string Prefix;
            public readonly string Name;

            public PrefixName(string prefix, string name)
            {
                Prefix = prefix;
                Name = name;
            }
        }
    }
}
