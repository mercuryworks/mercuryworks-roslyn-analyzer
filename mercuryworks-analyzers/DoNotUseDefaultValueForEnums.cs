using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mercuryworks_analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DoNotUseDefaultValueForEnums : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DoNotUseDefaultValueForEnums";
        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.DoNotUseDefaultValueForEnumsTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.DoNotUseDefaultValueForEnumsFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.DoNotUseDefaultValueForEnumsDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.EnumMemberDeclaration);
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            var declaration = context.Node as EnumMemberDeclarationSyntax;
            var doesNotHaveEqualsValue = declaration.EqualsValue == null;

            //shortcut value checking if not explicitly set to value
            var shouldTriggerRule = doesNotHaveEqualsValue || (int)(declaration.EqualsValue.Value as LiteralExpressionSyntax).Token.Value == 0;

            if (shouldTriggerRule)
                context.ReportDiagnostic(
                        Diagnostic.Create(Rule, declaration.GetLocation(), declaration.Identifier.Value)
                    );
        }
    }
}
