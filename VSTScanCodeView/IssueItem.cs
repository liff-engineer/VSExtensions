using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace VSTScanCodeView
{
    public class IssueItem
    {
        [XmlAttribute("file")]
        public string File { get; set; }
        [XmlAttribute("line")]
        public int Line { get; set; }
        [XmlAttribute("id")]
        public string IssueType { get; set; }
        [XmlAttribute("subid")]
        public string IssueSubType { get; set; }
        [XmlAttribute("severity")]
        public string IssueLevel { get; set; }
        [XmlAttribute("msg")]
        public string Message { get; set; }
        [XmlAttribute("web_identify")]
        public string Identify { get; set; }
        [XmlAttribute("func_info")]
        public string Function { get; set; }
        [XmlAttribute("content")]
        public string Content { get; set; }
    }

    [XmlRoot("results")]
    public class ScanResult
    {
        [XmlElement("error")]
        public List<IssueItem> errors;

        public static ScanResult LoadFromXMLString(string xmlText)
        {
            if (!xmlText.StartsWith("<?xml version"))
            {
                if (xmlText.Contains("<?xml version"))
                {
                    xmlText = xmlText.Substring(xmlText.IndexOf("<?xml version"));
                }
                else
                {
                    return null;
                }
            }
            try
            {
                using (var reader = new StringReader(xmlText))
                {
                    var serializer = new XmlSerializer(typeof(ScanResult));
                    return serializer.Deserialize(reader) as ScanResult;
                }
            }
            catch { return null; }
        }
    }

    public class ScanFileAction
    {
        public string file { get; set; }
    }

    public class ScanDirectoryAction
    {
        public string directory { get; set; }
    }
}
