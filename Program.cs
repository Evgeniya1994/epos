using System;
using System.IO; 
using System.Collections.Generic; 
using System.Linq; 
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp; 
using Microsoft.CodeAnalysis.CSharp.Syntax;

struct Function
{
    public int len;
    public int nesting;
    public int lineIndex;
    public string nameFile;
    public Function(int len, int nesting, int lineIndex, string nameFile)
    {
        this.len = len;
        this.nesting = nesting;
        this.lineIndex = lineIndex;
        this.nameFile = nameFile;
    }
}
class Program
{
    static void Main(string[] args)
    {
        List<Function> functions = new List<Function>();
        try
        {
            var dir = String.Join(" ", args);
            foreach (var filePath in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                functions.AddRange(GetFunctions(filePath));
            }
        }
        catch (DirectoryNotFoundException e)
        {
            Console.WriteLine(e.Message);
        }
        catch (UnauthorizedAccessException e)
        {
            Console.WriteLine(e.Message);
        }
        catch (ArgumentException e)
        {
            Console.WriteLine(e.Message);
        }
        List<Function> top100Long = functions.OrderByDescending(x => x.len)
                                                .ThenBy(x => x.nameFile)
                                                .ThenBy(x => x.lineIndex)
                                                .Take(100)
                                                .ToList();
        List<Function> top100Nesting = functions.OrderByDescending(x => x.nesting)
                                                .ThenBy(x => x.nameFile)
                                                .ThenBy(x => x.lineIndex)
                                                .Take(100)
                                                .ToList();
        try
        {
            File.WriteAllLines("long.txt", top100Long.Select(x => x.len + "\t" + x.nameFile + ":" + x.lineIndex));
            File.WriteAllLines("nesting.txt", top100Nesting.Select(x => x.nesting + "\t" + x.nameFile + ":" + x.lineIndex));
        }
        catch (UnauthorizedAccessException e)
        {
            Console.WriteLine(e.Message);
        }
    }
    static int CountNesting(SyntaxNode node)
    {
        int nesting = 0;
        foreach (var child in node.ChildNodes())
        {
            if(!(child is BlockSyntax) && child is StatementSyntax) nesting = Math.Max(CountNesting(child) + 1, nesting);
            else nesting = Math.Max(CountNesting(child), nesting);
        }
        return nesting;
    }
    static List<Function> GetFunctions(string pathToFile)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseFile(pathToFile);
        var functions = tree
            .GetRoot()
            .DescendantNodes()
            .Where(x => (x.Parent is ClassDeclarationSyntax || x.Parent is StructDeclarationSyntax) &&
                !(x is ClassDeclarationSyntax) && !(x is StructDeclarationSyntax) && !(x is FieldDeclarationSyntax))
            .Select(x =>
                new Function(
                    x.DescendantNodes().OfType<StatementSyntax>().Where(x1 => !(x1 is BlockSyntax)).Count(),
                    CountNesting(x) - 1,
                    x.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    Path.GetFileName(pathToFile)))
            .Where(x => x.len > 0)
            .ToList();
        return functions;
    }
}