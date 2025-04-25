using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AMDevIT.Restling.Tests.Models.Security
{
    [XmlRoot("foo")]
    public class SecurityXMLTestPayload
    {
        #region Properties

        [XmlText]
        public string? Value 
        { 
            get; 
            set; 
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            return $"{this.Value}";
        }

        #endregion
    }
}
