using System.Text;
using Fmod5Sharp;

/// <summary>
/// Mainly serves as an example of how to use fmod5sharp
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        var bankPath = args[0];
        var outPath = args.Length > 1 ? args[1] : $"{Path.GetFileNameWithoutExtension(bankPath)}-extracted";

        Console.WriteLine("Loading bank...");

        var bytes = File.ReadAllBytes(bankPath);

        var index = bytes.AsSpan().IndexOf(Encoding.ASCII.GetBytes("FSB5"));

        if (index > 0)
        {
            bytes = bytes.AsSpan(index).ToArray();
        }

        var bank = FsbLoader.LoadFsbFromByteArray(bytes);

        if (Directory.Exists(outPath))
        {
            Console.WriteLine("Removing existing output directory...");
            Directory.Delete(outPath, true);
        }

        var outDir = Directory.CreateDirectory(outPath);
        
        Console.WriteLine("Extracting...");
        var i = 0;
        foreach (var bankSample in bank.Samples)
        {
            i++;
            var name = bankSample.Name ?? $"sample-{i}";

            if(!bankSample.RebuildAsStandardFileFormat(out var data, out var extension))
            {
                Console.WriteLine($"Failed to extract sample {name} (format {bank.Header.AudioType})");
                continue;
            }
            
            var filePath = Path.Combine(outDir.FullName, $"{name}.{extension}");
            File.WriteAllBytes(filePath, data);
            Console.WriteLine($"Extracted sample {name}");
        }
    }
}