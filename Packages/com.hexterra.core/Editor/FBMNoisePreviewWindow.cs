using UnityEditor;
using UnityEngine;

namespace HexTerra.Editor
{
    public class FBMNoisePreviewWindow : EditorWindow
    {
        private Texture2D _generatedTexture;
        private int _textureScale = 512; // Width and height of the texture
        private int _previewSize = 512;
        private int _step = 20; // Number of steps between 0.0f and 1.0f
        private int _octaves = 5;
        private float _lacunarity = 2.0f;
        private float _persistence = 0.5f;
        private float _xOffset;
        private float _yOffset;

        private void OnGUI()
        {
            GUILayout.Label("FBM Noise Texture Generator", EditorStyles.boldLabel);

            // Store previous values to check for changes
            var prevTextureScale = _textureScale;
            var prevPreviewSize = _previewSize;
            var prevStep = _step;
            var prevOctaves = _octaves;
            var prevLacunarity = _lacunarity;
            var prevPersistence = _persistence;
            var prevXOffset = _xOffset;
            var prevYOffset = _yOffset;

            // Texture parameters
            _textureScale = EditorGUILayout.IntField("Texture Scale", _textureScale);
            _previewSize = EditorGUILayout.IntField("Preview Size", _previewSize);
            _step = EditorGUILayout.IntField("Steps", _step);
            _octaves = EditorGUILayout.IntSlider("Octaves", _octaves, 1, 10);
            _lacunarity = EditorGUILayout.Slider("Lacunarity", _lacunarity, 1.0f, 5.0f);
            _persistence = EditorGUILayout.Slider("Persistence", _persistence, 0.0f, 1.0f);
            _xOffset = EditorGUILayout.FloatField("X Offset", _xOffset);
            _yOffset = EditorGUILayout.FloatField("Y Offset", _yOffset);

            if (GUILayout.Button("Generate Noise") || _previewSize != prevPreviewSize || prevTextureScale != _textureScale || prevStep != _step || prevOctaves != _octaves ||
                !Mathf.Approximately(prevLacunarity, _lacunarity) || !Mathf.Approximately(prevPersistence, _persistence) || prevXOffset != _xOffset || prevYOffset != _yOffset)
            {
                GenerateNoisePreview(new Vector2(_xOffset, _yOffset));
                Repaint();
            }

            if (_generatedTexture)
            {
                var textureRect = GUILayoutUtility.GetRect(_previewSize, _previewSize);
                GUI.DrawTexture(textureRect, _generatedTexture, ScaleMode.ScaleToFit);
            }

            if (GUILayout.Button("Save Texture"))
                SaveNoiseTexture();
        }

        [MenuItem("Tools/HexTerra/FBM Noise Preview")]
        private static void Init() => GetWindow<FBMNoisePreviewWindow>("FBM Noise Preview").Show();

        // Builds a transient FBMNoiseData from the current sliders and delegates texture
        // generation to FBMElevationSource, so the FBM formula lives in exactly one place
        private void GenerateNoisePreview(Vector2 _translation)
        {
            var previewData = ScriptableObject.CreateInstance<FBMNoiseData>();
            previewData.step = _step;
            previewData.octaves = _octaves;
            previewData.lacunarity = _lacunarity;
            previewData.persistence = _persistence;

            var elevationSource = new FBMElevationSource(previewData);
            _generatedTexture = elevationSource.GenerateFbmNoiseTexture(_translation, _textureScale);

            DestroyImmediate(previewData);
        }

        private void SaveNoiseTexture()
        {
            if (!_generatedTexture)
                return;

            var path = EditorUtility.SaveFilePanelInProject("Save FBM Noise Texture", "FBMNoiseTexture", "png", "Choose where to save the generated noise texture");
            if (string.IsNullOrEmpty(path))
                return;

            System.IO.File.WriteAllBytes(path, _generatedTexture.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

            // Get the TextureImporter for the saved texture and adjust import settings
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer)
            {
                importer.isReadable = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            Debug.Log("FBM noise texture generated and saved at: " + path);
        }
    }
}
