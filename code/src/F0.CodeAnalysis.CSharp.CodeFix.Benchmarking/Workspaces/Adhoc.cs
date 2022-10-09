using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace F0.CodeAnalysis.CSharp.Workspaces;

internal static class Adhoc
{
	private const string ProjectName = "BenchmarkProject";
	private const string LanguageName = LanguageNames.CSharp;
	private const string FileName = "Benchmark";
	private const string FileExtension = ".cs";

	internal static Document CreateDocument(string text, CSharpCompilationOptions? compilationOptions, CSharpParseOptions? parseOptions, IEnumerable<MetadataReference>? metadataReferences)
	{
		var texts = ImmutableArray.Create(text);
		Solution solution = CreateSolution(texts, compilationOptions, parseOptions, metadataReferences);

		Debug.Assert(solution.ProjectIds.Count == 1, $"Unexpectedly created {solution.ProjectIds.Count} {nameof(solution.Projects)}.");
		Project project = solution.Projects.Single();

		Debug.Assert(project.DocumentIds.Count == 1, $"Unexpectedly created {project.DocumentIds.Count} {nameof(project.Documents)}.");
		Document document = project.Documents.Single();

		return document;
	}

	internal static Project CreateProject(ImmutableArray<string> texts, CSharpCompilationOptions? compilationOptions, CSharpParseOptions? parseOptions, IEnumerable<MetadataReference>? metadataReferences)
	{
		Solution solution = CreateSolution(texts, compilationOptions, parseOptions, metadataReferences);

		Debug.Assert(solution.ProjectIds.Count == 1, $"Unexpectedly created {solution.ProjectIds.Count} {nameof(solution.Projects)}.");
		Project project = solution.Projects.Single();

		return project;
	}

	private static Solution CreateSolution(ImmutableArray<string> texts, CSharpCompilationOptions? compilationOptions, CSharpParseOptions? parseOptions, IEnumerable<MetadataReference>? metadataReferences)
	{
		using Workspace workspace = CreateWorkspace();

		ProjectId projectId = CreateProjectId();

		compilationOptions ??= new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
		parseOptions ??= new CSharpParseOptions(LanguageVersion.Default, DocumentationMode.Diagnose);
		metadataReferences ??= new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) };

		Solution solution = workspace
			.CurrentSolution
			.AddProject(projectId, ProjectName, ProjectName, LanguageName)
			.WithProjectCompilationOptions(projectId, compilationOptions)
			.WithProjectParseOptions(projectId, parseOptions)
			.WithProjectMetadataReferences(projectId, metadataReferences);

		for (int i = 0; i < texts.Length; i++)
		{
			string text = texts[i];
			string name = CreateFullFileName(0, i);
			DocumentId documentId = CreateDocumentId(projectId, name);

			solution = solution.AddDocument(documentId, name, text, null, name);
		}

		return solution;
	}

	private static Workspace CreateWorkspace()
		=> new AdhocWorkspace();

	private static ProjectId CreateProjectId()
		=> ProjectId.CreateNewId(ProjectName);

	private static DocumentId CreateDocumentId(ProjectId projectId, string? debugName)
		=> DocumentId.CreateNewId(projectId, debugName);

	private static string CreateFullFileName(int projectIndex, int documentIndex)
		=> $"/{projectIndex}/{FileName}{documentIndex}{FileExtension}";
}
