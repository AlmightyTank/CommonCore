using SPTarkov.Server.Core.Models.Common;

namespace CommonCore.Constants;

public static class ItemMaps
{
    public static readonly Dictionary<string, MongoId> ItemBaseClassMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["AMMO"] = new("5485a8684bdc2da71d8b4567"),
            ["AMMO_CONTAINER"] = new("543be5cb4bdc2deb348b4568"),
            ["ARMORED_EQUIPMENT"] = new("57bef4c42459772e8d35a53b"),
            ["ARMBAND"] = new("5b3f15d486f77432d0509248"),
            ["ARMOR"] = new("5448e54d4bdc2dcc718b4568"),
            ["ARMORPLATE"] = new("644120aa86ffbe10ee032b6f"),
            ["ASSAULT_CARBINE"] = new("5447b5fc4bdc2d87278b4567"),
            ["ASSAULT_RIFLE"] = new("5447b5f14bdc2d61278b4567"),
            ["ASSAULT_SCOPE"] = new("55818add4bdc2d5b648b456f"),
            ["BACKPACK"] = new("5448e53e4bdc2d60728b4567"),
            ["BARREL"] = new("555ef6e44bdc2de9068b457e"),
            ["BATTERY"] = new("57864ee62459775490116fc1"),
            ["BIPOD"] = new("55818afb4bdc2dde698b456d"),
            ["BUILDING_MATERIAL"] = new("57864ada245977548638de91"),
            ["CHARGING_HANDLE"] = new("55818a6f4bdc2db9688b456b"),
            ["CHEST_RIG"] = new("5448e5284bdc2dcb718b4567"),
            ["COMMON_CONTAINER"] = new("5795f317245977243854e041"),
            ["COMPACT_REFLEX_SIGHT"] = new("55818acf4bdc2dde698b456b"),
            ["COMPASS"] = new("5f4fbaaca5573a5ac31db429"),
            ["DRINK"] = new("5448e8d64bdc2dce718b4568"),
            ["DRUG"] = new("5448f3a14bdc2d27728b4569"),
            ["ELECTRONICS"] = new("57864a66245977548f04a81f"),
            ["FACECOVER"] = new("5a341c4686f77469e155819e"),
            ["FLASHLIGHT"] = new("55818b084bdc2d5b648b4571"),
            ["FLASHHIDER"] = new("550aa4bf4bdc2dd6348b456b"),
            ["FOOD"] = new("5448e8d04bdc2ddf718b4569"),
            ["FOREGRIP"] = new("55818af64bdc2d5b648b4570"),
            ["FUEL"] = new("5d650c3e815116009f6201d2"),
            ["GAS_BLOCK"] = new("56ea9461d2720b67698b456f"),
            ["GRENADE_LAUNCHER"] = new("5447bedf4bdc2d87278b4568"),
            ["HANDGUN"] = new("5447b5cf4bdc2d65278b4567"),
            ["HANDGUARD"] = new("55818a104bdc2db9688b4569"),
            ["HEADPHONES"] = new("5645bcb74bdc2ded0b8b4578"),
            ["HEADWEAR"] = new("5a341c4086f77401f2541505"),
            ["INFO"] = new("5448ecbe4bdc2d60728b4568"),
            ["INVENTORY"] = new("55d720f24bdc2d88028b456d"),
            ["IRON_SIGHT"] = new("55818ac54bdc2d5b648b456e"),
            ["KEYCARD"] = new("5c164d2286f774194c5e69fa"),
            ["KEYMECHANICAL"] = new("5c99f98d86f7745c314214b3"),
            ["KEY_CARD"] = new("5c164d2286f774194c5e69fa"),
            ["KNIFE"] = new("5447e1d04bdc2dff2f8b4567"),
            ["LOCKING_CONTAINER"] = new("5671435f4bdc2d96058b4569"),
            ["LOOT_CONTAINER"] = new("566965d44bdc2d814c8b4571"),
            ["LUBRICANT"] = new("57864e4c24597754843f8723"),
            ["MACHINEGUN"] = new("5447bed64bdc2d97278b4568"),
            ["MAGAZINE"] = new("5448bc234bdc2d3c308b4569"),
            ["MAP"] = new("567849dd4bdc2d150f8b456e"),
            ["MARKSMAN_RIFLE"] = new("5447b6194bdc2d67278b4567"),
            ["MEDICAL_ITEM"] = new("5448f3ac4bdc2dce718b4569"),
            ["MEDICAL_SUPPLIES"] = new("57864c8c245977548867e7f1"),
            ["MEDITKIT"] = new("5448f39d4bdc2d0a728b4568"),
            ["MONEY"] = new("543be5dd4bdc2deb348b4569"),
            ["MUZZLECOMBO"] = new("550aa4dd4bdc2dc9348b4569"),
            ["MOUNT"] = new("55818b224bdc2dde698b456f"),
            ["NIGHTVISION"] = new("5a2c3a9486f774688b05e574"),
            ["OTHER"] = new("590c745b86f7743cc433c5f2"),
            ["PISTOLGRIP"] = new("55818a684bdc2ddd698b456d"),
            ["POCKETS"] = new("557596e64bdc2dc2118b4571"),
            ["PORTABLE_RANGEFINDER"] = new("61605ddea09d851a0a0c1bbc"),
            ["RANDOMLOOTCONTAINER"] = new("62f109593b54472778797866"),
            ["RECEIVER"] = new("55818a304bdc2db5418b457d"),
            ["REFLEX_SIGHT"] = new("55818ad54bdc2ddc698b4569"),
            ["REPAIRKITS"] = new("616eb7aea207f41933308f46"),
            ["SCOPE"] = new("55818ae44bdc2dde698b456c"),
            ["SHOTGUN"] = new("5447b6094bdc2dc3278b4567"),
            ["SILENCER"] = new("550aa4cd4bdc2dd8348b456c"),
            ["SNIPER_RIFLE"] = new("5447b6254bdc2dc3278b4568"),
            ["SPECIAL_ITEM"] = new("5447e0e74bdc2d3c308b4567"),
            ["STASH"] = new("566abbb64bdc2d144c8b457d"),
            ["STATIONARY_CONT."] = new("567583764bdc2d98058b456e"),
            ["STIMULANT"] = new("5448f3a64bdc2d60728b456a"),
            ["STOCK"] = new("55818a594bdc2db9688b456a"),
            ["THROWABLE_WEAPON"] = new("543be6564bdc2df4348b4568"),
            ["THERMALVISION"] = new("5d21f59b6dbe99052b54ef83"),
            ["TOOL"] = new("57864bb7245977548b3b66c2"),
            ["UBGL"] = new("55818b014bdc2ddc698b456b"),
            ["VIS_OBSERV_DEVICE"] = new("5448e5724bdc2ddf718b4568")
        };

    public static readonly Dictionary<string, MongoId> ItemHandbookCategoryMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["AMMO"] = new("5b47574386f77428ca22b346"),
            ["AMMO_BOXES"] = new("5b47574386f77428ca22b33c"),
            ["AMMO_ROUNDS"] = new("5b47574386f77428ca22b33b"),
            ["BARTER"] = new("5b47574386f77428ca22b33e"),
            ["BARTER_BUILDING"] = new("5b47574386f77428ca22b2ee"),
            ["BARTER_ELECTRONICS"] = new("5b47574386f77428ca22b2ef"),
            ["BARTER_ENERGY"] = new("5b47574386f77428ca22b2ed"),
            ["BARTER_FLAMMABLE"] = new("5b47574386f77428ca22b2f2"),
            ["BARTER_HOUSEHOLD"] = new("5b47574386f77428ca22b2f0"),
            ["BARTER_MEDICAL"] = new("5b47574386f77428ca22b2f3"),
            ["BARTER_OTHERS"] = new("5b47574386f77428ca22b2f4"),
            ["BARTER_TOOLS"] = new("5b47574386f77428ca22b2f6"),
            ["BARTER_VALUABLES"] = new("5b47574386f77428ca22b2f1"),
            ["GEAR"] = new("5b47574386f77428ca22b33f"),
            ["GEAR_ARMOR"] = new("5b5f701386f774093f2ecf0f"),
            ["GEAR_BACKPACKS"] = new("5b5f6f6c86f774093f2ecf0b"),
            ["GEAR_CASES"] = new("5b5f6fa186f77409407a7eb7"),
            ["GEAR_COMPONENTS"] = new("5b5f704686f77447ec5d76d7"),
            ["GEAR_FACECOVERS"] = new("5b47574386f77428ca22b32f"),
            ["GEAR_HEADSETS"] = new("5b5f6f3c86f774094242ef87"),
            ["GEAR_HEADWEAR"] = new("5b47574386f77428ca22b330"),
            ["GEAR_RIGS"] = new("5b5f6f8786f77447ed563642"),
            ["GEAR_SECURED"] = new("5b5f6fd286f774093f2ecf0d"),
            ["GEAR_VISORS"] = new("5b47574386f77428ca22b331"),
            ["INFO"] = new("5b47574386f77428ca22b341"),
            ["KEYS"] = new("5b47574386f77428ca22b342"),
            ["KEYS_ELECTRONIC"] = new("5c518ed586f774119a772aee"),
            ["KEYS_MECHANIC"] = new("5c518ec986f7743b68682ce2"),
            ["MAPS"] = new("5b47574386f77428ca22b343"),
            ["MEDICAL"] = new("5b47574386f77428ca22b344"),
            ["MEDICAL_INJECTORS"] = new("5b47574386f77428ca22b33a"),
            ["MEDICAL_INJURY"] = new("5b47574386f77428ca22b339"),
            ["MEDICAL_MEDKITS"] = new("5b47574386f77428ca22b338"),
            ["MEDICAL_PILLS"] = new("5b47574386f77428ca22b337"),
            ["MODS"] = new("5b5f71a686f77447ed5636ab"),
            ["MODS_FUNCTIONAL"] = new("5b5f71b386f774093f2ecf11"),
            ["MODS_GEAR"] = new("5b5f750686f774093e6cb503"),
            ["MODS_VITAL"] = new("5b5f75b986f77447ec5d7710"),
            ["MOD_ASSAULT_SCOPE"] = new("5b5f740a86f77447ec5d7706"),
            ["MOD_AUX"] = new("5b5f74cc86f77447ec5d770a"),
            ["MOD_BARREL"] = new("5b5f75c686f774094242f19f"),
            ["MOD_BIPOD"] = new("5b5f71c186f77409407a7ec0"),
            ["MOD_CHARGE"] = new("5b5f751486f77447ec5d770c"),
            ["MOD_FLASHHIDER"] = new("5b5f724c86f774093f2ecf15"),
            ["MOD_FOREGRIP"] = new("5b5f71de86f774093f2ecf13"),
            ["MOD_GASBLOCK"] = new("5b5f760586f774093e6cb509"),
            ["MOD_HANDGUARD"] = new("5b5f75e486f77447ec5d7712"),
            ["MOD_IRON_SIGHT"] = new("5b5f746686f77447ec5d7708"),
            ["MOD_LAUNCHER"] = new("5b5f752e86f774093e6cb505"),
            ["MOD_LIGHTLASER"] = new("5b5f736886f774094242f193"),
            ["MOD_MAGAZINE"] = new("5b5f754a86f774094242f19b"),
            ["MOD_MOUNT"] = new("5b5f755f86f77447ec5d770e"),
            ["MOD_MICRO_DOT"] = new("5b5f744786f774094242f197"),
            ["MOD_MUZZLE"] = new("5b5f724186f77447ed5636ad"),
            ["MOD_OPTIC"] = new("5b5f748386f774093e6cb501"),
            ["MOD_PISTOLGRIP"] = new("5b5f761f86f774094242f1a1"),
            ["MOD_RECEIVER"] = new("5b5f764186f77447ec5d7714"),
            ["MOD_SIGHT"] = new("5b5f73ec86f774093e6cb4fd"),
            ["MOD_STOCK"] = new("5b5f757486f774093e6cb507"),
            ["MOD_SUPPRESSOR"] = new("5b5f731a86f774093e6cb4f9"),
            ["MONEY"] = new("5b5f78b786f77447ed5636af"),
            ["PROVISIONS"] = new("5b47574386f77428ca22b340"),
            ["PROVISIONS_DRINKS"] = new("5b47574386f77428ca22b335"),
            ["PROVISIONS_FOOD"] = new("5b47574386f77428ca22b336"),
            ["QUEST"] = new("5b619f1a86f77450a702a6f3"),
            ["SPEC"] = new("5b47574386f77428ca22b345"),
            ["WEAPONS"] = new("5b5f78dc86f77409407a7f8e"),
            ["WEAPONS_ASSAULTRIFLES"] = new("5b5f78fc86f77409407a7f90"),
            ["WEAPONS_BOLTACTION"] = new("5b5f798886f77447ed5636b5"),
            ["WEAPONS_CARBINES"] = new("5b5f78e986f77447ed5636b1"),
            ["WEAPONS_DMR"] = new("5b5f791486f774093f2ed3be"),
            ["WEAPONS_GL"] = new("5b5f79d186f774093f2ed3c2"),
            ["WEAPONS_MG"] = new("5b5f79a486f77409407a7f94"),
            ["WEAPONS_MELEE"] = new("5b5f7a0886f77409407a7f96"),
            ["WEAPONS_PISTOLS"] = new("5b5f792486f77447ed5636b3"),
            ["WEAPONS_SHOTGUNS"] = new("5b5f794b86f77409407a7f92"),
            ["WEAPONS_SMG"] = new("5b5f796a86f774093f2ed3c0"),
            ["WEAPONS_SPECIAL"] = new("5b5f79eb86f77447ed5636b7"),
            ["WEAPONS_THROW"] = new("5b5f7a2386f774093f2ed3c4")
        };

    public static readonly Dictionary<string, MongoId> DefaultCaliberAmmo =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Caliber762x25TT"] = new("573603562459776430731618"),
            ["Caliber9x18PM"] = new("57372140245977611f70ee91"),
            ["Caliber9x19PARA"] = new("5efb0da7a29a85116f6ea05f"),
            ["Caliber9x21"] = new("6576f4708ca9c4381d16cd9d"),
            ["Caliber9x33R"] = new("62330c40bdd19b369e1e53d1"),
            ["Caliber1143x23ACP"] = new("5efb0cabfb3e451d70735af5"),
            ["Caliber127x33"] = new("66a0d1c87d0d369e270bb9de"),
            ["Caliber46x30"] = new("5ba26835d4351e0035628ff5"),
            ["Caliber57x28"] = new("5cc86840d7f00c002412c56c"),
            ["Caliber545x39"] = new("61962b617c6c7b169525f168"),
            ["Caliber556x45NATO"] = new("59e690b686f7746c9f75e848"),
            ["Caliber68x51"] = new("6529243824cbe3c74a05e5c1"),
            ["Caliber762x35"] = new("5fbe3ffdf8b6a877a729ea82"),
            ["Caliber762x39"] = new("59e0d99486f7744a32234762"),
            ["Caliber762x51"] = new("5efb0c1bd79ff02a1f5e68d9"),
            ["Caliber762x54R"] = new("560d61e84bdc2da74d8b4571"),
            ["Caliber9x39"] = new("57a0e5022459774d1673f889"),
            ["Caliber366TKM"] = new("59e655cb86f77411dc52a77b"),
            ["Caliber127x55"] = new("5cadf6ddae9215051e1c23b2"),
            ["Caliber127x108"] = new("5cde8864d7f00c0010373be1"),
            ["Caliber12g"] = new("5c0d591486f7744c505b416f"),
            ["Caliber20g"] = new("5d6e6a5fa4b93614ec501745"),
            ["Caliber23x75"] = new("5f647f31b6238e5dd066e196"),
            ["Caliber40x46"] = new("5ede474b0c226a66f5402622")
        };

    public static readonly Dictionary<string, string> AllBotTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ARENAFIGHTER"] = "arenafighter",
            ["ARENAFIGHTEREVENT"] = "arenafighterevent",
            ["ASSAULT"] = "assault",
            ["ASSAULTGROUP"] = "assaultgroup",
            ["MARKSMAN"] = "marksman",
            ["CRAZYASSAULTEVENT"] = "crazyassaultevent",
            ["CURSEDASSAULT"] = "cursedassault",
            ["BEAR"] = "bear",
            ["USEC"] = "usec",
            ["PMCBEAR"] = "pmcbear",
            ["PMCUSEC"] = "pmcusec",
            ["PMC"] = "pmcbot",
            ["EXUSEC"] = "exusec",
            ["CULTISTPRIEST"] = "sectantpriest",
            ["CULTISTWARRIOR"] = "sectantwarrior",
            ["CULTISTONI"] = "sectantoni",
            ["CULTISTPRIESTEVENT"] = "sectantpriestevent",
            ["CULTISTPREDVESTNIK"] = "sectantpredvestnik",
            ["CULTISTPRIZRAK"] = "sectantprizrak",
            ["BTR"] = "btrshooter",
            ["SPIRITSPRING"] = "spiritspring",
            ["SPIRITWINTER"] = "spiritwinter",
            ["INFECTEDASSAULT"] = "infectedassault",
            ["INFECTEDCIVIL"] = "infectedcivil",
            ["INFECTEDLABORANT"] = "infectedlaborant",
            ["INFECTEDPMC"] = "infectedpmc",
            ["INFECTEDTAGILLA"] = "infectedtagilla",
            ["GIFTER"] = "gifter",
            ["KABAN"] = "bossboar",
            ["KABANSNIPER"] = "bossboarsniper",
            ["FOLLOWERBOAR"] = "followerboar",
            ["FOLLOWERBOARCLOSE1"] = "followerboarclose1",
            ["FOLLOWERBOARCLOSE2"] = "followerboarclose2",
            ["KILLA"] = "bosskilla",
            ["KOLONTAY"] = "bosskolontay",
            ["FOLLOWERKOLONTAYASSAULT"] = "followerkolontayassault",
            ["FOLLOWERKOLONTAYSECURITY"] = "followerkolontaysecurity",
            ["PARTISAN"] = "bosspartisan",
            ["RESHALA"] = "bossbully",
            ["FOLLOWERRESHALA"] = "followerbully",
            ["GLUHAR"] = "bossgluhar",
            ["FOLLOWERGLUHARASSAULT"] = "followergluharassault",
            ["FOLLOWERGLUHARSCOUT"] = "followergluharscout",
            ["FOLLOWERGLUHARSECURITY"] = "followergluharsecurity",
            ["FOLLOWERGLUHARSNIPER"] = "followergluharsnipe",
            ["KNIGHT"] = "bossknight",
            ["FOLLOWERBIGPIPE"] = "followerbigpipe",
            ["FOLLOWERBIRDEYE"] = "followerbirdeye",
            ["SHTURMAN"] = "bosskojaniy",
            ["FOLLOWERSHTURMAN"] = "followerkojaniy",
            ["SANITAR"] = "bosssanitar",
            ["FOLLOWERSANITAR"] = "followersanitar",
            ["TAGILLA"] = "bosstagilla",
            ["FOLLOWERTAGILLA"] = "followertagilla",
            ["ZRYACHIY"] = "bosszryachiy",
            ["FOLLOWERZRYACHIY"] = "followerzryachiy",
            ["PEACEFULZRYACHIYEVENT"] = "peacefulzryachiyevent",
            ["RAVANGEZRYACHIYEVENT"] = "ravengezryachiyevent",
            ["PEACEMAKER"] = "peacemaker",
            ["SKIER"] = "skier"
        };

    public static readonly Dictionary<string, MongoId> InventorySlots =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["FirstPrimaryWeapon"] = new("55d729c64bdc2d89028b4570"),
            ["SecondPrimaryWeapon"] = new("55d729d14bdc2d86028b456e"),
            ["Holster"] = new("55d729d84bdc2de3098b456b"),
            ["Scabbard"] = new("55d729e34bdc2d1b198b456d"),
            ["FaceCover"] = new("55d729e84bdc2d8a028b4569"),
            ["Headwear"] = new("55d729ef4bdc2d3a168b456c"),
            ["TacticalVest"] = new("55d729f74bdc2d87028b456e"),
            ["SecuredContainer"] = new("55d72a054bdc2d88028b456e"),
            ["Backpack"] = new("55d72a104bdc2d89028b4571"),
            ["ArmorVest"] = new("55d72a194bdc2d86028b456f"),
            ["Pockets"] = new("55d72a274bdc2de3098b456c"),
            ["Earpiece"] = new("5665b7164bdc2d144c8b4570"),
            ["Dogtag"] = new("59f0be1e86f77453be490939"),
            ["Eyewear"] = new("5a0ad9313f1241000e072755"),
            ["ArmBand"] = new("5b3f583786f77411d552fb2b")
        };

    public static readonly Dictionary<string, MongoId> Stashes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["LEVEL1"] = new("566abbc34bdc2d92178b4576"),
            ["LEVEL2"] = new("5811ce572459770cba1a34ea"),
            ["LEVEL3"] = new("5811ce662459770f6f490f32"),
            ["LEVEL4"] = new("5811ce772459770e9e5f9532")
        };

    public static readonly Dictionary<string, MongoId> TraderMap = 
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "mechanic", "5a7c2eca46aef81a7ca2145d" },
            { "skier", "58330581ace78e27b8b10cee" },
            { "peacekeeper", "5935c25fb3acc3127c3d8cd9" },
            { "therapist", "54cb57776803fa99248b456e" },
            { "prapor", "54cb50c76803fa8b248b4571" },
            { "jaeger", "5c0647fdd443bc2504c2d371" },
            { "ragman", "5ac3b934156ae10c4430e83c" },
            { "fence", "579dc571d53a0658a154fbec" },
            { "ref", "6617beeaa9cfa777ca915b7c"},
            { "tony", "698f904cd0fa772942d237c7" }
        };

    public static readonly Dictionary<string, MongoId> ContainerMap =
    new(StringComparer.OrdinalIgnoreCase)
    {
        // 🔴 Corpses
        ["dead_scav"] = new("5909e4b686f7747f5b744fa4"),
        ["corpse"] = new("5909e4b686f7747f5b744fa4"),
        ["body"] = new("5909e4b686f7747f5b744fa4"),

        // 🧥 Jackets
        ["jacket"] = new("59387ac686f77401442ddd61"),
        ["jackets"] = new("59387ac686f77401442ddd61"),

        // 🎒 Bags
        ["duffle"] = new("578f87b2245977356274f2cc"),
        ["duffle_bag"] = new("578f87b2245977356274f2cc"),
        ["bag"] = new("578f87b2245977356274f2cc"),

        // 🔫 Weapon boxes
        ["weapon_box"] = new("578f87ad245977356274f2cc"),
        ["weapon"] = new("578f87ad245977356274f2cc"),

        // 💣 Grenade boxes
        ["grenade_box"] = new("578f87b7245977356274f2cd"),
        ["grenades"] = new("578f87b7245977356274f2cd"),

        // 🔫 Ammo crates
        ["ammo"] = new("578f87a3245977356274f2cb"),
        ["ammo_crate"] = new("578f87a3245977356274f2cb"),

        // 🔐 Safes
        ["safe"] = new("578f8778245977358849a9b5"),

        // 🧰 Technical crates
        ["technical"] = new("578f87c2245977357b2c3f6f"),
        ["toolbox"] = new("578f87c2245977357b2c3f6f"),

        // 🧃 Provisions
        ["food"] = new("578f87c6245977356274f2cf"),
        ["provisions"] = new("578f87c6245977356274f2cf"),

        // 🧪 Medical
        ["medical"] = new("578f87c8245977356274f2d0"),
        ["med"] = new("578f87c8245977356274f2d0"),

        // 💻 Electronics
        ["electronics"] = new("578f87cc245977357b2c3f70"),

        // 🧳 Ground cache
        ["cache"] = new("5d6d2bb386f774785b07a77a"),
        ["ground_cache"] = new("5d6d2bb386f774785b07a77a"),
        ["stash"] = new("5d6d2bb386f774785b07a77a")
    };

    public static readonly Dictionary<string, MongoId[]> ContainerGroups =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["all"] = [],
            ["*"] = [],

            ["corpse"] =
            [
                new MongoId("5909e4b686f7747f5b744fa4")
            ],

            ["containers"] =
            [
                new MongoId("578f87ad245977356274f2cc"), // weapon
                new MongoId("578f87a3245977356274f2cb"), // ammo
                new MongoId("578f87b7245977356274f2cd"), // grenade
                new MongoId("578f87b2245977356274f2cc"), // duffle
                new MongoId("578f8778245977358849a9b5")  // safe
            ],

            ["all_loot"] =
            [
                new MongoId("5909e4b686f7747f5b744fa4"),
                new MongoId("59387ac686f77401442ddd61"),
                new MongoId("578f87b2245977356274f2cc"),
                new MongoId("578f87ad245977356274f2cc"),
                new MongoId("578f87a3245977356274f2cb"),
                new MongoId("578f87b7245977356274f2cd"),
                new MongoId("578f8778245977358849a9b5")
            ]
        };
}