// Default name lists per species for the name-list tool: original names, ASCII only, given names one word each.
using System.Collections.Generic;
using WanderersGuild;

namespace Editor
{
    // One species' defaults (the asset it becomes and the species it's assigned to).
    public sealed class SpeciesNameDefaults
    {
        public string SpeciesId { get; }
        public string AssetName { get; }
        private readonly string[] _given, _family, _epithets;
        private readonly float _familyChance, _epithetChance;

        public SpeciesNameDefaults(string speciesId, string assetName, float familyChance, float epithetChance,
                                   string[] given, string[] family, string[] epithets)
        {
            SpeciesId = speciesId;
            AssetName = assetName;
            _familyChance = familyChance;
            _epithetChance = epithetChance;
            _given = given;
            _family = family;
            _epithets = epithets;
        }

        public void ApplyTo(AdventurerNameList list) => list.Fill(_given, _family, _familyChance, _epithets, _epithetChance);
    }

    public static class DefaultAdventurerNames
    {
        // NOTE: Built by methods, not static fields — an array of static fields declared below it would hold nulls
        // (static initializers run in textual order).
        public static IReadOnlyList<SpeciesNameDefaults> All => new[] { Human(), Elf(), Dwarf() };

        private static SpeciesNameDefaults Human() => new("human_species", "HumanNames", 0.45f, 0.15f,
            new[]
            {
                "Aldric", "Bran", "Mira", "Tobin", "Elsa", "Garrick", "Lysa", "Corwin", "Nell", "Edric", "Rowan", "Hale",
                "Bess", "Cedric", "Dara", "Ewan", "Fenna", "Gideon", "Hilde", "Isolde", "Jory", "Kate", "Leofric", "Maren",
                "Nolan", "Osric", "Pella", "Quinn", "Rhea", "Silas", "Tamsin", "Ulric", "Vera", "Wren", "Alden", "Cora",
                "Dunstan", "Eda", "Finn", "Greta", "Hugh", "Ivy", "Joss", "Lena", "Merrick", "Odette", "Piers", "Sabine",
                "Willa"
            },
            new[]
            {
                "Ashford", "Blackwood", "Carver", "Dunmore", "Fairbanks", "Fletcher", "Greaves", "Hawthorne", "Holloway",
                "Lockwood", "Marsh", "Oakley", "Pembrook", "Redfern", "Rook", "Shaw", "Thatcher", "Tanner", "Vale",
                "Whitlock", "Wolfe", "Yarrow", "Barrow", "Cobb", "Dale", "Elwood", "Fenwick", "Gale", "Hart", "Mallory",
                "Stroud", "Penhallow"
            },
            new[]
            {
                "the Bold", "the Quiet", "Ironhand", "the Lucky", "Swiftfoot", "the Fair", "the Brave", "Oakheart",
                "the Red", "the Young", "Halfpenny", "the Patient"
            });

        private static SpeciesNameDefaults Elf() => new("elf_species", "ElfNames", 0.5f, 0.15f,
            new[]
            {
                "Aelith", "Aeris", "Caelan", "Elowen", "Faelar", "Ilyra", "Nimue", "Thalia", "Ysolde", "Aranel", "Calenith",
                "Eryndor", "Faenya", "Ilanis", "Myrael", "Naeris", "Quillon", "Saelis", "Tirien", "Varis", "Yllana",
                "Zephyr", "Daelis", "Elaria", "Finrel", "Iselle", "Kaelen", "Liora", "Miravel", "Nyssa", "Orinel", "Sariel",
                "Talindra", "Velanor", "Ithiel", "Aelinor", "Cyrel", "Elandra", "Ilvaris", "Seren", "Taelor", "Vaena",
                "Ellisar", "Mirin", "Nalia"
            },
            new[]
            {
                "Moonsong", "Dawnleaf", "Starbloom", "Leafsong", "Mistvale", "Sunshade", "Thornwood", "Dewglade",
                "Nightbloom", "Willowmere", "Ashgrove", "Starfall", "Fernshade", "Lightbough", "Skyreach", "Briarwind",
                "Galeleaf", "Glenmoor", "Duskmere", "Rainsong", "Wildbloom", "Sablewing", "Larkspur", "Silverfen",
                "Brightvale", "Amberlight", "Riverlight", "Swiftbrook"
            },
            new[]
            {
                "the Fair", "the Silent", "Swiftarrow", "the Wise", "Dawnborn", "the Elder", "Starsight", "the Gentle",
                "Mistwalker", "the Wistful"
            });

        private static SpeciesNameDefaults Dwarf() => new("dwarf_species", "DwarfNames", 0.6f, 0.15f,
            new[]
            {
                "Barend", "Bruna", "Dagny", "Durgan", "Edda", "Gorrim", "Grenna", "Hakon", "Helga", "Ivra", "Jarl", "Kelda",
                "Korvak", "Lodrik", "Magda", "Morna", "Norvik", "Orrin", "Ragna", "Rulf", "Sabra", "Sten", "Thora",
                "Torvald", "Ulla", "Varn", "Vigdis", "Yorik", "Brannoc", "Dorna", "Hjalma", "Kragg", "Mardun", "Tova",
                "Ulrik", "Grimma", "Baldra", "Hrolf", "Odda", "Brokk", "Agna", "Feldra"
            },
            new[]
            {
                "Ironfoot", "Stonebrow", "Copperkin", "Deepdelve", "Anvilborn", "Hammerson", "Flintbeard", "Goldvein",
                "Coalbrand", "Emberhelm", "Rockjaw", "Oreheart", "Gravelmane", "Steelgrip", "Bronzefist", "Ashforge",
                "Cragborn", "Tinbelly", "Runebeard", "Forgeson", "Slagborn", "Ironbrow", "Blackanvil", "Pebblefoot",
                "Deepmine", "Granitejaw"
            },
            new[]
            {
                "the Stout", "Beardless", "the Grim", "Alebelly", "Rockfist", "the Deep", "Oathkeeper", "the Loud",
                "Twice-Forged", "Stonehand", "the Gruff", "the Unbroken"
            });
    }
}