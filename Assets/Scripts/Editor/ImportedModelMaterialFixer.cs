using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// Assets/Imported klasöründeki modellerin materyallerine otomatik texture atar.
/// Unity Menu: Tools > ARFish > Fix Imported Materials
/// </summary>
public class ImportedModelMaterialFixer : AssetPostprocessor
{
    const string ImportedRoot = "Assets/Imported";

    // Model import edildiğinde otomatik çalışır
    void OnPostprocessModel(GameObject model)
    {
        string path = assetPath.Replace('\\', '/');
        if (!path.StartsWith(ImportedRoot + "/")) return;

        string groupFolder = GetGroupFolder(path);
        if (groupFolder == null) return;

        EnsureNormalMapTypesForGroup(groupFolder);
        ApplyTexturesToModel(model, groupFolder, path);
    }

    // ─── Manuel tetikleyici ────────────────────────────────────────────────────
    [MenuItem("Tools/ARFish/Fix Imported Materials")]
    static void FixAll()
    {
        // Önce normal map texture tiplerini ayarla
        EnsureNormalMapTypesForGroup(null);

        // Tüm modelleri yeniden import et (OnPostprocessModel'ı tetikler)
        string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { ImportedRoot });
        foreach (string guid in modelGuids)
        {
            string modelPath = AssetDatabase.GUIDToAssetPath(guid);
            Debug.Log($"[MaterialFixer] Yeniden import: {modelPath}");
            EditorApplication.delayCall += () => {
                AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
            };
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MaterialFixer] Tüm materyaller güncellendi.");
    }

    // ─── Yardımcı metodlar ─────────────────────────────────────────────────────

    static string GetGroupFolder(string path)
    {
        // "Assets/Imported/red-finned-fish/source/..." → "Assets/Imported/red-finned-fish"
        int start = ImportedRoot.Length + 1;
        int slash = path.IndexOf('/', start);
        return slash < 0 ? null : path.Substring(0, slash);
    }

    static void EnsureNormalMapTypesForGroup(string groupFolder)
    {
        string[] searchIn = groupFolder != null ? new[] { groupFolder } : new[] { ImportedRoot };
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", searchIn);

        foreach (string guid in guids)
        {
            string texPath = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(texPath).ToLower();
            if (!name.Contains("normal") && !name.Contains("nrm")) continue;

            var ti = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (ti == null || ti.textureType == TextureImporterType.NormalMap) continue;

            ti.textureType = TextureImporterType.NormalMap;
            ti.SaveAndReimport();
        }
    }

    // OBJ'nin yanındaki MTL dosyasını okur → { materialAdı: textureAdı }
    static Dictionary<string, string> ParseMtlFile(string objAssetPath)
    {
        var mapping = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        string dir = Path.GetDirectoryName(objAssetPath);

        foreach (string mtlPath in Directory.GetFiles(dir, "*.mtl"))
        {
            string currentMat = null;
            foreach (string line in File.ReadAllLines(mtlPath))
            {
                string t = line.Trim();
                if (t.StartsWith("newmtl "))
                    currentMat = t.Substring(7).Trim();
                else if (t.StartsWith("map_Kd ") && currentMat != null)
                    mapping[currentMat] = Path.GetFileNameWithoutExtension(t.Substring(7).Trim());
            }
        }
        return mapping;
    }

    // Bir klasördeki tüm Texture2D'leri isim → texture sözlüğüne yükler
    static void AddTexturesFromFolder(string folder, Dictionary<string, Texture2D> result)
    {
        if (!AssetDatabase.IsValidFolder(folder)) return;
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        foreach (string guid in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            if (tex != null)
                result[Path.GetFileNameWithoutExtension(p)] = tex;
        }
    }

    static Dictionary<string, Texture2D> BuildTextureLookup(string groupFolder)
    {
        var result = new Dictionary<string, Texture2D>(System.StringComparer.OrdinalIgnoreCase);
        AddTexturesFromFolder(groupFolder + "/textures", result);
        AddTexturesFromFolder(groupFolder + "/source", result);
        return result;
    }

    static Texture2D FindByKeyword(Dictionary<string, Texture2D> textures, params string[] keywords)
    {
        foreach (string kw in keywords)
            foreach (var kvp in textures)
                if (kvp.Key.ToLower().Contains(kw))
                    return kvp.Value;
        return null;
    }

    static void AssignIfEmpty(Material mat, string property, Texture2D tex)
    {
        if (tex != null && mat.HasProperty(property) && mat.GetTexture(property) == null)
            mat.SetTexture(property, tex);
    }

    // ─── Materyal atama ────────────────────────────────────────────────────────

    static void ApplyTexturesToModel(GameObject model, string groupFolder, string modelPath)
    {
        var textures = BuildTextureLookup(groupFolder);
        if (textures.Count == 0) return;

        // OBJ modelleri için MTL tabanlı eşleme oluştur
        Dictionary<string, string> mtlMapping = null;
        if (modelPath.EndsWith(".obj", System.StringComparison.OrdinalIgnoreCase))
        {
            string absolutePath = Path.Combine(Application.dataPath.Replace("/Assets", ""), modelPath);
            mtlMapping = ParseMtlFile(absolutePath);
        }

        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat == null) continue;
                ApplyToMaterial(mat, textures, mtlMapping);
            }
        }
    }

    static void ApplyToMaterial(Material mat, Dictionary<string, Texture2D> textures,
                                 Dictionary<string, string> mtlMapping)
    {
        // ── MTL tabanlı atama (OBJ modeller: Coral vb.) ──
        if (mtlMapping != null && mtlMapping.TryGetValue(mat.name, out string texName))
        {
            if (textures.TryGetValue(texName, out Texture2D mtlTex))
            {
                AssignIfEmpty(mat, "_MainTex", mtlTex);
                return; // MTL eşlemesi yeterliyse diğer mantığı atla
            }
        }

        // ── Material_N → tex(N+1) fallback (coral için) ──
        var coralMatch = Regex.Match(mat.name, @"[Mm]aterial_(\d+)");
        if (coralMatch.Success)
        {
            int n = int.Parse(coralMatch.Groups[1].Value) + 1;
            // tex5, tex4, tex3, tex2, tex1 dene
            for (int i = n; i >= 1; i--)
            {
                if (textures.TryGetValue("tex" + i, out Texture2D coralTex))
                {
                    AssignIfEmpty(mat, "_MainTex", coralTex);
                    break;
                }
            }
            return;
        }

        // ── Genel isimlendirme kuralı (fish, moss, rock vb.) ──
        // Albedo / Diffuse / BaseColor → _MainTex
        AssignIfEmpty(mat, "_MainTex",
            FindByKeyword(textures, "diffuse", "albedo", "basecolor", "base_color"));

        // Normal map → _BumpMap
        var normalTex = FindByKeyword(textures, "normal", "nrm", "normalmap");
        if (normalTex != null && mat.HasProperty("_BumpMap") && mat.GetTexture("_BumpMap") == null)
        {
            mat.SetTexture("_BumpMap", normalTex);
            mat.EnableKeyword("_NORMALMAP");
        }

        // Roughness → _GlossMapScale (ters çevrilmiş: smoothness = 1 - roughness)
        // Texture olarak atamak yerine _Glossiness'i düşürüyoruz
        if (FindByKeyword(textures, "roughness", "rough") != null)
        {
            if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", 0.25f); // ham değer; roughness texture map yok
        }

        // AO / Occlusion → _OcclusionMap
        AssignIfEmpty(mat, "_OcclusionMap",
            FindByKeyword(textures, "ao", "occlusion", "ambient_occlusion"));
    }
}
