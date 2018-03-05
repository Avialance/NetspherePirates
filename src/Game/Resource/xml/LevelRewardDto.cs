using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Netsphere.Resource.xml
{
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false, ElementName = "LevelReward")]
    public class LevelRewardDto
    {
        [XmlElement("level")]
        public Level[] Levels { get; set; }
    }

    [XmlType(AnonymousType = true)]
    public class Level
    {
        [XmlAttribute]
        public int number { get; set; }

        [XmlAttribute]
        public int pen { get; set; }
    }
}
