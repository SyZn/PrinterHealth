using System.Collections.Generic;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace KMBizhubHealthModule
{
    /// <summary>
    /// Device access class supporting Konica Minolta C454, C554, and C554e.
    /// </summary>
    public class BizhubC554Device : BizhubDevice
    {
        public BizhubC554Device(JObject parameters)
            : base(parameters)
        {
        }

        public override string ActiveJobsEndpoint
        {
            get { return "/wcd/job_active.xml"; }
        }

        public override string ConsumablesStatusEndpoint
        {
            get { return "/wcd/system_device.xml"; }
        }

        protected override IEnumerable<XElement> AllJobs(XDocument doc)
        {
            return doc
                .Element("MFP")
                .Element("JobList")
                .Elements("Job")
            ;
        }
    }
}
