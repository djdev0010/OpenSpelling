#:package Microsoft.CodeAnalysis.CSharp@5.12.0
#:property ManagePackageVersionsCentrally=false

// Checks C# files against docs/CODE_STANDARDS.md.
// Usage: dotnet run scripts/CheckStandards.cs [file-or-folder ...]
// With no arguments it checks src/ and tests/. Exits 1 when anything is wrong.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

const int MaxClassLines = 250;
const int MaxMethodLines = 40;
const int MaxParameters = 5;
const int MaxNestingDepth = 3;

string[] vagueMethodNames = ["Handle", "Process", "Manage", "DoWork", "Run", "Execute", "Update", "Check", "Helper", "Flush", "Evaluate", "Do", "Go", "Clean", "Next", "Data"];
string[] vagueTypeSuffixes = ["Manager", "Helper", "Helpers", "Util", "Utils", "Utility"];
string[] bannedFolders = ["Models", "Services", "Helpers", "Utils", "Interfaces"];

var repoRoot = FindRepoRoot();
var targets = args.Length > 0 ? args : [Path.Combine(repoRoot, "src"), Path.Combine(repoRoot, "tests")];
var files = targets.SelectMany(ExpandToCSharpFiles).Distinct().ToList();
var problems = new List<string>();

foreach (var file in files)
{
    CheckFolderName(file);
    CheckFile(file);
    CheckArchitecture(file);
}
CheckProjectReferences();
CheckEveryGoalFolderHasItsService();

if (problems.Count == 0)
{
    Console.WriteLine($"Code standards: OK ({files.Count} files)");
    return 0;
}

Console.Error.WriteLine($"Code standards: {problems.Count} problem(s). See docs/CODE_STANDARDS.md");
foreach (var problem in problems) Console.Error.WriteLine("  " + problem);
return 1;

void CheckFolderName(string file)
{
    var folders = Path.GetRelativePath(repoRoot, Path.GetDirectoryName(file)!).Split(Path.DirectorySeparatorChar);
    foreach (var banned in bannedFolders.Where(folders.Contains))
        problems.Add($"{Relative(file)}: folder '{banned}/' is not allowed; organize by feature (§2)");
}

void CheckFile(string file)
{
    var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file);
    var root = tree.GetCompilationUnitRoot();

    var topLevelTypes = root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>()
        .Where(t => t.Parent is BaseNamespaceDeclarationSyntax or CompilationUnitSyntax).ToList();
    var delegates = root.DescendantNodes().OfType<DelegateDeclarationSyntax>()
        .Where(d => d.Parent is BaseNamespaceDeclarationSyntax or CompilationUnitSyntax).Count();
    if (topLevelTypes.Count + delegates > 1 && !IsClosedRecordUnion(topLevelTypes))
        problems.Add($"{Relative(file)}: {topLevelTypes.Count + delegates} top-level types; one per file (§2)");

    foreach (var type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
        CheckType(tree, file, type);
    foreach (var method in root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>())
        CheckMethod(tree, file, method);
    foreach (var local in root.DescendantNodes().OfType<LocalFunctionStatementSyntax>())
        CheckBodySize(tree, file, local.Identifier.Text, local);
}

// ---------- Architecture (§1): which layer a file is in, and what it may see ----------

// The project folder under src/ (SpellingBee.Domain, SpellingBee.Application, SpellingBee.Infrastructure.X, SpellingBee.App).
string? ProjectOf(string file)
{
    var parts = Path.GetRelativePath(repoRoot, file).Split(Path.DirectorySeparatorChar);
    return parts.Length > 2 && parts[0] == "src" ? parts[1] : null;
}

// Folders inside the project, e.g. ["Ports", "Speech"] for src/SpellingBee.Application/Ports/Speech/ISpeechVoice.cs.
string[] FoldersInProject(string file)
{
    var parts = Path.GetRelativePath(repoRoot, file).Split(Path.DirectorySeparatorChar);
    return parts.Length > 3 ? parts[2..^1] : [];
}

// The namespaces a file may import, by layer. App/Composition may use Application.Shared; only the
// platform heads (SpellingBee.App.Desktop, SpellingBee.App.iOS) may name Infrastructure.
bool MayUseNamespace(string project, string[] folders, string usedNamespace)
{
    bool Uses(string prefix) => usedNamespace == prefix || usedNamespace.StartsWith(prefix + ".");
    if (project == "SpellingBee.Domain") return Uses("SpellingBee.Domain");
    if (project == "SpellingBee.Application") return Uses("SpellingBee.Domain") || Uses("SpellingBee.Application");
    if (project.StartsWith("SpellingBee.Infrastructure."))
        return Uses("SpellingBee.Domain") || Uses("SpellingBee.Application.Ports") || Uses(project);
    if (project.StartsWith("SpellingBee.App.")) return true; // platform heads (Desktop, iOS) wire the adapters
    if (Uses("SpellingBee.Infrastructure")) return false;    // the shared app never names an adapter
    if (folders.FirstOrDefault() == "Composition") return true;
    return Uses("SpellingBee.Domain") || Uses("SpellingBee.App")
        || (Uses("SpellingBee.Application") && !Uses("SpellingBee.Application.Shared"));
}

void CheckArchitecture(string file)
{
    var project = ProjectOf(file);
    if (project is null) return;
    var folders = FoldersInProject(file);
    var root = CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file).GetCompilationUnitRoot();

    CheckNamespaceMatchesFolder(file, project, folders, root);
    foreach (var usedNamespace in root.Usings.Select(u => u.Name?.ToString() ?? "").Where(n => n.StartsWith("SpellingBee.")))
    {
        if (!MayUseNamespace(project, folders, usedNamespace))
            problems.Add($"{Relative(file)}: {project} may not use {usedNamespace} (§1 layers)");
    }
    if (project is "SpellingBee.Domain" or "SpellingBee.Application") CheckNoInputOutput(file, root);
    CheckTypePlacement(file, project, folders, root);
}

void CheckNamespaceMatchesFolder(string file, string project, string[] folders, CompilationUnitSyntax root)
{
    var expected = string.Join('.', new[] { project }.Concat(folders));
    var declared = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString();
    if (declared is not null && declared != expected)
        problems.Add($"{Relative(file)}: namespace {declared} should be {expected} (matches the folder, §2)");
}

// Domain and Application never touch the disk, the clock or the OS directly; they go through a port.
void CheckNoInputOutput(string file, CompilationUnitSyntax root)
{
    string[] banned = ["File", "Directory", "Path", "Environment", "Process", "DateTime.Now", "DateTime.UtcNow"];
    foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
    {
        var text = access.ToString();
        var hit = banned.FirstOrDefault(b => access.Expression.ToString() == b || text == b);
        if (hit is not null)
            problems.Add($"{Relative(file)}: uses {hit}; Domain and Application reach the outside only through Ports (§1)");
    }
}

// One home per kind of type: interfaces in Ports, "XxxService" only as the entry point of its goal folder
// (anywhere else the suffix is vague, §5).
void CheckTypePlacement(string file, string project, string[] folders, CompilationUnitSyntax root)
{
    var top = folders.FirstOrDefault();
    foreach (var type in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
    {
        var name = type.Identifier.Text;
        bool isInterface = type is InterfaceDeclarationSyntax;
        if (isInterface && !(project == "SpellingBee.Application" && top == "Ports") && !(project == "SpellingBee.App" && top == "Shared"))
            problems.Add($"{Relative(file)}: interface {name} belongs in SpellingBee.Application/Ports/ (§2)");
        if (name.EndsWith("Service") && !(project == "SpellingBee.Application" && folders.Length == 1 && name == top + "Service"))
            problems.Add($"{Relative(file)}: {name} must be SpellingBee.Application/<Name>/<Name>Service.cs (§2, §5)");
        if (project == "SpellingBee.Application" && top == "Ports" && type is ClassDeclarationSyntax c && !c.Modifiers.Any(SyntaxKind.StaticKeyword))
            problems.Add($"{Relative(file)}: Ports holds only interfaces and the data they carry; move class {name} to a service folder or Shared (§2)");
    }
}

void CheckEveryGoalFolderHasItsService()
{
    var goalFolders = Path.Combine(repoRoot, "src", "SpellingBee.Application");
    if (!Directory.Exists(goalFolders)) return;
    foreach (var folder in Directory.GetDirectories(goalFolders).Select(Path.GetFileName))
    {
        if (folder is null or "Ports" or "Shared" or "obj" or "bin") continue;
        if (!File.Exists(Path.Combine(goalFolders, folder, folder + "Service.cs")))
            problems.Add($"src/SpellingBee.Application/{folder}/: every goal folder has {folder}Service.cs (§2)");
    }
}

// Project references follow the layers: Domain ← Application ← Infrastructure ← App.
void CheckProjectReferences()
{
    foreach (var csproj in Directory.GetFiles(Path.Combine(repoRoot, "src"), "*.csproj", SearchOption.AllDirectories))
    {
        var project = Path.GetFileNameWithoutExtension(csproj);
        var allowed = project switch
        {
            "SpellingBee.Domain" => Array.Empty<string>(),
            "SpellingBee.Application" => ["SpellingBee.Domain"],
            _ when project.StartsWith("SpellingBee.Infrastructure.") => ["SpellingBee.Application"],
            "SpellingBee.App" => ["SpellingBee.Application"],
            _ => null,
        };
        if (allowed is null) continue;
        var referenced = System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(csproj), @"ProjectReference Include=""[^""]*?([\w.]+)\.csproj""")
            .Select(m => m.Groups[1].Value);
        foreach (var reference in referenced.Where(r => !allowed.Contains(r)))
            problems.Add($"{Relative(csproj)}: {project} may not reference {reference} (§1 layers)");
    }
}

// Allowed exception (§3): "// Signature fixed by <API>" above a method whose shape an external API dictates.
static bool HasFixedSignatureComment(SyntaxNode method) =>
    method.GetLeadingTrivia().Any(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) && t.ToString().Contains("Signature fixed by"));

// Allowed exception (§2): an abstract base record followed only by records that derive from it.
static bool IsClosedRecordUnion(List<BaseTypeDeclarationSyntax> types)
{
    if (types[0] is not RecordDeclarationSyntax baseRecord) return false;
    if (!baseRecord.Modifiers.Any(SyntaxKind.AbstractKeyword)) return false;
    return types.Skip(1).All(type =>
        type is RecordDeclarationSyntax { BaseList: { } bases }
        && bases.Types.Any(b => b.Type.ToString() == baseRecord.Identifier.Text));
}

void CheckType(SyntaxTree tree, string file, TypeDeclarationSyntax type)
{
    var name = type.Identifier.Text;
    int lines = LineCount(tree, type);
    if (lines > MaxClassLines)
        problems.Add($"{Location(tree, type)}: type '{name}' is {lines} lines (max {MaxClassLines}) (§3)");
    if (vagueTypeSuffixes.Any(s => name.EndsWith(s, StringComparison.Ordinal)))
        problems.Add($"{Location(tree, type)}: type '{name}' has a vague suffix; name its one job (§5)");
}

void CheckMethod(SyntaxTree tree, string file, BaseMethodDeclarationSyntax method)
{
    var name = method switch
    {
        MethodDeclarationSyntax m => m.Identifier.Text,
        ConstructorDeclarationSyntax c => c.Identifier.Text + " (constructor)",
        _ => method.Kind().ToString(),
    };

    CheckBodySize(tree, file, name, method);

    int parameters = method.ParameterList.Parameters.Count;
    if (parameters > MaxParameters && !HasFixedSignatureComment(method))
        problems.Add($"{Location(tree, method)}: '{name}' has {parameters} parameters (max {MaxParameters}) (§3)");

    if (method is MethodDeclarationSyntax named)
    {
        var bare = named.Identifier.Text.EndsWith("Async") ? named.Identifier.Text[..^5] : named.Identifier.Text;
        if (vagueMethodNames.Contains(bare))
            problems.Add($"{Location(tree, method)}: method name '{named.Identifier.Text}' is vague; say what it does (§5)");
    }
}

void CheckBodySize(SyntaxTree tree, string file, string name, SyntaxNode member)
{
    int lines = LineCount(tree, member);
    if (lines > MaxMethodLines)
        problems.Add($"{Location(tree, member)}: '{name}' is {lines} lines (max {MaxMethodLines}) (§3)");

    int depth = DeepestNesting(member);
    if (depth > MaxNestingDepth)
        problems.Add($"{Location(tree, member)}: '{name}' nests {depth} levels deep (max {MaxNestingDepth}) (§3)");
}

static int DeepestNesting(SyntaxNode member)
{
    int deepest = 0;
    foreach (var statement in member.DescendantNodes().OfType<StatementSyntax>().Where(IsNestingStatement))
    {
        int depth = statement.AncestorsAndSelf().TakeWhile(n => n != member).Count(IsNestingStatement);
        deepest = Math.Max(deepest, depth);
    }
    return deepest;
}

static bool IsNestingStatement(SyntaxNode node) =>
    node is IfStatementSyntax { Parent: not ElseClauseSyntax }
        or ForStatementSyntax or ForEachStatementSyntax or WhileStatementSyntax or DoStatementSyntax
        or SwitchStatementSyntax or TryStatementSyntax or UsingStatementSyntax { Statement: BlockSyntax }
        or LockStatementSyntax;

static int LineCount(SyntaxTree tree, SyntaxNode node)
{
    var span = tree.GetLineSpan(node.Span);
    return span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
}

string Location(SyntaxTree tree, SyntaxNode node) =>
    $"{Relative(tree.FilePath)}:{tree.GetLineSpan(node.Span).StartLinePosition.Line + 1}";

string Relative(string path) => Path.GetRelativePath(repoRoot, path);

static IEnumerable<string> ExpandToCSharpFiles(string target)
{
    if (File.Exists(target)) return target.EndsWith(".cs") ? [Path.GetFullPath(target)] : [];
    if (!Directory.Exists(target)) return [];
    return Directory.EnumerateFiles(target, "*.cs", SearchOption.AllDirectories)
        .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                 && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
        .Select(Path.GetFullPath);
}

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Directory.Build.props")))
        dir = dir.Parent;
    return dir?.FullName ?? Directory.GetCurrentDirectory();
}
