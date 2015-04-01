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

        public override string ErrorJobsXPath
        {
            get { return "/MFP/JobList/Print/Job[JobStatus/Status='ErrorPrinting']"; }
        }

        public override string AllJobsXPath
        {
            get { return "/MFP/JobList/Print/Job"; }
        }

        public override string ActiveJobsEndpoint
        {
            get { return "/wcd/job.xml"; }
        }

        public override string ConsumablesStatusEndpoint
        {
            get { return "/wcd/system.xml"; }
        }
    }
}
