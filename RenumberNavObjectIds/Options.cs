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
        public bool Reverse { get; set; }

        [Option('m', "dontrename", Required = false, HelpText = "Don't rename files (helps you compare files afterwards)")]
        public bool DontRename { get; set; }

        [Option('s', "sources", Required = false, HelpText = "Folder with source files.")]
        public string Sources { get; set; }

        [Option('d', "destination", Required = false, HelpText = "Folder in which new source files will be written.")]
        public string Destination { get; set; }

        [Option('o', "objectidfile", Required = false, HelpText = "Path of XML file holding object id renumber set. (Required if range isn't specified)")]
        public string ObjectIdFile { get; set; }

        [Option('f', "fromobjectid", Required = false, HelpText = "First object id of the source object id range. (Required if objectidfile isn't specified)")]
        public string FromObjectId { get; set; }

        [Option('t', "toobjectid", Required = false, HelpText = "First object id of the destination object id range. (Required if objectidfile isn't specified)")]
        public string ToObjectId { get; set; }

        [Option('n', "noofobjects", Required = false, HelpText = "Number of object ids in the object id range. (Required if objectidfile isn't specified)")]
        public string NoOfObjects { get; set; }

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
