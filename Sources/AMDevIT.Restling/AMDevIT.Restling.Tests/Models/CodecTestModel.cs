using System.Xml.Serialization;

namespace AMDevIT.Restling.Tests.Models
{
    [XmlRoot("item")]
    public sealed class CodecTestModel
    {
        #region Properties

        public int Id { get; set; }
        public string? Name { get; set; }

        #endregion
    }
}
