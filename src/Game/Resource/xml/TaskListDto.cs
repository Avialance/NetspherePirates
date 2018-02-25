using System.Xml.Serialization;

namespace Netsphere.Resource.xml
{
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false, ElementName = "task")]
    public class TaskListDto
    {
        [XmlElement("ex_pensetup")]
        public ExPenSetup[] ex_pensetup { get; set; }

        [XmlElement("compulsory_task")]
        public CompulsoryTask compulsory_task { get; set; }

        [XmlElement("weekly_task")]
        public CompulsoryTask weekly_task { get; set; }

        [XmlAttribute]
        public string string_table { get; set; }
    }

    [XmlType(AnonymousType = true)]
    public class ExPenSetup
    {
        [XmlElement("set")]
        public Set[] set { get; set; }
    }

    [XmlType(AnonymousType =true)]
    public class CompulsoryTask
    {
        [XmlElement("base_setting")]
        public BaseSetting[] base_setting { get; set; }
    }

    [XmlType(AnonymousType = true)]
    public class Set
    {
        [XmlAttribute]
        public string period { get; set; }

        [XmlAttribute]
        public int level { get; set; }

        [XmlAttribute]
        public int count { get; set; }
    }

    [XmlType(AnonymousType =true)]
    public class BaseSetting
    {
        [XmlElement("lang")]
        public Lang[] lang { get; set; }

        [XmlElement("level_setting")]
        public LevelSetting level_setting { get; set; }

        [XmlAttribute]
        public string name_key { get; set; }

        [XmlAttribute]
        public string mode_type { get; set; }

        [XmlAttribute]
        public string category { get; set; }

        [XmlAttribute]
        public string name { get; set; }
    }

    [XmlType(AnonymousType = true)]
    public class Lang
    {
        [XmlElement("nation")]
        public Nation[] nation { get; set; }
    }

    [XmlType(AnonymousType = true)]
    public class LevelSetting
    {
        [XmlElement("select_condition")]
        public SelectCondition select_condition { get; set; }

        [XmlElement("complet_condition")]
        public CompletCondition complet_condition { get; set; }

        [XmlElement("reward")]
        public Reward reward { get; set; }

        [XmlElement("help_message")]
        public HelpMessage help_message { get; set; }

        [XmlAttribute]
        public int id { get; set; }

        [XmlAttribute]
        public int level { get; set; }

        [XmlAttribute]
        public int chance_value { get; set; }

        [XmlAttribute]
        public int add_chance_value { get; set; }

        [XmlAttribute]
        public int add_chan_limit_lv { get; set; }
    }

    [XmlType(AnonymousType = true)]
    public class Nation
    {
        public int id { get; set; }
    }

    [XmlType(AnonymousType = true)]
    public class SelectCondition
    {
        public SingleValuef kill_per_death { get; set; }
        public SingleValue touch_down_score { get; set; }
        public HaveLicense have_license { get; set; }
        public SingleValue have_weapon { get; set; }
        public SingleValue min_level { get; set; }
        public SingleValue max_level { get; set; }
        public SingleValue exp_rate { get; set; }
    }

    [XmlType(AnonymousType = true)]
    public class HaveLicense
    {
        [XmlAttribute]
        public string value { get; set; }
    }

    [XmlType(AnonymousType = true)]
    public class CompletCondition
    {
        public SingleValue game_play_ts { get; set; }
        public SingleValue number_of_team_person { get; set; }
        public SingleValue goal_of_match { get; set; }
        public SingleValue repetetion { get; set; }
        public CheckerType checker_type { get; set; }
    }

    [XmlType(AnonymousType = true)]
    public class Reward
    {
        [XmlElement("pen")]
        public RPen pen { get; set; }

        [XmlElement("ex_pen")]
        public SingleValue[] ex_pen { get; set; }
    }

    [XmlType(AnonymousType = true)]
    public class HelpMessage
    {
        public Message massage01 { get; set; }
        public Message massage02 { get; set; }

        public Lang lang { get; set; }
    }

    [XmlType(AnonymousType = true)]
    public class Message
    {
        public string string_key { get; set; }
    }

    [XmlType(AnonymousType = true)]
    public class RPen
    {
        [XmlAttribute]
        public int value { get; set; }

        [XmlAttribute]
        public int chance_value { get; set; }
    }

    [XmlType(AnonymousType = true)]
    public class CheckerType
    {
        [XmlAttribute]
        public string value { get; set; }

        [XmlAttribute]
        public string data { get; set; }
    }

    [XmlType(AnonymousType = true)]
    public class SingleValue
    {
        [XmlAttribute]
        public int value { get; set; }
    }

    [XmlType(AnonymousType = true)]
    public class SingleValuef
    {
        [XmlAttribute]
        public float value { get; set; }
    }
}
