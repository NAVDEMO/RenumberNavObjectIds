using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using System.Collections;

namespace RenumberObjectIds
{
    [Cmdlet("Renumber", "NavObjectIds")]
    public class PerformRenumberCommand : Cmdlet
    {
        [Parameter(Mandatory=true)]
        public string SourceFolder { get; set; }
        [Parameter(Mandatory = true)]
        public string DestinationFolder { get; set; }
        [Parameter(Mandatory = true)]
        public Hashtable RenumberList { get; set; }

        protected override void ProcessRecord()
        {
            RenumberIds renumberIds = new RenumberIds();
            renumberIds.OnWriteVerbose += RenumberIds_OnWriteVerbose;
            foreach (DictionaryEntry obj in RenumberList)
            {
                renumberIds.AddToRenumberList((int)obj.Key, (int)obj.Value, 1);
            }
            renumberIds.PerformRenumber(SourceFolder, DestinationFolder);
        }

        private void RenumberIds_OnWriteVerbose(string message)
        {
            WriteVerbose(message);
        }
    }
}
