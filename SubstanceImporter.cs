using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SubstanceImporter : Editor
{
	[MenuItem("Substance/Import Model", false, -1)]
    public static void ImportModel() {
        string path = EditorUtility.OpenFilePanel("Overwrite with obj", "", "obj");
        if (path.Length != 0)
        {
            var fileName = GetFileName(path);
            var pathWithoutName = GetPathWithoutFileName(path);
            var fileContentObj = File.ReadAllBytes(path);
            var fileContentMtl = File.ReadAllBytes(
                pathWithoutName +
                fileName +
                ".mtl"
            );
            CreateFoldersIfDontExist(fileName);
            
            var assetPath = Application.dataPath + "/Models/" + fileName + "/" + fileName;

            File.WriteAllBytes(assetPath + ".mtl", fileContentMtl);
            AssetDatabase.ImportAsset(assetPath + ".mtl");

            File.WriteAllBytes(assetPath + ".obj", fileContentObj);
            AssetDatabase.ImportAsset(assetPath + ".obj");

            AssetDatabase.Refresh();

            ExtractMaterials(
                "Assets/Models/" + fileName + "/" + fileName + ".obj",
                "Assets/Models/" + fileName + "/Materials/"
            );
            AssetDatabase.Refresh();
            SetTexturesForMaterials(
                "/Models/" + fileName + "/Materials/",
                pathWithoutName,
                fileName
            );
        }
    }

    private static void SetTexturesForMaterials(string materialsFolder, string loadPath, string fileName)
    {
        foreach (string file in System.IO.Directory.GetFiles(Application.dataPath + materialsFolder))
        {
            if (!file.EndsWith(".mat")) continue;
            var materialName = GetFileName(file);
            Material material = AssetDatabase.LoadAssetAtPath(
                "Assets" + materialsFolder + materialName + ".mat", 
                typeof(Material)) as Material;
            var baseColorTexturePath = LoadTextureForMaterial(fileName, loadPath, materialName, "BaseColor");
            if(baseColorTexturePath != "-1")
            {
                Texture texture = AssetDatabase.LoadAssetAtPath(
                    baseColorTexturePath,
                    typeof(Texture)
                ) as Texture;
                material.SetTexture("_BaseColorMap", texture);
            }
            var maskMapTexturePath = LoadTextureForMaterial(fileName, loadPath, materialName, "Metallic");
            if (maskMapTexturePath != "-1")
            {
                Texture texture = AssetDatabase.LoadAssetAtPath(
                    maskMapTexturePath,
                    typeof(Texture)
                ) as Texture;
                material.SetTexture("_MaskMap", texture);
            }
            var normalMapTexturePath = LoadTextureForMaterial(fileName, loadPath, materialName, "Normal");
            if (normalMapTexturePath != "-1")
            {
                TextureImporter textureImporter = AssetImporter.GetAtPath(normalMapTexturePath) as TextureImporter;
                textureImporter.textureType = TextureImporterType.NormalMap;
                AssetDatabase.ImportAsset(normalMapTexturePath);
                AssetDatabase.Refresh();
                Texture texture = AssetDatabase.LoadAssetAtPath(
                    normalMapTexturePath,
                    typeof(Texture)
                ) as Texture;
                material.SetTexture("_NormalMap", texture);
            }
            var emissionMapTexturePath = LoadTextureForMaterial(fileName, loadPath, materialName, "Emission");
            if (emissionMapTexturePath != "-1")
            {
                Texture texture = AssetDatabase.LoadAssetAtPath(
                    emissionMapTexturePath,
                    typeof(Texture)
                ) as Texture;
                material.SetTexture("_EmissiveColorMap", texture);
                material.SetInt("_UseEmissiveIntensity", 1);
                material.EnableKeyword("_EMISSION");
                material.SetFloat("_EmissiveIntensity", 40.0f);
                material.SetColor("_EmissiveColor", Color.red);
            }
            material.SetFloat("_SmoothnessRemapMax", 0.4f);
            material.SetFloat("_NormalScale", 1.2f);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }

    private static string LoadTextureForMaterial(string fileNameOriginal, string loadPath, string materialName, string textureType)
    {
        foreach(string file in System.IO.Directory.EnumerateFiles(loadPath, "*.*", SearchOption.AllDirectories))
        {
            var fileName = GetFileName(file);
            var fileExtension = GetFileExtension(file);
            var assetPath = Application.dataPath + "/Models/" + fileNameOriginal + "/Textures/"; 
            if (fileName.EndsWith(materialName + "_" + textureType))
            {
                var path = loadPath + fileName;
                var textureContent = File.ReadAllBytes(
                    file
                );
                File.WriteAllBytes(
                    assetPath + fileName + "." + fileExtension,
                    textureContent
                );
                AssetDatabase.ImportAsset(
                    assetPath + fileName + "." + fileExtension,
                    ImportAssetOptions.ForceUpdate
                );
                AssetDatabase.Refresh();
                return "Assets/Models/" + fileNameOriginal + "/Textures/" + fileName + "." + fileExtension;
            }
        }
        return "-1";
    }

    private static void CreateFoldersIfDontExist(string fileName)
    {
        if (!Directory.Exists(Application.dataPath + "/Models/"))
        {
            AssetDatabase.CreateFolder("Assets", "Models");
        }
        if (!Directory.Exists(Application.dataPath + "/Models/" + fileName + "/"))
        {
            AssetDatabase.CreateFolder("Assets/Models", fileName);
        }
        if (!Directory.Exists(Application.dataPath + "/Models/" + fileName + "/Materials"))
        {
            AssetDatabase.CreateFolder("Assets/Models/" + fileName, "Materials");
        }
        if (!Directory.Exists(Application.dataPath + "/Models/" + fileName + "/Textures"))
        {
            AssetDatabase.CreateFolder("Assets/Models/" + fileName, "Textures");
        }
    }

    public static string GetFileName(string path)
    {
        path = path.Replace("\\", "/");
        var parts = path.Split('/');
        var lastPart = parts[parts.Length - 1];
        return lastPart.Split('.')[0];
    }
    public static string GetFileExtension(string path)
    {
        var parts = path.Split('/');
        var lastPart = parts[parts.Length - 1];
        return lastPart.Split('.')[1];
    }

    public static string GetPathWithoutFileName(string path)
    {
        var parts = path.Split('/');
        var lastPart = parts[parts.Length - 1];
        return path.Replace(lastPart, "");
    }

    public static void ExtractMaterials(string assetPath, string destinationPath)
    {
        HashSet<string> hashSet = new HashSet<string>();
        var test = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        IEnumerable<Object> enumerable = from x in AssetDatabase.LoadAllAssetsAtPath(assetPath)
                                         where x.GetType() == typeof(Material)
                                         select x;
        foreach (Object item in enumerable)
        {
            string path = System.IO.Path.Combine(destinationPath, item.name) + ".mat";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            string value = AssetDatabase.ExtractAsset(item, path);
            if (string.IsNullOrEmpty(value))
            {
                hashSet.Add(assetPath);
            }
        }

        foreach (string item2 in hashSet)
        {
            AssetDatabase.WriteImportSettingsIfDirty(item2);
            AssetDatabase.ImportAsset(item2, ImportAssetOptions.ForceUpdate);
        }
    }
}
