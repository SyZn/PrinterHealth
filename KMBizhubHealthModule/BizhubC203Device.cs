using System.Collections.Generic;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace KMBizhubHealthModule
{
    /// <summary>
    /// Device access class supporting Konica Minolta C203.
    /// </summary>
    public class BizhubC203Device : BizhubDevice
    {
        public BizhubC203Device(JObject parameters)
            : base(parameters)
        {
        }

        public override string ActiveJobsEndpoint
        {
            get { return "/wcd/job.xml"; }
        }

        public override string ConsumablesStatusEndpoint
        {
            get { return "/wcd/system.xml"; }
        }

        protected override IEnumerable<XElement> AllJobs(XDocument doc)
        {
            return doc
                .Element("MFP")
                .Element("JobList")
                .Element("Print")
                .Elements("Job")
            ;
        }
    }
}
