using Microsoft.Extensions.Primitives;
using System.Xml.Serialization;

namespace AMDevIT.Restling.Tests.Models.Xml
{
    [XmlRoot("slideshow")]
    public class Slideshow
    {
        #region Properties

        [XmlAttribute("title")]
        public string? Title { get; set; }

        [XmlAttribute("date")]
        public string? Date { get; set; }

        [XmlAttribute("author")]
        public string? Author { get; set; }

        [XmlElement("slide")]
        public List<Slide>? Slides { get; set; }

        #endregion

        #region Methods

        public override string ToString()
        {
            string slides = string.Join(", ", this.Slides?.Select(s => s.ToString()) ?? Enumerable.Empty<string>());
            return $"[Title:{this.Title},Date:{this.Date},Author:{this.Author},Slides:{slides}]";
        }

        #endregion
    }
}
