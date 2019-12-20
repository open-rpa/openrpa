////
//// EmptyStatementIssueTests.cs
////
//// Author:
////       Mike Krüger <mkrueger@xamarin.com>
////
//// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
////
//// Permission is hereby granted, free of charge, to any person obtaining a copy
//// of this software and associated documentation files (the "Software"), to deal
//// in the Software without restriction, including without limitation the rights
//// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//// copies of the Software, and to permit persons to whom the Software is
//// furnished to do so, subject to the following conditions:
////
//// The above copyright notice and this permission notice shall be included in
//// all copies or substantial portions of the Software.
////
//// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//// THE SOFTWARE.
//using System;
//using System.Linq;
////using NUnit.Framework;
//using Microsoft.CodeAnalysis.Diagnostics;
//using Microsoft.CodeAnalysis.CSharp;
//using System.Collections.Generic;
//using Microsoft.CodeAnalysis;
//using System.Threading;
//using Microsoft.CodeAnalysis.CodeFixes;
//using Microsoft.CodeAnalysis.Text;
//using Microsoft.CodeAnalysis.Host.Mef;
//using System.Reflection;
//using System.Text;
//using Microsoft.CodeAnalysis.Host;
//using Microsoft.CodeAnalysis.CodeActions;
//namespace OpenRPA.Script.Activities
//{


//	public class WorkspaceController : IWorkspaceService
//	{

//	}
//	public class InspectionActionTestBase
//	{
//		static MetadataReference mscorlib;
//		static MetadataReference systemAssembly;
//		//        static MetadataReference systemXmlLinq;
//		static MetadataReference systemCore;

//		internal static MetadataReference[] DefaultMetadataReferences;

//		static Dictionary<string, CodeFixProvider> providers = new Dictionary<string, CodeFixProvider>();

//		static InspectionActionTestBase()
//		{
//			try
//			{
//				mscorlib = MetadataReference.CreateFromFile(typeof(Console).Assembly.Location);
//				systemAssembly = MetadataReference.CreateFromFile(typeof(System.Text.RegularExpressions.Regex).Assembly.Location);
//				//systemXmlLinq = MetadataReference.CreateFromFile (typeof(System.Xml.Linq.XElement).Assembly.Location);
//				systemCore = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
//				DefaultMetadataReferences = new[] {
//					mscorlib,
//					systemAssembly,
//					systemCore,
//					//systemXmlLinq
//				};
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e);
//			}
//		}

//		public static string GetUniqueName()
//		{
//			return Guid.NewGuid().ToString("D");
//		}

//		public static CSharpCompilation CreateCompilation(
//			IEnumerable<SyntaxTree> trees,
//			IEnumerable<MetadataReference> references = null,
//			CSharpCompilationOptions compOptions = null,
//			string assemblyName = "")
//		{
//			if (compOptions == null)
//			{
//				compOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, false, "a.dll");
//			}

//			return CSharpCompilation.Create(
//				string.IsNullOrEmpty(assemblyName) ? GetUniqueName() : assemblyName,
//				trees,
//				references,
//				compOptions);
//		}


//		public static CSharpCompilation CreateCompilationWithMscorlib(
//			IEnumerable<SyntaxTree> source,
//			IEnumerable<MetadataReference> references = null,
//			CSharpCompilationOptions compOptions = null,
//			string assemblyName = "")
//		{
//			var refs = new List<MetadataReference>();
//			if (references != null)
//			{
//				refs.AddRange(references);
//			}

//			refs.AddRange(DefaultMetadataReferences);

//			return CreateCompilation(source, refs, compOptions, assemblyName);
//		}

//		internal class TestWorkspace : Workspace
//		{
//			public TestWorkspace() : base(CompositionManager.Instance.HostServices, ServiceLayer.Default)
//			{
//			}

//			public void ChangeDocument(DocumentId id, SourceText text)
//			{
//				ApplyDocumentTextChanged(id, text);
//			}

//			protected override void ApplyDocumentTextChanged(DocumentId id, SourceText text)
//			{
//				base.ApplyDocumentTextChanged(id, text);
//				var document = CurrentSolution.GetDocument(id);
//				if (document != null)
//					OnDocumentTextChanged(id, text, PreservationMode.PreserveValue);
//			}

//			public override bool CanApplyChange(ApplyChangesKind feature)
//			{
//				return true;
//			}

//			public void Open(ProjectInfo projectInfo)
//			{
//				var sInfo = SolutionInfo.Create(
//								SolutionId.CreateNewId(),
//								VersionStamp.Create(),
//								null,
//								new[] { projectInfo }
//							);
//				OnSolutionAdded(sInfo);
//			}
//		}

//		static void RunFix(Workspace workspace, ProjectId projectId, DocumentId documentId, Diagnostic diagnostic, int index = 0)
//		{
//			CodeFixProvider provider;
//			if (providers.TryGetValue(diagnostic.Id, out provider))
//			{
//				//Assert.IsNotNull(provider, "null provider for : " + diagnostic.Id);
//				var document = workspace.CurrentSolution.GetProject(projectId).GetDocument(documentId);
//				var actions = new List<CodeAction>();
//				var context = new CodeFixContext(document, diagnostic, (fix, diags) => actions.Add(fix), default(CancellationToken));
//				provider.RegisterCodeFixesAsync(context).Wait();
//				if (!actions.Any())
//				{
//					//Assert.Fail("Provider has no fix for " + diagnostic.Id + " at " + diagnostic.Location.SourceSpan);
//					return;
//				}
//				foreach (var op in actions[index].GetOperationsAsync(default(CancellationToken)).Result)
//				{
//					op.Apply(workspace, default(CancellationToken));
//				}
//			}
//			else
//			{
//				//Assert.Fail("No code fix provider found for :" + diagnostic.Id);
//			}
//		}
//		static TextSpan GetWholeSpan(Diagnostic d)
//		{
//			int start = d.Location.SourceSpan.Start;
//			int end = d.Location.SourceSpan.End;
//			foreach (var a in d.AdditionalLocations)
//			{
//				start = Math.Min(start, a.SourceSpan.Start);
//				end = Math.Max(start, a.SourceSpan.End);
//			}
//			return TextSpan.FromBounds(start, end);
//		}

//		protected static void Analyze<T>(string input, string output = null, int issueToFix = -1, int actionToRun = 0, Action<int, Diagnostic> diagnosticCheck = null) where T : DiagnosticAnalyzer, new()
//		{
//			var text = new StringBuilder();

//			var expectedDiagnosics = new List<TextSpan>();
//			int start = -1;
//			for (int i = 0; i < input.Length; i++)
//			{
//				char ch = input[i];
//				if (ch == '$')
//				{
//					if (start < 0)
//					{
//						start = text.Length;
//						continue;
//					}
//					expectedDiagnosics.Add(TextSpan.FromBounds(start, text.Length));
//					start = -1;
//				}
//				else
//				{
//					text.Append(ch);
//				}
//			}

//			var syntaxTree = CSharpSyntaxTree.ParseText(text.ToString());

//			Compilation compilation = CreateCompilationWithMscorlib(new[] { syntaxTree });

//			var diagnostics = new List<Diagnostic>();

//			var compilationWithAnalyzers = compilation.WithAnalyzers(System.Collections.Immutable.ImmutableArray<DiagnosticAnalyzer>.Empty.Add(new T()));
//			var result = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
//			diagnostics.AddRange(result);

//			if (expectedDiagnosics.Count != diagnostics.Count)
//			{
//				foreach (var diag in diagnostics)
//				{
//					Console.WriteLine(diag.Id + "/" + diag.GetMessage() + "/" + diag.Location.SourceSpan);
//				}
//				Assert.Fail("Diagnostic count mismatch expected: " + expectedDiagnosics.Count + " was " + diagnostics.Count);
//			}

//			for (int i = 0; i < expectedDiagnosics.Count; i++)
//			{
//				var d = diagnostics[i];
//				var wholeSpan = GetWholeSpan(d);
//				if (wholeSpan != expectedDiagnosics[i])
//				{
//					Assert.Fail("Diagnostic " + i + " span mismatch expected: " + expectedDiagnosics[i] + " but was " + wholeSpan);
//				}
//				if (diagnosticCheck != null)
//					diagnosticCheck(i, d);
//			}

//			if (output == null)
//				return;

//			var workspace = new TestWorkspace();
//			var projectId = ProjectId.CreateNewId();
//			var documentId = DocumentId.CreateNewId(projectId);
//			workspace.Open(ProjectInfo.Create(
//				projectId,
//				VersionStamp.Create(),
//				"a", "a.exe", LanguageNames.CSharp, null, null, null, null,
//				new[] {
//					DocumentInfo.Create(
//						documentId,
//						"a.cs",
//						null,
//						SourceCodeKind.Regular,
//						TextLoader.From(TextAndVersion.Create(SourceText.From(text.ToString()), VersionStamp.Create())))
//				}
//			));
//			if (issueToFix < 0)
//			{
//				diagnostics.Reverse();
//				foreach (var v in diagnostics)
//				{
//					RunFix(workspace, projectId, documentId, v);
//				}
//			}
//			else
//			{
//				RunFix(workspace, projectId, documentId, diagnostics.ElementAt(issueToFix), actionToRun);
//			}

//			var txt = workspace.CurrentSolution.GetProject(projectId).GetDocument(documentId).GetTextAsync().Result.ToString();
//			if (output != txt)
//			{
//				Console.WriteLine("expected:");
//				Console.WriteLine(output);
//				Console.WriteLine("got:");
//				Console.WriteLine(txt);
//				Console.WriteLine("-----Mismatch:");
//				for (int i = 0; i < txt.Length; i++)
//				{
//					if (i >= output.Length)
//					{
//						Console.Write("#");
//						continue;
//					}
//					if (txt[i] != output[i])
//					{
//						Console.Write("#");
//						continue;
//					}
//					Console.Write(txt[i]);
//				}
//				Assert.Fail();
//			}
//		}

//		protected static void AnalyzeWithRule<T>(string input, string ruleId, string output = null, int issueToFix = -1, int actionToRun = 0, Action<int, Diagnostic> diagnosticCheck = null) where T : DiagnosticAnalyzer, new()
//		{
//			var text = new StringBuilder();

//			var expectedDiagnosics = new List<TextSpan>();
//			int start = -1;
//			for (int i = 0; i < input.Length; i++)
//			{
//				char ch = input[i];
//				if (ch == '$')
//				{
//					if (start < 0)
//					{
//						start = text.Length;
//						continue;
//					}
//					expectedDiagnosics.Add(TextSpan.FromBounds(start, text.Length));
//					start = -1;
//				}
//				else
//				{
//					text.Append(ch);
//				}
//			}

//			var syntaxTree = CSharpSyntaxTree.ParseText(text.ToString());

//			Compilation compilation = CreateCompilationWithMscorlib(new[] { syntaxTree });

//			var diagnostics = new List<Diagnostic>();
//			var compilationWithAnalyzers = compilation.WithAnalyzers(System.Collections.Immutable.ImmutableArray<DiagnosticAnalyzer>.Empty.Add(new T()));
//			diagnostics.AddRange(compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result);


//			if (expectedDiagnosics.Count != diagnostics.Count)
//			{
//				Console.WriteLine("Diagnostics: " + diagnostics.Count);
//				foreach (var diag in diagnostics)
//				{
//					Console.WriteLine(diag.Id + "/" + diag.GetMessage());
//				}
//				Assert.Fail("Diagnostic count mismatch expected: " + expectedDiagnosics.Count + " but was:" + diagnostics.Count);
//			}

//			for (int i = 0; i < expectedDiagnosics.Count; i++)
//			{
//				var d = diagnostics[i];
//				var wholeSpan = GetWholeSpan(d);
//				if (wholeSpan != expectedDiagnosics[i])
//				{
//					Assert.Fail("Diagnostic " + i + " span mismatch expected: " + expectedDiagnosics[i] + " but was " + wholeSpan);
//				}
//				if (diagnosticCheck != null)
//					diagnosticCheck(i, d);
//			}

//			if (output == null)
//				return;

//			var workspace = new TestWorkspace();
//			var projectId = ProjectId.CreateNewId();
//			var documentId = DocumentId.CreateNewId(projectId);
//			workspace.Open(ProjectInfo.Create(
//				projectId,
//				VersionStamp.Create(),
//				"", "", LanguageNames.CSharp, null, null, null, null,
//				new[] {
//					DocumentInfo.Create(
//						documentId,
//						"a.cs",
//						null,
//						SourceCodeKind.Regular,
//						TextLoader.From(TextAndVersion.Create(SourceText.From(text.ToString()), VersionStamp.Create())))
//				}
//			));
//			if (issueToFix < 0)
//			{
//				diagnostics.Reverse();
//				foreach (var v in diagnostics)
//				{
//					RunFix(workspace, projectId, documentId, v);
//				}
//			}
//			else
//			{
//				RunFix(workspace, projectId, documentId, diagnostics.ElementAt(issueToFix), actionToRun);
//			}

//			var txt = workspace.CurrentSolution.GetProject(projectId).GetDocument(documentId).GetTextAsync().Result.ToString();
//			if (output != txt)
//			{
//				Console.WriteLine("expected:");
//				Console.WriteLine(output);
//				Console.WriteLine("got:");
//				Console.WriteLine(txt);
//				//Assert.Fail();
//			}
//		}
//	}
//}
