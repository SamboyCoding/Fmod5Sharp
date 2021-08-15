using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

//Intending for parsing https://github.com/HearthSim/python-fsb5/blob/master/fsb5/vorbis_headers.py
namespace HeaderGenerator
{
    public class Program
    {
        private const string FILE_START = "lookup = {";
        private const string SEARCH_TERM = "',\n";
        private const string DUMMY_HEX_ESCAPE = @"\x00";
        private static readonly Regex EscapeRegex = new Regex(@"\\x([a-f0-9]{2})", RegexOptions.Compiled);
        
        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Need to provide path to vorbis_headers.py as an arg");
                return;
            }
            
            var fileContent = File.ReadAllText(args[0]);
            fileContent = fileContent[FILE_START.Length..];

            //Each entry should be of the format "{id}: b'first line'\nb'second line'\n...\nb'last line
            //Note there is no closing quote on the last line as it is part of the search term.
            var entries = fileContent.Split(SEARCH_TERM);

            var result = new Dictionary<uint, byte[]>();
            foreach (var entry in entries)
            {
                var colonPos = entry.IndexOf(':');
                var num = entry[..colonPos];
                var body = entry[(colonPos + 1)..].Trim().TrimEnd('}');

                var contentStringBuilder = new StringBuilder();
                foreach (var line in body.Split("\n"))
                {
                    var trimmed = line.Trim();

                    if (trimmed.StartsWith("b"))
                        trimmed = trimmed[2..];
                    if (trimmed.EndsWith("'") || trimmed.EndsWith('"'))
                        trimmed = trimmed[..^1];

                    contentStringBuilder.Append(trimmed);
                }

                var contentString = contentStringBuilder.ToString();

                contentString = contentString.Replace("\\n", "\n")
                    .Replace("\\t", "\t")
                    .Replace("\\r", "\r")
                    .Replace("\\0", "\0")
                    .Replace("\\'", "\'")
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\");
                
                var matches = EscapeRegex.Matches(contentString);

                var offsetDict = new Dictionary<int, byte>();
                foreach (Match match in matches)
                {
                    var hexString = match.Groups[1].Value;
                    var charValue = byte.Parse(hexString, NumberStyles.HexNumber);
                    offsetDict[match.Index] = charValue;
                }

                List<byte> headerBytes = new List<byte>();
                for (var i = 0; i < contentString.Length; i++)
                {
                    if (offsetDict.ContainsKey(i))
                    {
                        headerBytes.Add(offsetDict[i]);
                        i += DUMMY_HEX_ESCAPE.Length - 1;
                        continue;
                    }
                    
                    headerBytes.Add((byte) contentString[i]);
                }

                result[uint.Parse(num)] = headerBytes.ToArray();
            }

            var jsonValue = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            File.WriteAllText("vorbis_headers.json", jsonValue);
        }
    }
}