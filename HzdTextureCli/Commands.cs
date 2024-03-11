namespace HzdTextureCli;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Threading;
using CommandLine;
using HzdTextureLib;
using SixLabors.ImageSharp.ColorSpaces.Conversion;

public class Commands
{
    private static HzDCore m_core = null;

    public class CommonOptions
    {
        [Option('c', "core-file", Required = true, HelpText = "OS path of texture core (.core) file.")]
        public string CoreFile { get; set; }

        [Option('q', "quiet", HelpText = "Do not print extra information to the console, such as files being extracted.")]
        public bool Quiet { get; set; }
    }

    [Verb("replace", HelpText = "Replace texture in core file.")]
    public class ReplaceTextureOptions : CommonOptions
    {
        [Option('i', "input", Required = true, HelpText = "OS input path or list of texture files.")]
        public string InputPath { get; set; }
        [Value(0, HelpText = "List of textures to be replaced. If empty all will be replaced.")]
        public IEnumerable<string> ReplaceList { get; set; }
    }

    [Verb("extract", HelpText = "Extract texture from core file.")]
    public class ExtractTextureOptions : CommonOptions
    {
        [Option('o', "output", Required = true, HelpText = "OS output directory for texture files.")]
        public string OutputPath { get; set; }

        [Option('f', "format", Default = "dds", HelpText = "Image format to stored exported texture.")]
        public string OutputFormat { get; set; }
        [Value(0, HelpText = "List of textures to be extracted. If empty all will be extracted.")]
        public IEnumerable<string> ExtractList { get; set; }
    }

    [Verb("list", HelpText = "List textures in core file.")]
    public class ListTexturesOptions : CommonOptions
    {
        [Option('s', "short", HelpText = "List only texture names.")]
        public bool ShortListing { get; set; }
    }

    public static void ReplaceTexture(ReplaceTextureOptions options)
    {
        string file = options.InputPath;
        var images = LoadCoreFile(options.CoreFile, options);
        List<string> textures = options.ReplaceList.ToList();
        bool found = false;

        if (Directory.Exists(options.InputPath))
        {
            if (textures.Count() == 0) {
                textures = images.Select(t => t.Name).ToList();
            }
        }
        else
        {
            if (textures.Count() == 0)
            {
                textures.Add(Path.GetFileNameWithoutExtension(file));
            }
            else if (textures.Count() > 1)
            {
                Console.Error.WriteLine($"Texture mapping for {file} is not explicit.");
                Environment.Exit(1);
            }
        }

        foreach (ITexture tex in images)
        {
            if (!textures.Contains(tex.Name)) continue;
            found = true;

            if (Directory.Exists(options.InputPath))
                file = Path.Combine(options.InputPath,$"{tex.Name}.dds");

            if (!options.Quiet)
            {
                Console.WriteLine("Replace {0} with {1}.", tex.Name, file);
            }
            UpdateTexture(tex, file);
        }
        if (!found)
        {
            Console.Error.WriteLine("No matching texture to replace found.");
            Environment.Exit(1);
        }
    }

    private static void UpdateTexture(ITexture tex, string file)
        {
            try
            {
                tex.UpdateImageData(file);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"An error occured while updating texture {tex.Name}: {ex.Message}");
                Environment.Exit(1);
            }
        }
    public static void ExtractTexture(ExtractTextureOptions options)
    {
        var format = options.OutputFormat;
        var images = LoadCoreFile(options.CoreFile, options);
        var textures = options.ExtractList.Count() > 0 ? options.ExtractList : images.Select(t => t.Name);

        if (!Directory.Exists(options.OutputPath))
        {
            Console.Error.WriteLine($"Directory {options.OutputPath} does not exists.");
            Environment.Exit(1);
        }

        foreach (ITexture tex in images)
        {
            string file = Path.Combine(options.OutputPath, $"{tex.Name}.{format}");

            if (!textures.Contains(tex.Name)) continue;

            if (!options.Quiet)
            {
                Console.WriteLine("Exporting {0} -> {1}", tex.Name, file);
            }
            ExportTexture(file, format, tex);
        }
    }

    public static void ListTextures(ListTexturesOptions options)
    {
        var Images = LoadCoreFile(options.CoreFile, options);
        foreach (ITexture tex in Images)
        {
            Console.WriteLine(tex.Name);
            if (options.ShortListing) continue;
            foreach (var item in tex.Info)
            {
                Console.WriteLine($"    {item.Title}: {item.Value}");
            }
        }
    }

    private static void ExportTexture(string file, string format, ITexture tex)
    {
        if (format == "dds")
        {
            tex.WriteDds(file);
        }
        else if(format == "png")
        {
            tex.WritePng(file);
        }
        else if(format == "tga")
        {
            tex.WriteTga(file);
        }
        else
        {
            Console.Error.WriteLine($"Unknown file extension {format}");
            Environment.Exit(1);
        }
    }

    private static IList<ITexture> LoadCoreFile(String path, CommonOptions options)
    {
        IList<ITexture> Images = new List<ITexture>();

        string ext = Path.GetExtension(path);
        try
        {
            m_core = new HzDCore(path);

            if (!options.Quiet)
            {
                Console.WriteLine($"Loaded {path}");
            }

            foreach (ITexture tex in m_core.Textures)
            {
                Images.Add(tex);
            }

            foreach (UITexture uitex in m_core.UITextures)
            {
                foreach (ITexture tex in uitex.TextureItems)
                {
                    Images.Add(tex);
                }
            }

        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"An error occured while loading: {e.Message}");
            Environment.Exit(1);
        }
        return Images;
    }
}
