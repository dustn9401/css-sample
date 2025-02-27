using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.TextCore;

namespace TMPUtil.Editor
{
    public class TextSpriteUtilityWindow : EditorWindow
    {
       private List<string> PngFiles { get; } = new();
       private Vector2 _scrollPos;
       private SpriteDataProviderFactories _providerFactory;
       private TMP_SpriteAsset _asset;
       private bool _preserveScale = true;
       private bool _preserveMetrics = true;

       [MenuItem("Tools/Text Sprite Utility Window")]
       private static void ShowWindow()
       {
          GetWindow<TextSpriteUtilityWindow>();
       }

       private void OnEnable()
       {
          _providerFactory = new SpriteDataProviderFactories();
          _providerFactory.Init();
       }

       private void OnGUI()
       {
          EditorGUILayout.LabelField("Combine Sprites");
          if (GUILayout.Button("Select Directory"))
          {
             var dir = EditorUtility.OpenFolderPanel("Select Addressable Asset", GetSelectedPathOrFallback(), "");
             if (!string.IsNullOrWhiteSpace(dir))
             {
                var directoryAssetPath = FileUtil.GetProjectRelativePath(dir);

                PngFiles.Clear();
                foreach (var file in Directory.GetFiles(directoryAssetPath))
                {
                   if (Path.GetExtension(file) != ".png")
                   {
                      continue;
                   }

                   PngFiles.Add(file);
                }
             }
          }

          if (PngFiles.Count > 0)
          {
             GUILayout.Label("PNG file list:", EditorStyles.wordWrappedLabel);
             _scrollPos = GUILayout.BeginScrollView(_scrollPos);
             for (var i = 0; i < PngFiles.Count; i++)
             {
                var fName = PngFiles[i];
                GUILayout.Label($"#{i + 1}\t{fName}");
             }
             GUILayout.EndScrollView();

             if (GUILayout.Button("Combine"))
             {
                var savedPath = ExecuteCombine(PngFiles);
                if (!string.IsNullOrWhiteSpace(savedPath))
                {
                   Debug.Log($"file saved: {savedPath}");
                }
             }
          }

          EditorGUILayout.Space(20);
          EditorGUILayout.LabelField("TMP_SpriteAsset Cleanup");
          _asset = EditorGUILayout.ObjectField(_asset, typeof(TMP_SpriteAsset), false) as TMP_SpriteAsset;
          _preserveScale = EditorGUILayout.Toggle("preserve scale", _preserveScale);
          _preserveMetrics = EditorGUILayout.Toggle("preserve metrics", _preserveMetrics);


          EditorGUI.BeginDisabledGroup(!_asset);
          if (GUILayout.Button("Cleanup"))
          {
             // ReSharper disable once PossibleNullReferenceException
             var prevMap = _asset.spriteCharacterTable.ToDictionary(x => x.name, x => x.glyph);

             _asset.spriteCharacterTable.Clear();
             _asset.spriteGlyphTable.Clear();

             var spritePath = AssetDatabase.GetAssetPath(_asset.spriteSheet);
             var importer = AssetImporter.GetAtPath(spritePath);
             var provider = _providerFactory.GetSpriteEditorDataProviderFromObject(importer);
             provider.InitSpriteEditorDataProvider();

             var spriteRects = provider.GetSpriteRects();
             const float yOffsetConst = .78f;

             for (var i = 0; i < spriteRects.Length; i++)
             {
                var spriteRect = spriteRects[i];
                var metrics = new GlyphMetrics(spriteRect.rect.width, spriteRect.rect.height, 0, spriteRect.rect.height * yOffsetConst, spriteRect.rect.width);
                var spriteGlyph = new TMP_SpriteGlyph((uint)i, metrics, new GlyphRect(spriteRect.rect), 1f, 0);

                // 기존에 존재하던 항목의 스케일 등 설정값을 유지해줘야 함
                if (prevMap.TryGetValue(spriteRect.name, out var existingGlyph))
                {
                   if (_preserveScale)
                   {
                      spriteGlyph.scale = existingGlyph.scale;
                   }

                   if (_preserveMetrics)
                   {
                      spriteGlyph.metrics = existingGlyph.metrics;
                   }
                }
                _asset.spriteGlyphTable.Add(spriteGlyph);

                var spriteCharacter = new TMP_SpriteCharacter(0xFFFE, _asset, spriteGlyph) { name = spriteRect.name, scale = 1.0f };
                _asset.spriteCharacterTable.Add(spriteCharacter);
             }

             _asset.UpdateLookupTables();
             EditorUtility.SetDirty(_asset);
             AssetDatabase.SaveAssetIfDirty(_asset);
             Debug.Log(string.Join('\n', spriteRects.Select((x, i) => $"#{i}\t[<sprite name={x.name}>] ({x.name})")));
          }
          EditorGUI.EndDisabledGroup();
       }

       private static string GetSelectedPathOrFallback()
       {
          var path = "Assets";
          var obj = Selection.activeObject;

          if (obj != null)
          {
             path = AssetDatabase.GetAssetPath(obj);

             if (!string.IsNullOrEmpty(path) && !AssetDatabase.IsValidFolder(path))
             {
                path = System.IO.Path.GetDirectoryName(path);
             }
          }

          return path;
       }

       private string ExecuteCombine(List<string> fileNames)
       {
          var textures = new List<Texture2D>();
          foreach (var fileName in fileNames)
          {
             var fileData = File.ReadAllBytes(fileName);
             var tex = new Texture2D(2, 2);
             tex.LoadImage(fileData);
             textures.Add(tex);
          }

          var resultTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
          var resultRects = resultTex.PackTextures(textures.ToArray(), 0, 1 << 11);
          if (resultRects is { Length: > 0 })
          {
             var savePath = EditorUtility.SaveFilePanel("Save PNG", Application.dataPath, "newImage", "png");
             if (!string.IsNullOrEmpty(savePath))
             {
                File.WriteAllBytes(savePath, resultTex.EncodeToPNG());
                AssetDatabase.Refresh();

                var projectRelativePath = FileUtil.GetProjectRelativePath(savePath);
                var importer = AssetImporter.GetAtPath(projectRelativePath) as TextureImporter;
                if (!importer)
                {
                   Debug.LogError("TextureImporter를 가져올 수 없습니다: " + projectRelativePath);
                   return null;
                }

                // 스프라이트 타입 & Multiple로 설정
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;

                var metas = new SpriteRect[resultRects.Length];
                for (var i = 0; i < resultRects.Length; i++)
                {
                   var rt = resultRects[i];
                   metas[i] = new SpriteRect
                   {
                      name = Path.GetFileNameWithoutExtension(fileNames[i]),  // 스프라이트 이름
                      rect = new Rect(resultTex.width * rt.x, resultTex.height * rt.y, resultTex.width * rt.width, resultTex.height * rt.height),
                      alignment = (int)SpriteAlignment.Center,
                      pivot = new Vector2(0.5f, 0.5f),
                   };
                }

                // 메타 데이터 적용 & 재임포트
                var provider = _providerFactory.GetSpriteEditorDataProviderFromObject(importer);
                provider.InitSpriteEditorDataProvider();
                provider.SetSpriteRects(metas);
                provider.Apply();
                importer.SaveAndReimport();

                return projectRelativePath;
             }
          }
          else
          {
             Debug.LogError($"Texture2D.GenerateAtlas() Failed");
             return null;
          }

          return null;
       }
    }
}