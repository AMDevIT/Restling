using System.Xml.Serialization;

namespace AMDevIT.Restling.Tests.Models.Xml
{
    public class SlideItem
    {
        #region Properties

        [XmlText]
        public string? Text { get; set; }

        [XmlElement("em")]
        public string? Emphasis { get; set; }

        #endregion

        #region Methods

        public override string ToString()
        {
            return $"[Text:{this.Text},Emphasis:{this.Emphasis}]";
        }

        #endregion
    }
}
