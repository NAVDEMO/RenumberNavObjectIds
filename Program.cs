using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace RenumberNavObjectIds
{
    public class Renumber
    {
        public int FromObjectId;
        public int ToObjectId;

        public Renumber() {}
    };

    class Program
    {
        public static Options options = new Options();
        public static List<Renumber> renumber;

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

            if (!string.IsNullOrEmpty(options.fromobjectid) || !string.IsNullOrEmpty(options.toobjectid) || !string.IsNullOrEmpty(options.noofobjects))
            {
                int from, to, count;
                if (int.TryParse(options.fromobjectid, out from) && int.TryParse(options.toobjectid, out to) && int.TryParse(options.noofobjects, out count))
                {
                    CreateRenumberList(from, to, count);
                    if (!string.IsNullOrEmpty(options.objectidfile))
                    {
                        try
                        {
                            WriteRenumberFile(options.objectidfile);
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
                    using (var fs = new FileStream(options.objectidfile, FileMode.Open))
                    {
                        XmlSerializer ser = new XmlSerializer(typeof(List<Renumber>));
                        renumber = (List<Renumber>)ser.Deserialize(fs);
                    }
                }
                catch
                {
                    Console.WriteLine(options.GetUsage(string.Format("Object id file ({0}) doesn't exist or has a wrong format.", options.objectidfile)));
                    return;
                }
            }

            // Check that renumber ranges doesn't overlap
            if (!CheckRenumberList())
            {
                Console.WriteLine("Renumber ranges overlap, cannot renumber.");
                return;
            }

            // Reverse to/from 
            if (options.reverse)
            {
                // Swap to/from
                foreach (var renum in renumber)
                {
                    var old = renum.FromObjectId;
                    renum.FromObjectId = renum.ToObjectId;
                    renum.ToObjectId = old;
                }
                // Swap sources/destination
                var temp = options.sources;
                options.sources = options.destination;
                options.destination = temp;
            }

            // Perform renumber
            if (!string.IsNullOrEmpty(options.sources) && !string.IsNullOrEmpty(options.destination))
            {
                if (!options.sources.EndsWith(@"\")) { options.sources += @"\"; }
                if (!options.destination.EndsWith(@"\")) { options.destination += @"\"; }
                Console.WriteLine(string.Format("Renumbering objects in {0}", options.sources));
                var extensionList = new string[] { ".delta", ".txt", ".xml", ".al", ".json" };
                foreach (var filename in Directory.GetFiles(options.sources, "*.*", SearchOption.AllDirectories))
                {
                    var directory = Path.GetDirectoryName(filename) + @"\";
                    var newdirectory = options.destination + directory.Substring(options.sources.Length);
                    if (!System.IO.Directory.Exists(newdirectory))
                        System.IO.Directory.CreateDirectory(newdirectory);
                    var extension = Path.GetExtension(filename).ToLowerInvariant();
                    if (extensionList.Any(extension.Equals))
                    {
                        Console.Write(string.Format("Renumber {0}", Path.GetFileName(filename)));
                        var encoding = GetNavTextFileEncoding(filename);
                        var content = System.IO.File.ReadAllText(filename);
                        renum(extension, ref content);
                        var newFileName = renumFileName(Path.GetFileName(filename));
                        File.WriteAllText(newdirectory + newFileName, content, encoding);
                    }
                    else
                    {
                        Console.Write(string.Format("Copy {0}", Path.GetFileName(filename)));
                        File.Copy(filename, newdirectory + Path.GetFileName(filename), true);
                    }
                    Console.WriteLine(" done.");
                }
                Console.WriteLine("Object renumbering done.");
            }
        }

        /// <summary>
        /// Check that numbers in the Renumber ranges doesn't overlap
        /// as this might yield unpredictable results
        /// </summary>
        /// <returns>true if the Renumber list is OK</returns>
        private static bool CheckRenumberList()
        {
            foreach (var r1 in renumber)
                foreach (var r2 in renumber)
                    if (r1 != r2 && r1.FromObjectId == r2.ToObjectId)
                        return false;
            return true;
        }

        /// <summary>
        /// Create Renumber list based on from, to and count
        /// </summary>
        /// <param name="fromObjectId">First object id of the source object id range.</param>
        /// <param name="toObjectId">First object id of the destination object id range.</param>
        /// <param name="noOfObjectIds">Number of object ids in the object id range.</param>
        public static void CreateRenumberList(int fromObjectId, int toObjectId, int noOfObjectIds)
        {
            renumber = new List<Renumber>();
            for (int i = 0; i < noOfObjectIds; i++)
                renumber.Add(new Renumber() { FromObjectId = fromObjectId + i, ToObjectId = toObjectId + i });
        }

        /// <summary>
        /// Write Renumber Array file
        /// </summary>
        /// <param name="filename">File of Renumber file</param>
        public static void WriteRenumberFile(string filename)
        {
            using (var fs = new FileStream(options.objectidfile, FileMode.Create))
            {
                XmlSerializer ser = new XmlSerializer(typeof(List<Renumber>));
                ser.Serialize(fs, renumber);
            }
        }

        /// <summary>
        /// Get Encoding of NAV Text file
        /// All Delta files are CP1252, Some text files are UTF8
        /// </summary>
        /// <param name="filename">File to check</param>
        /// <returns>Encoding of the file</returns>
        private static Encoding GetNavTextFileEncoding(string filename)
        {
            var encoding = Encoding.Default;
            var bom = new byte[3];
            using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read))
                file.Read(bom, 0, 3);
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf)
                encoding = Encoding.UTF8;
            return encoding;
        }

        /// <summary>
        /// Renumber file content
        /// </summary>
        /// <param name="extension">File Extention</param>
        /// <param name="content">File Content</param>
        private static void renum(string extension, ref string content)
        {
            switch (extension)
            {
                case ".delta":
                    // Renumber object references in DELTA files
                    content = renumDelta(content);
                    break;
                case ".txt":
                    if (content.StartsWith("OBJECT", StringComparison.InvariantCultureIgnoreCase))
                        // TXT file is OBJECT metadata - not language file
                        content = renumDelta(content);
                    else
                        // Renumber object references in language text files
                        foreach (var obj in renumber)
                            renumTxt(ref content, obj.FromObjectId, obj.ToObjectId);
                    break;
                case ".xml":
                    // Renumber object references in .xml files (permissions or Web services)
                    foreach (var obj in renumber)
                        renumXml(ref content, obj.FromObjectId, obj.ToObjectId);
                    break;
                case ".al":
                    // Renumber object references in .al files (New Development Environment)
                    content = renumAl(content);
                    break;
                case ".json":
                    // Renumber object references in launch.json file (startupObjectId)
                    foreach (var obj in renumber)
                        renumJson(ref content, obj.FromObjectId, obj.ToObjectId);
                    break;
            }
        }

        /// <summary>
        /// Renumber object references in .DELTA files
        /// </summary>
        /// <param name="content">Content of the .DELTA file</param>
        private static string renumDelta(string content)
        {
            var codelineStart = new string(' ', 16);
            var newcontent = new StringBuilder();
            // Handle different types of newline
            var lines = content.Replace("\r\n", "\r").Replace("\n", "\r").Split('\r');
            var lengthDiff = 0;
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var newline = renumLine(line);

                // If line to be indented, then indent
                if (line.StartsWith(codelineStart))
                {
                    if (lengthDiff < 0)
                        newline = newline.Remove(0, -lengthDiff);
                    else if (lengthDiff > 0)
                        newline = newline.Substring(0, lengthDiff) + newline;
                }
                else
                {
                    // Recalc indentation
                    lengthDiff = 0;
                    foreach (var obj in renumber)
                    {
                        var fromStr = obj.FromObjectId.ToString();
                        var toStr = obj.ToObjectId.ToString();
                        if (line.StartsWith("    { " + fromStr) && newline.StartsWith("    { " + toStr))
                            lengthDiff = toStr.Length - fromStr.Length;
                    }
                }
                if (i<lines.Length-1)
                {
                    newcontent.AppendLine(newline);
                }
                else
                {
                    newcontent.Append(newline);
                }

            }
            return newcontent.ToString();
        }

        /// <summary>
        /// Renumber object references in line
        /// </summary>
        /// <param name="line">Line from .DELTA file</param>
        /// <returns>New line (with renumbered object references)</returns>
        private static string renumLine(string line)
        {
            char[] quotes = { '"', '\'' };
            var idx = 0;
            var newline = new StringBuilder();
            while (idx < line.Length)
            {
                var strstart = line.IndexOfAny(quotes, idx);
                string str;
                if (strstart < 0)
                {
                    // No more strings in the file
                    strstart = line.Length;
                    str = "";
                }
                else
                {
                    // String found in file
                    var quote = line[strstart];
                    var strend = line.IndexOf(quote, strstart + 1);
                    str = line.Substring(strstart, strend - strstart + 1);
                }

                // Renumber object references in part of file until string                       
                var part = line.Substring(idx, strstart - idx);
                foreach (var obj in renumber)
                    ReplaceInString(ref part, string.Empty, obj.FromObjectId, obj.ToObjectId);

                // Add part with renumbered objects references
                newline.Append(part);

                // Add String
                newline.Append(str);

                // Find next string
                idx = strstart + str.Length;
            }
            return newline.ToString();
        }

        /// <summary>
        /// Renumber object references in language .TXT files
        /// </summary>
        /// <param name="content">Content of a language text file</param>
        /// <param name="from">ObjectId to renumber from</param>
        /// <param name="to">ObjectId to renumber to</param>
        private static void renumTxt(ref string content, int from, int to)
        {
            foreach (var prefix in new string[] { "N", "C", "Q", "F", "G" })
            {
                var str = "{0}{1}-";
                var fromStr = string.Format(str, prefix, from.ToString());
                var toStr = string.Format(str, prefix, to.ToString());
                content = content.Replace(fromStr, toStr);
            }
        }

        /// <summary>
        /// Renumber object references in .XML files (Permissions, Web Services)
        /// </summary>
        /// <param name="content">Content of a .XML file</param>
        /// <param name="from">ObjectId to renumber from</param>
        /// <param name="to">ObjectId to renumber to</param>
        private static void renumXml(ref string content, int from, int to)
        {
            var str = "<ObjectID>{0}</ObjectID>";
            var fromStr = string.Format(str, from.ToString());
            var toStr = string.Format(str, to.ToString());
            content = content.Replace(fromStr, toStr);
        }

        /// <summary>
        /// Renumber object references in .AL files
        /// </summary>
        /// <param name="content">Content of the .AL file</param>
        private static string renumAl(string content)
        {
            var newcontent = new StringBuilder();
            // Handle different types of newline
            var lines = content.Replace("\r\n", "\r").Replace("\n", "\r").Split('\r');
            foreach (var line in lines)
                newcontent.AppendLine(renumLine(line));
            return newcontent.ToString();
        }

        /// <summary>
        /// Renumber object references in launch.json file
        /// </summary>
        /// <param name="content">Content of the launch.json</param>
        private static void renumJson(ref string content, int from, int to)
        {
            ReplaceInString(ref content, "\"startupObjectId\": {0}\n", from, to);
            ReplaceInString(ref content, "\"startupObjectId\": {0}\r", from, to);
        }

        /// <summary>
        /// Replace objectId in string if it matches a pattern or the Id alone
        /// </summary>
        /// <param name="content">string to replace the string in</param>
        /// <param name="searchForPattern">Pattern to use when searching</param>
        /// <param name="from">Renumber from ObjectId</param>
        /// <param name="to">Renumber to ObjectId</param>
        private static void ReplaceInString(ref string content, string searchForPattern, int from, int to)
        {
            var fromStr = from.ToString();
            var toStr = to.ToString();
            var startIdx = 0;
            while (true)
            {
                if (string.IsNullOrEmpty(searchForPattern))
                {
                    // No search pattern
                    // Search for ObjectId without a numeric character in front/behind
                    var idx = content.IndexOf(fromStr, startIdx);
                    if (idx < 0)
                        break;
                    if ((idx == 0 || IsNonNumeric(content[idx - 1])) &&
                        (idx + fromStr.Length == content.Length || IsNonNumeric(content[idx + fromStr.Length])))
                    {
                        content = content.Substring(0, idx) + toStr + content.Substring(idx + fromStr.Length);
                        startIdx = idx + toStr.Length;
                    }
                    else
                    {
                        startIdx = idx + 1;
                    }
                }
                else
                {
                    // Search for pattern and replace ObjectId
                    var searchFor = string.Format(searchForPattern, from);
                    var idx = content.IndexOf(searchFor, StringComparison.InvariantCultureIgnoreCase);
                    if (idx < 0)
                        break;
                    var subIdx = searchFor.IndexOf(fromStr);
                    content = content.Substring(0, idx + subIdx) + toStr + content.Substring(idx + subIdx + fromStr.Length);
                    startIdx = idx + toStr.Length;
                }
            }
        }

        /// <summary>
        /// Check whether character is non-numeric
        /// </summary>
        /// <param name="ch">Character to test</param>
        /// <returns>true if the character is non-numeric</returns>
        private static bool IsNonNumeric(char ch)
        {
            return "0123456789".IndexOf(ch) < 0;
        }

        /// <summary>
        /// Renumber Object ID in filename
        /// </summary>
        /// <param name="fileName">Filename with Object ID (ex.COD52000.TXT)</param>
        /// <returns>Filename with new Object ID</returns>
        private static string renumFileName(string fileName)
        {
            foreach (var obj in renumber)
                ReplaceInString(ref fileName, string.Empty, obj.FromObjectId, obj.ToObjectId);
            return fileName;
        }

    }
}
