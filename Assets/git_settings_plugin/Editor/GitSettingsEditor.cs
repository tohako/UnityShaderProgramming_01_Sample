using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace GitSettings.Editor
{
    public class GitSettingsEditor : EditorWindow
    {
        /// <summary>
        /// GitIgnoreに含めるテキスト
        /// </summary>
        private class CustomGitIgnore
        {
            /// <summary>
            /// 含めるか
            /// </summary>
            public bool IsInclude;

            /// <summary>
            ///  タイトル
            /// </summary>
            public readonly string Title;
            
            /// <summary>
            /// テキスト
            /// </summary>
            public readonly string IgnoreText;

            public CustomGitIgnore(bool isInclude, string title, string ignoreText)
            {
                IsInclude = isInclude;
                Title = title;
                IgnoreText = ignoreText;
            }
        }

        /// <summary>
        /// GitIgnoreのカスタマイズ
        /// </summary>
        private CustomGitIgnore[] _customGitIgnore = new CustomGitIgnore[]
        {
            new CustomGitIgnore(true, 
                "!.gitkeep",
                "# include .gitkeep" + Environment.NewLine +
                "!.gitkeep"),
            
            new CustomGitIgnore(true, 
                "JetBrainsファイル",
                "# JetBrains Rider" + Environment.NewLine +
                ".idea"),
        };
        
        [MenuItem("Window/Git Settings")]
        private static void OpenWindow()
        {
            GetWindow<GitSettingsEditor>("Git Settings");
        }
        
        private void OnGUI()
        {
            var existsGit = ExistsGit();
            
            EditorGUILayout.LabelField("このプロジェクトのGit: ", existsGit ? "有効！" : "未作成...");
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            
            using (new EditorGUI.DisabledScope(!existsGit))
            {
                DrawEditorSettingsGUI();
                EditorGUILayout.Space();
                DrawJetBrainGitKeepGUI();
                EditorGUILayout.Space();
                DrawWriteGitIgnoreFileGUI();
            }
        }

        private bool ExistsGit()
        {
            var projectDir = Path.GetDirectoryName(Application.dataPath);
            var existsGit = Directory.Exists(projectDir + "/" + ".git");

            return existsGit;
        }

        private void DrawEditorSettingsGUI()
        {
            var isChangedSettings = 
                EditorSettings.externalVersionControl == ExternalVersionControl.Generic &&
                UnityEditor.EditorSettings.serializationMode == SerializationMode.ForceText;
            
            using (new EditorGUI.DisabledScope(isChangedSettings))
            {
                if (GUILayout.Button("Editor設定で.meta管理を有効化", GUILayout.Height(30f)))
                {
                    UnityEditor.EditorSettings.externalVersionControl = ExternalVersionControl.Generic; // "Visible Meta Files";
                    UnityEditor.EditorSettings.serializationMode = SerializationMode.ForceText;
                }
            }
        }

        private void DrawJetBrainGitKeepGUI()
        {
            var rootDirectory = "Plugins/Editor";
            var rootFullDirectory = Application.dataPath + "/" + rootDirectory;
            var fileName = ".gitkeep";

            var existFile = File.Exists(rootFullDirectory + "/" + fileName);

            using (new EditorGUI.DisabledScope(existFile))
            {
                if (GUILayout.Button("Jetbrainに.gitkeep作成", GUILayout.Height(30f)))
                {
                    //ディレクトリ作成
                    CreateDirectory(Path.Combine(rootFullDirectory));
                    //gitkeepファイル作成
                    CreateEmptyText(rootFullDirectory, fileName);
                
                    AssetDatabase.Refresh();
                    AssetDatabase.SaveAssets();
                }
            }
        }

        private void DrawWriteGitIgnoreFileGUI()
        {
            var rootDirectory = Path.GetDirectoryName(Application.dataPath);
            var fileName = ".gitignore";
            var existsFile = ExistsFile(rootDirectory, fileName);
            
            
            if (GUILayout.Button(".gitignoreファイルにUnity用無視リストを上書き", GUILayout.Height(30f)))
            {
                var text = GetText("https://raw.githubusercontent.com/github/gitignore/master/Unity.gitignore");
                if (text == null)
                {
                    return;
                }
                
                //カスタム分追加する
                foreach (var customGitIgnore in _customGitIgnore)
                {
                    //含めないものは無視
                    if (!customGitIgnore.IsInclude)
                    {
                        continue;
                    }

                    //TextにAddしていく
                    text += Environment.NewLine;
                    text += customGitIgnore.IgnoreText;
                    text += Environment.NewLine;
                }
                
                //Unity.gitignoreを上書き
                CreateText(rootDirectory, fileName, text);
            }

            foreach (var customGitIgnore in _customGitIgnore)
            {
                customGitIgnore.IsInclude = EditorGUILayout.Toggle(customGitIgnore.Title, customGitIgnore.IsInclude);
            }
            
            if (!existsFile)
            {
                EditorGUILayout.HelpBox($"{fileName}ファイルが見つかりませんでした。", MessageType.Error);
            }
        }

        private string GetText(string url)
        {
            //wwwで取得
            using (var req = UnityWebRequest.Get(url))
            {
                req.SendWebRequest();
                
                //完了するまで待機
                while (!req.isDone)
                {
                }

                //ネットワークエラー
                if (req.isNetworkError)
                {
                    Debug.LogError($"<NetworkError> {req.error}");
                    return null;
                }

                //レスポンスエラー
                if(req.responseCode != 200)
                {
                    Debug.LogError($"<Response Error> responseCode: {req.responseCode}");
                    return null;
                }
            
                //成功
                return req.downloadHandler.text;
            }
        }
        

        private void CreateEmptyText(string directory, string fileName)
        {
            //gitkeepファイル作成
            using (var writer = File.CreateText(directory + "/" + fileName))
            {
                //空ファイルのためそのまま閉じる
                writer.Close();
            }
        }

        private void CreateText(string directory, string fileName, string contents)
        {
            //txtを作成して内容を書き込み(既にファイルが存在する場合は上書き)
            File.WriteAllText(directory + "/" + fileName, contents, Encoding.Default);
        }

        private void CreateDirectory(string relativeDir)
        {
            if (Directory.Exists(relativeDir))
            {
                return;
            }
            
            Directory.CreateDirectory(relativeDir);
        }

        private bool ExistsFile(string directory, string fileName)
        {
            return File.Exists(directory + "/" + fileName);
        }
    }
}