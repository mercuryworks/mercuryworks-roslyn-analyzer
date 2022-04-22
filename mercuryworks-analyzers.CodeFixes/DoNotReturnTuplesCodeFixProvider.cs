using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace mercuryworks_analyzers

{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DoNotReturnTuplesCodeFixProvider)), Shared]
    public class DoNotReturnTuplesCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(DoNotReturnTuplesAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.DoNotReturnTuplesTitle,
                    createChangedDocument: c => CreateClassFromTuple(context.Document, declaration, c),
                    equivalenceKey: nameof(CodeFixResources.DoNotReturnTuplesTitle)
                    ),
                diagnostic);
        }


        private async Task<Document> CreateClassFromTuple(Document document, MethodDeclarationSyntax declaration, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken);

            ClassDeclarationSyntax classDeclaration = GenerateClassFromTuple(declaration);

            var typeSyntax = SyntaxFactory.IdentifierName(classDeclaration.Identifier);

            editor.InsertAfter(declaration.Parent, classDeclaration.WithAdditionalAnnotations(Formatter.Annotation));

            editor.ReplaceNode(declaration.ReturnType, typeSyntax.WithAdditionalAnnotations(Formatter.Annotation));

            // interfaces have a null body for method declaration
            if (declaration.Body != null)
            {
                var returnStatement = declaration.Body?.ChildNodes().OfType<ReturnStatementSyntax>().First();
                var args = GenerateArgsFromTupleReturn(returnStatement, classDeclaration);
                var holder = SyntaxFactory.ObjectCreationExpression(typeSyntax)
                    .WithInitializer(SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression, args))
                    .NormalizeWhitespace();
                editor.ReplaceNode(returnStatement.Expression, holder.WithAdditionalAnnotations(Formatter.Annotation));
            }

            return editor.GetChangedDocument();
        }

        private static ClassDeclarationSyntax GenerateClassFromTuple(MethodDeclarationSyntax declaration)
        {
            var classDeclaration = SyntaxFactory.ClassDeclaration($"{declaration.Identifier.Text}DTO")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            var typesInTuple = declaration.ReturnType.DescendantNodesAndSelf().OfType<TypeArgumentListSyntax>().FirstOrDefault()?.Arguments;

            SeparatedSyntaxList<MemberDeclarationSyntax> propertyDeclarations; 

            if (typesInTuple == null && declaration.ReturnType is TupleTypeSyntax tupleTypeSyntax)
            {
                propertyDeclarations = GeneratePropertiesFromTupleTypeSyntax(tupleTypeSyntax);

                classDeclaration = classDeclaration.AddMembers(propertyDeclarations.ToArray());
            }
            else
            {
                int index = 1;
                foreach (var type in typesInTuple)
                {
                    MemberDeclarationSyntax propertyDeclaration = null;
                    if (type is IdentifierNameSyntax identifierName)
                    {
                        propertyDeclaration = SyntaxFactory.PropertyDeclaration(SyntaxFactory.IdentifierName(identifierName.Identifier), $"Item{index}")
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                            .AddAccessorListAccessors(
                                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                                SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
                    }
                    else if (type is PredefinedTypeSyntax predefinedType)
                    {
                        propertyDeclaration = SyntaxFactory.PropertyDeclaration(SyntaxFactory.PredefinedType(predefinedType.Keyword), $"Item{index}")
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                            .AddAccessorListAccessors(
                                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                                SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
                    }
                    ++index;
                    classDeclaration = classDeclaration.AddMembers(propertyDeclaration);
                }
            }

            return classDeclaration;
        }

        private static SeparatedSyntaxList<MemberDeclarationSyntax> GeneratePropertiesFromTupleTypeSyntax(TupleTypeSyntax tupleTypeSyntax)
        {
            return new SeparatedSyntaxList<MemberDeclarationSyntax>().AddRange(
                tupleTypeSyntax.Elements.Select(t =>
                {
                    switch (t.Type)
                    {
                        case PredefinedTypeSyntax predefined:
                            return SyntaxFactory.PropertyDeclaration(SyntaxFactory.PredefinedType(predefined.Keyword), t.Identifier.Text)
                                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                                .AddAccessorListAccessors(
                                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
                        case IdentifierNameSyntax identifier:
                            return SyntaxFactory.PropertyDeclaration(SyntaxFactory.IdentifierName(identifier.Identifier), t.Identifier.Text)
                                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                                .AddAccessorListAccessors(
                                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
                        default:
                            throw new NotImplementedException();
                    }
                }));
        }

        private static SeparatedSyntaxList<ExpressionSyntax> GenerateArgsFromTupleReturn(ReturnStatementSyntax current, ClassDeclarationSyntax classDeclaration)
        {
            var ssList = SyntaxFactory.SeparatedList<ExpressionSyntax>();
            if (current is null) return ssList;

            var argList = SyntaxFactory.SeparatedList<ArgumentSyntax>();
            if (current.Expression is TupleExpressionSyntax tupleSyntax)
            {
                argList = tupleSyntax.Arguments;
            } 
            else if (current.Expression is InvocationExpressionSyntax invocationSyntax)
            {
                argList = invocationSyntax.ArgumentList.Arguments;
            }

            var index = 1;
            foreach (var item in argList)
            {
                var identifierName = (classDeclaration.Members[index - 1] as PropertyDeclarationSyntax).Identifier.Text;
                ssList = ssList.Add(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName(identifierName), item.Expression));
                ++index;
            }
            return ssList;
        }
    }
}