using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenumberNavObjectIds
{
    class Options
    {
        [Option('r', "reverse", Required = false, HelpText = "Reverse (renumber from destination/toObjectId to sources/fromObjectId)")]
        public bool reverse { get; set; }

        [Option('s', "sources", Required = false, HelpText = "Folder with source files.")]
        public string sources { get; set; }

        [Option('d', "destination", Required = false, HelpText = "Folder in which new source files will be written.")]
        public string destination { get; set; }

        [Option('o', "objectidfile", Required = false, HelpText = "Path of XML file holding object id renumber set. (Required if range isn't specified)")]
        public string objectidfile { get; set; }

        [Option('f', "fromobjectid", Required = false, HelpText = "First object id of the source object id range. (Required if objectidfile isn't specified)")]
        public string fromobjectid { get; set; }

        [Option('t', "toobjectid", Required = false, HelpText = "First object id of the destination object id range. (Required if objectidfile isn't specified)")]
        public string toobjectid { get; set; }

        [Option('n', "noofobjects", Required = false, HelpText = "Number of object ids in the object id range. (Required if objectidfile isn't specified)")]
        public string noofobjects { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return GetUsage(string.Empty);
        }

        public string GetUsage(string error)
        {
            var help = new HelpText
            {
                Heading = new HeadingInfo("Renumber Nav Object Ids", "Freddy Kristiansen"),
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true,
                MaximumDisplayWidth = 80
            };
            var parserErrors = help.RenderParsingErrorsText(this, 2);
            if (!string.IsNullOrEmpty(error) || !string.IsNullOrEmpty(parserErrors))
            {
                help.AddPostOptionsLine("ERRORS:");
                if (!string.IsNullOrEmpty(parserErrors))
                    help.AddPostOptionsLine(parserErrors);
                if (!string.IsNullOrEmpty(error))
                    help.AddPostOptionsLine("  "+error);
            }
            help.AddOptions(this);
            return help;
        }
    }
}
