using System.Collections.Generic;

namespace SaveData1.CrossPlateTesting.Models
{
    public class ParameterReferenceFile
    {
        public string name { get; set; }
        public string version { get; set; }
        public List<ParameterDef> parameters { get; set; }
    }

    public class ParameterDef
    {
        public string name { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public string range { get; set; }
    }
}
