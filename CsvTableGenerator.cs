using System;
using System.Linq;
using Data.Tables;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class CsvTableGenerator : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var assetPath in importedAssets)
            {
                if (!assetPath.EndsWith("Table.csv")) continue;
                GenerateOrModifyTableClass(assetPath);
            }
        }

        private static void GenerateOrModifyTableClass(string assetPath)
        {
            Debug.Log($"Postprocess Table : {assetPath}");

            var csvTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            var lines = csvTextAsset.text.Trim().Split('\n');

            var types = lines[0].Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
            var headers = lines[1].Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
            Debug.Assert(types.Length == headers.Length, "types.Length == headers.Length");

            var className = csvTextAsset.name;
            var compilationUnit = SyntaxFactory.CompilationUnit();
            
            // namespace
            var namespaceDecl = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("Data.Tables"))
                    .NormalizeWhitespace();
            
            // 클래스 SkinTable 선언
            var tableClassDecl = SyntaxFactory.ClassDeclaration(className)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithBaseList(
                    SyntaxFactory.BaseList(
                        SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                            SyntaxFactory.SimpleBaseType(
                                SyntaxFactory.ParseTypeName($"TableBase<{className}.Row>")
                            )
                        )
                    )
                );
            
            // 생성자
            var constructorDecl = SyntaxFactory.ParseMemberDeclaration(@$"
public {className}(string csvStr) : base(csvStr)
{{
}}"
            );
            
            // 내부 클래스 Row 선언
            var rowClassDecl = SyntaxFactory.ClassDeclaration("Row")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithBaseList(
                    SyntaxFactory.BaseList(
                        SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                            SyntaxFactory.SimpleBaseType(
                                SyntaxFactory.ParseTypeName(nameof(RowBase))
                            )
                        )
                    )
                );
            
            // CSV의 타입과 필드명 정보를 기반으로 각 필드를 생성
            for (var i = 1; i < types.Length; i++)
            {
                // public {타입} {필드명};
                var fieldDeclaration = SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(
                            SyntaxFactory.ParseTypeName(types[i])
                        ).AddVariables(
                            SyntaxFactory.VariableDeclarator(headers[i])
                        )
                    )
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

                rowClassDecl = rowClassDecl.AddMembers(fieldDeclaration);
            }

            tableClassDecl = tableClassDecl.AddMembers(constructorDecl, rowClassDecl);
            namespaceDecl = namespaceDecl.AddMembers(tableClassDecl);
            compilationUnit = compilationUnit.AddMembers(namespaceDecl);
            
            var generatedCode = compilationUnit.NormalizeWhitespace().ToFullString();
            var savePath = $"Assets/Scripts/Data/Tables/{className}.cs";
            System.IO.File.WriteAllText(savePath, generatedCode);
            AssetDatabase.Refresh();
        }
    }
}
