using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace RenumberObjectIds
{
    public delegate void WriteVerboseHandler(string message);

    public class RenumberIds
    {
        private List<Renumber> renumberList = new List<Renumber>();

        public event WriteVerboseHandler OnWriteVerbose;

        private void WriteVerbose(string message)
        {
            if (this.OnWriteVerbose != null)
                this.OnWriteVerbose(message);
        }

        public void PerformRenumber(string sourceFolder, string destinationFolder, int fromObjectId, int toObjectId, int noOfObjects, bool reverse = false)
        {
            ClearRenumberList();
            AddToRenumberList(fromObjectId, toObjectId, noOfObjects);
            PerformRenumber(sourceFolder, destinationFolder, reverse);
        }

        public void PerformRenumber(string sourceFolder, string destinationFolder, string objectIdFile, bool reverse = false)
        {
            ReadRenumberList(objectIdFile);
            PerformRenumber(sourceFolder, destinationFolder, reverse);
        }

        // Perform renumber
        public void PerformRenumber(string sourceFolder, string destinationFolder, bool reverse = false)
        {
            if (this.renumberList.Count == 0)
            {
                throw new Exception("Nothing to renumber.");
            }

            // Check that renumber ranges doesn't overlap
            if (!CheckRenumberList())
            {
                throw new ArgumentException("Renumber ranges overlap, cannot renumber.");
            }

            // Reverse to/from 
            if (reverse)
            {
                // Swap to/from
                foreach (var renum in this.renumberList)
                {
                    var old = renum.FromObjectId;
                    renum.FromObjectId = renum.ToObjectId;
                    renum.ToObjectId = old;
                }

                // Swap sources/destination
                var temp = sourceFolder;
                sourceFolder = destinationFolder;
                destinationFolder = temp;
            }

            if (!string.IsNullOrEmpty(sourceFolder) && !string.IsNullOrEmpty(destinationFolder))
            {
                if (!sourceFolder.EndsWith(@"\")) { sourceFolder += @"\"; }
                if (!destinationFolder.EndsWith(@"\")) { destinationFolder += @"\"; }

                // Source and Destination folder cannot be the same
                if (sourceFolder.Equals(destinationFolder, StringComparison.InvariantCultureIgnoreCase))
                    throw new ArgumentException("Sources and Destination cannot point to the same location");

                // Remove Destination folder if it exists
                if (Directory.Exists(destinationFolder))
                    Directory.Delete(destinationFolder, true);

                WriteVerbose(string.Format("Renumbering objects in {0}", sourceFolder));
                var extensionList = new string[] { ".delta", ".txt", ".xml", ".al", ".json" };
                foreach (var filename in Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories))
                {
                    var directory = Path.GetDirectoryName(filename) + @"\";
                    var newdirectory = destinationFolder + directory.Substring(sourceFolder.Length);
                    if (!System.IO.Directory.Exists(newdirectory))
                        System.IO.Directory.CreateDirectory(newdirectory);
                    var extension = Path.GetExtension(filename).ToLowerInvariant();
                    if (extensionList.Any(extension.Equals))
                    {
                        WriteVerbose(string.Format("Renumber {0}", Path.GetFileName(filename)));
                        var encoding = GetNavTextFileEncoding(filename);
                        var content = System.IO.File.ReadAllText(filename, encoding);
                        renum(extension, ref content);
                        var newFileName = renumFileName(Path.GetFileName(filename));
                        File.WriteAllText(newdirectory + newFileName, content, encoding);
                    }
                    else
                    {
                        WriteVerbose(string.Format("Copy {0}", Path.GetFileName(filename)));
                        File.Copy(filename, newdirectory + Path.GetFileName(filename), true);
                    }
                    WriteVerbose("OK");
                }
                WriteVerbose("Object renumbering succeeded");
            }
        }

        /// <summary>
        /// Check that numbers in the Renumber ranges doesn't overlap
        /// as this might yield unpredictable results
        /// </summary>
        /// <returns>true if the Renumber list is OK</returns>
        private bool CheckRenumberList()
        {
            foreach (var r1 in this.renumberList)
                foreach (var r2 in this.renumberList)
                    if (r1 != r2 && r1.FromObjectId == r2.ToObjectId)
                        return false;
            return true;
        }

        /// <summary>
        /// Clear Renumber List
        /// </summary>
        public void ClearRenumberList()
        {
            this.renumberList.Clear();
        }

        /// <summary>
        /// Create Renumber list based on from, to and count
        /// </summary>
        /// <param name="fromObjectId">First object id of the source object id range.</param>
        /// <param name="toObjectId">First object id of the destination object id range.</param>
        /// <param name="noOfObjectIds">Number of object ids in the object id range.</param>
        public void AddToRenumberList(int fromObjectId, int toObjectId, int noOfObjectIds)
        {
            for (int i = 0; i < noOfObjectIds; i++)
                this.renumberList.Add(new Renumber() { FromObjectId = fromObjectId + i, ToObjectId = toObjectId + i });
        }

        /// <summary>
        /// Write Renumber List file
        /// </summary>
        /// <param name="filename">File of Renumber file</param>
        public void WriteRenumberList(string filename)
        {
            using (var fs = new FileStream(filename, FileMode.Create))
            {
                XmlSerializer ser = new XmlSerializer(typeof(List<Renumber>));
                ser.Serialize(fs, this.renumberList);
            }
        }

        /// <summary>
        /// Read Renumber List file
        /// </summary>
        /// <param name="filename">File of Renumber file</param>
        public void ReadRenumberList(string filename)
        {
            using (var fs = new FileStream(filename, FileMode.Open))
            {
                XmlSerializer ser = new XmlSerializer(typeof(List<Renumber>));
                foreach(var renum in (List<Renumber>)ser.Deserialize(fs))
                    this.renumberList.Add(renum);
            }
        }

        /// <summary>
        /// Get Encoding of NAV Text file
        /// All Delta files are CP1252, Some text files are UTF8
        /// </summary>
        /// <param name="filename">File to check</param>
        /// <returns>Encoding of the file</returns>
        private Encoding GetNavTextFileEncoding(string filename)
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
        private void renum(string extension, ref string content)
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
                        foreach (var obj in this.renumberList)
                            renumTxt(ref content, obj.FromObjectId, obj.ToObjectId);
                    break;
                case ".xml":
                    // Renumber object references in .xml files (permissions or Web services)
                    foreach (var obj in this.renumberList)
                        renumXml(ref content, obj.FromObjectId, obj.ToObjectId);
                    break;
                case ".al":
                    // Renumber object references in .al files (New Development Environment)
                    content = renumAl(content);
                    break;
                case ".json":
                    // Renumber object references in launch.json file (startupObjectId)
                    foreach (var obj in this.renumberList)
                        renumJson(ref content, obj.FromObjectId, obj.ToObjectId);
                    break;
            }
        }

        /// <summary>
        /// Renumber object references in .DELTA files
        /// </summary>
        /// <param name="content">Content of the .DELTA file</param>
        private string renumDelta(string content)
        {
            var codelineStart = new string(' ', 16);
            var newcontent = new StringBuilder();
            // Handle different types of newline
            var lines = content.Replace("\r\n", "\r").Replace("\n", "\r").Split('\r');
            var lengthDiff = 0;
            var withinML = false;
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var newline = line;
                if (withinML)
                {
                    if (line.Trim().EndsWith("];"))
                        withinML = false;
                }
                else
                {
                    if ((line.Trim().StartsWith("CaptionML=[", StringComparison.InvariantCultureIgnoreCase)) ||
                        (line.Trim().StartsWith("ToolTipML=[", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        withinML = true;
                    }
                    else
                    {
                        newline = renumLine(line);

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
                            foreach (var obj in this.renumberList)
                            {
                                var fromStr = obj.FromObjectId.ToString();
                                var toStr = obj.ToObjectId.ToString();
                                if (line.StartsWith("    { " + fromStr) && newline.StartsWith("    { " + toStr))
                                    lengthDiff = toStr.Length - fromStr.Length;
                            }
                        }
                    }
                }
                if (i < lines.Length - 1)
                    newcontent.AppendLine(newline);
                else
                    newcontent.Append(newline);
            }
            return newcontent.ToString();
        }

        /// <summary>
        /// Renumber object references in line
        /// </summary>
        /// <param name="line">Line from .DELTA file</param>
        /// <returns>New line (with renumbered object references)</returns>
        private string renumLine(string line)
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
                foreach (var obj in this.renumberList)
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
        private void renumTxt(ref string content, int from, int to)
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
        private void renumXml(ref string content, int from, int to)
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
        private string renumAl(string content)
        {
            var newcontent = new StringBuilder();
            // Handle different types of newline
            var lines = content.Replace("\r\n", "\r").Replace("\n", "\r").Split('\r');
            var withinML = false;
            foreach (var line in lines)
            {
                var newline = line;
                if (withinML)
                {
                    if (line.Trim().EndsWith("];"))
                        withinML = false;
                }
                else
                {
                    if ((line.Trim().StartsWith("CaptionML=[", StringComparison.InvariantCultureIgnoreCase)) ||
                        (line.Trim().StartsWith("ToolTipML=[", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        withinML = true;
                    }
                    else
                    {
                        newline = renumLine(line);
                    }
                }
                newcontent.AppendLine(newline);
            }
            return newcontent.ToString();
        }

        /// <summary>
        /// Renumber object references in launch.json file
        /// </summary>
        /// <param name="content">Content of the launch.json</param>
        private void renumJson(ref string content, int from, int to)
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
        private void ReplaceInString(ref string content, string searchForPattern, int from, int to)
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
        private bool IsNonNumeric(char ch)
        {
            return "0123456789".IndexOf(ch) < 0;
        }

        /// <summary>
        /// Renumber Object ID in filename
        /// </summary>
        /// <param name="fileName">Filename with Object ID (ex.COD52000.TXT)</param>
        /// <returns>Filename with new Object ID</returns>
        private string renumFileName(string fileName)
        {
            foreach (var obj in this.renumberList)
                ReplaceInString(ref fileName, string.Empty, obj.FromObjectId, obj.ToObjectId);
            return fileName;
        }
    }
}
