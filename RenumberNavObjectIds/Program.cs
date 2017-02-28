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

            if (!string.IsNullOrEmpty(options.fromobjectid) || !string.IsNullOrEmpty(options.toobjectid) || !string.IsNullOrEmpty(options.noofobjects))
            {
                int from, to, count;
                if (int.TryParse(options.fromobjectid, out from) && int.TryParse(options.toobjectid, out to) && int.TryParse(options.noofobjects, out count))
                {
                    try
                    {
                        renumberIds.PerformRenumber(options.sources, options.destination, from, to, count, options.reverse);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(options.GetUsage(e.Message));
                        return;
                    }
                    if (!string.IsNullOrEmpty(options.objectidfile))
                    {
                        try
                        {
                            renumberIds.WriteRenumberList(options.objectidfile);
                            Console.WriteLine(string.Format("Object id file ({0}) updated.", options.objectidfile));
                        }
                        catch 
                        {
                            Console.WriteLine(options.GetUsage(string.Format("Error writing object id file ({0}).", options.objectidfile)));
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
                if (string.IsNullOrEmpty(options.objectidfile))
                {
                    Console.WriteLine(options.GetUsage("You need to specify the object id file (-o/--objectidfile) if you don't specify object id range parameters."));
                    return;
                }
                try
                { 
                    renumberIds.PerformRenumber(options.sources, options.destination, options.objectidfile, options.reverse);
                }
                catch(Exception e)
                {
                    Console.WriteLine(options.GetUsage(e.Message));
                    return;
                }
            }
        }
    }
}
