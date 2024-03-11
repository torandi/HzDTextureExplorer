namespace HzdTextureCli;

using System;
using System.Linq;
using CommandLine;

public class Program
{
    public static void Main(string[] args)
    {
        var types = new Type[]
        {
                typeof(Commands.ReplaceTextureOptions),
                typeof(Commands.ExtractTextureOptions),
                typeof(Commands.ListTexturesOptions),
        };

        var parser = new Parser(with => with.HelpWriter = Console.Error);

        parser.ParseArguments(args, types)
            .WithParsed(Run)
            .WithNotParsed(errs => Console.WriteLine(errs.IsHelp()?"":errs.First()));
    }

    private static void Run(object options)
    {
        switch (options)
        {
            case Commands.ReplaceTextureOptions c:
                Commands.ReplaceTexture(c);
                break;

            case Commands.ExtractTextureOptions c:
                Commands.ExtractTexture(c);
                break;

            case Commands.ListTexturesOptions c:
                Commands.ListTextures(c);
                break;
        }
    }
}
