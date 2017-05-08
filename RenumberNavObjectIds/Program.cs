using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using RenumberObjectIds;

namespace RenumberNavObjectIds
{

    class Program
    {
        public static Options options = new Options();

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                args = new string[] { "--help" };
            }
            if (!CommandLine.Parser.Default.ParseArguments(args, options))
                return;

            if (options.LastParserState != null)
                return;

            RenumberIds renumberIds = new RenumberIds();
            renumberIds.OnWriteVerbose += RenumberIds_OnWriteVerbose;

            if (!string.IsNullOrEmpty(options.FromObjectId) || !string.IsNullOrEmpty(options.ToObjectId) || !string.IsNullOrEmpty(options.NoOfObjects))
            {
                if (int.TryParse(options.FromObjectId, out int from) && int.TryParse(options.ToObjectId, out int to) && int.TryParse(options.NoOfObjects, out int count))
                {
                    try
                    {
                        renumberIds.PerformRenumber(options.Sources, options.Destination, from, to, count, options.Reverse, options.DontRename);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(options.GetUsage(e.Message));
                        return;
                    }
                    if (!string.IsNullOrEmpty(options.ObjectIdFile))
                    {
                        try
                        {
                            renumberIds.WriteRenumberList(options.ObjectIdFile);
                            Console.WriteLine(string.Format("Object id file ({0}) updated.", options.ObjectIdFile));
                        }
                        catch 
                        {
                            Console.WriteLine(options.GetUsage(string.Format("Error writing object id file ({0}).", options.ObjectIdFile)));
                            return;
                        }
                    }
                }
                else
                {
                    Console.WriteLine(options.GetUsage("You need to specify either none of or all three object id range parameters: -f/--fromobjectid, -t/--toobjectid and -n/--noofobjects"));
                    return;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(options.ObjectIdFile))
                {
                    Console.WriteLine(options.GetUsage("You need to specify the object id file (-o/--objectidfile) if you don't specify object id range parameters."));
                    return;
                }
                try
                { 
                    renumberIds.PerformRenumber(options.Sources, options.Destination, options.ObjectIdFile, options.Reverse, options.DontRename);
                }
                catch(Exception e)
                {
                    Console.WriteLine(options.GetUsage(e.Message));
                    return;
                }
            }
        }

        private static void RenumberIds_OnWriteVerbose(string message)
        {
            Console.WriteLine(message);
        }
    }
}
