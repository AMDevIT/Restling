using System.Xml.Serialization;

namespace AMDevIT.Restling.Tests.Models.Xml
{
    public class Slide
    {
        #region Properties

        [XmlAttribute("type")]
        public string? Type { get; set; }

        [XmlElement("title")]
        public string? Title { get; set; }

        [XmlElement("item")]
        public List<SlideItem>? Items { get; set; }

        #endregion

        #region Methods

        public override string ToString()
        {
            string slideItems = string.Join(", ", this.Items?.Select(i => i.ToString()) ?? Enumerable.Empty<string>());
            return $"[Type:{this.Type},Title:{this.Title},Items:{slideItems}]";
        }

        #endregion
    }
}
