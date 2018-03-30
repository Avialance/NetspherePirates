using System.Xml.Serialization;

namespace Netsphere.Resource.xml
{
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false, ElementName = "string_table")]
    public class StringTableDto
    {
        [XmlElement("string")]
        public StringTableStringDto[] @string { get; set; }
    }

    [XmlType(AnonymousType = true)]
    public class StringTableStringDto
    {
        [XmlAttribute]
        public string key { get; set; }

        [XmlAttribute]
        public string kor { get; set; }

        [XmlAttribute]
        public string ger { get; set; }

        [XmlAttribute]
        public string eng { get; set; }

        [XmlAttribute]
        public string fre { get; set; }

        [XmlAttribute]
        public string spa { get; set; }

        [XmlAttribute]
        public string ita { get; set; }

        [XmlAttribute]
        public string rus { get; set; }

        [XmlAttribute]
        public string ame { get; set; }

        [XmlAttribute]
        public string cns { get; set; }

        public override string ToString()
        {
            return key;
        }
    }

    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false, ElementName = "string_table")]
    public class TaskStringTableDto
    {
        [XmlElement("string")]
        public TaskStringDto[] @string { get; set; }
    }

    [XmlType(AnonymousType = true)]
    public class TaskStringDto
    {
        [XmlAttribute]
        public string key { get; set; }

        [XmlElement]
        public TaskSStringDto kor { get; set; }

        [XmlElement]
        public TaskSStringDto ger { get; set; }

        [XmlElement]
        public TaskSStringDto eng { get; set; }

        [XmlElement]
        public TaskSStringDto fre { get; set; }

        [XmlElement]
        public TaskSStringDto spa { get; set; }

        [XmlElement]
        public TaskSStringDto ita { get; set; }

        [XmlElement]
        public TaskSStringDto rus { get; set; }

        [XmlElement]
        public TaskSStringDto ame { get; set; }

        [XmlElement]
        public TaskSStringDto cns { get; set; }
    }

    [XmlType(AnonymousType = true)]
    public class TaskSStringDto
    {
        [XmlAttribute]
        public string value { get; set; }
    }
}
