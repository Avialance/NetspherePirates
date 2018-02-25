using System;
using System.Collections.Generic;
using System.Text;

namespace Netsphere.Resource
{
    public enum TaskMode
    {
        TMT_COMMON,
        TMT_DEATH_MATCH,
        TMT_TOUCH_DOWN,
        TMT_CHASER,
    }

    public enum TaskCategory
    {
        TC_TUTORIAL,
        TC_JOIN,
        TC_WIN,
        TC_NEW_RECORD,
        TC_WEAPON_NEW_RECORD,
        TC_LICENSE,
        TC_MAP_PLAY,
    }

    internal class TaskInfo
    {
        public uint Id { get; set; }
        public uint Level { get; set; }
        public uint Chance { get; set; }
        public uint AddChance { get; set; }
        public uint AddChanceLevelLimit { get; set; }
        public string Name { get; set; }
        public TaskMode Mode { get; set; }
        public TaskCategory Category { get; set; }
        public bool constant { get; set; }

        public SelectCondition StartCondition { get; set; }
        public CompletionCondition EndCondition { get; set; }

        public uint RewardPen { get; set; }
        public uint RewardExp { get; set; }

        private static readonly Dictionary<string, TaskMode> s_mode = new Dictionary<string, TaskMode> {
            { "TMT_COMMON", TaskMode.TMT_COMMON },
            { "TMT_DEATH_MATCH", TaskMode.TMT_DEATH_MATCH },
            { "TMT_TOUCH_DOWN", TaskMode.TMT_TOUCH_DOWN },
            { "TMT_CHASER", TaskMode.TMT_CHASER }
        };
        private static readonly Dictionary<string, TaskCategory> s_category = new Dictionary<string, TaskCategory> {
            { "TC_TUTORIAL", TaskCategory.TC_TUTORIAL },
            { "TC_JOIN", TaskCategory.TC_JOIN },
            { "TC_WIN", TaskCategory.TC_WIN },
            { "TC_NEW_RECORD", TaskCategory.TC_NEW_RECORD },
            { "TC_WEAPON_NEW_RECORD", TaskCategory.TC_WEAPON_NEW_RECORD },
            { "TC_LICENSE", TaskCategory.TC_LICENSE },
            { "TC_MAP_PLAY", TaskCategory.TC_MAP_PLAY },
        };

        public TaskInfo(string name, string mode, string category)
        {
            Name = name;
            Mode = s_mode[mode];
            Category = s_category[category];
        }

        public uint GetChance(Player plr)
        {
            return Chance + (AddChance * Math.Min(plr.Level, AddChanceLevelLimit));
        }
    }

    internal class SelectCondition
    {
        public float KDR { get; set; }
        public float TDScore { get; set; }
        public uint License { get; set; }
        public uint Weapon { get; set; }
        public uint MinLevel { get; set; }
        public uint MaxLevel { get; set; }
        public ulong ExpRate { get; set; }

        public bool CanStart(Player plr)
        {
            return ((plr.Level >= MinLevel && plr.Level <= MaxLevel) ||
                (MinLevel == 0 && MaxLevel == 0)) &&
                plr.DeathMatch.KDRate >= KDR &&
                plr.TouchDown.TD >= TDScore;
        }
    }

    internal class CompletionCondition
    {
        public int game_play_ts { get; set; }
        public int number_of_team_person { get; set; }
        public int goal_of_match { get; set; }
        public int repetetion { get; set; }
    }
}
