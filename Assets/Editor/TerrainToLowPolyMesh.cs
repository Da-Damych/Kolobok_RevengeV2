using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.IO;

public static class TerrainToLowPolyMesh
{
    // Насколько плотным будет меш.
    // 33  - очень низкий
    // 65  - низкий
    // 129 - средний
    // 257 - уже довольно много для мобилки
    private const int TargetResolution = 129;

    // Чтобы не превышать лимит 65535 вершин на меш,
    // режем на чанки. Для мобильных лучше не превышать это значение.
    private const int MaxVerticesPerMesh = 60000;

    // Если нужна коллизия по мешу.
    // Для слабых мобилок лучше делать отдельный ещё более простой коллизионный меш.
    private const bool AddMeshColliders = false;

    private const string OutputFolder = "Assets/TerrainMeshes";

    [MenuItem("Tools/Terrain/Convert Selected Terrain to Low-Poly Mesh")]
    private static void ConvertSelectedTerrain()
    {
        GameObject selected = Selection.activeGameObject;

        if (selected == null)
        {
            EditorUtility.DisplayDialog(
                "Terrain to Mesh",
                "Сначала выберите GameObject с компонентом Terrain.",
                "OK"
            );
            return;
        }

        Terrain terrain = selected.GetComponent<Terrain>();

        if (terrain == null || terrain.terrainData == null)
        {
            EditorUtility.DisplayDialog(
                "Terrain to Mesh",
                "На выбранном объекте нет Terrain или TerrainData.",
                "OK"
            );
            return;
        }

        TerrainData data = terrain.terrainData;

        if (!Directory.Exists(OutputFolder))
        {
            Directory.CreateDirectory(OutputFolder);
        }

        // Количество ячеек сетки.
        // Например, TargetResolution = 129 даёт 128x128 ячеек.
        int cells = Mathf.Clamp(TargetResolution - 1, 2, data.heightmapResolution - 1);

        // Размер чанка в ячейках, чтобы вершин было меньше лимита.
        int chunkCells = Mathf.FloorToInt(Mathf.Sqrt(MaxVerticesPerMesh)) - 1;
        chunkCells = Mathf.Clamp(chunkCells, 1, cells);

        int chunkCount = Mathf.CeilToInt((float)cells / chunkCells);

        GameObject root = new GameObject($"{selected.name}_LowPoly");
        root.transform.SetParent(selected.transform, false);

        Material sharedMaterial = CreateSharedMaterial($"{selected.name}_LowPoly");

        if (sharedMaterial == null)
        {
            sharedMaterial = terrain.materialTemplate;
        }

        int chunkIndex = 0;

        for (int cy = 0; cy < chunkCount; cy++)
        {
            for (int cx = 0; cx < chunkCount; cx++)
            {
                int startCellX = cx * chunkCells;
                int startCellZ = cy * chunkCells;

                int cellCountX = Mathf.Min(chunkCells, cells - startCellX);
                int cellCountZ = Mathf.Min(chunkCells, cells - startCellZ);

                if (cellCountX <= 0 || cellCountZ <= 0)
                    continue;

                Mesh mesh = BuildTerrainChunk(
                    data,
                    cells,
                    startCellX,
                    startCellZ,
                    cellCountX,
                    cellCountZ,
                    selected.name,
                    chunkIndex
                );

                string meshPath = AssetDatabase.GenerateUniqueAssetPath(
                    $"{OutputFolder}/{mesh.name}.asset"
                );

                AssetDatabase.CreateAsset(mesh, meshPath);
                AssetDatabase.SaveAssets();

                Mesh savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

                GameObject chunkObject = new GameObject($"Chunk_{chunkIndex:000}");
                chunkObject.transform.SetParent(root.transform, false);

                MeshFilter mf = chunkObject.AddComponent<MeshFilter>();
                mf.sharedMesh = savedMesh;

                MeshRenderer mr = chunkObject.AddComponent<MeshRenderer>();
                mr.sharedMaterial = sharedMaterial;

                if (AddMeshColliders)
                {
                    MeshCollider mc = chunkObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = savedMesh;
                }

                chunkIndex++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Terrain to Mesh",
            $"Готово.\nСоздано чанков: {chunkIndex}.\nМеши лежат в {OutputFolder}.\n\nНе забудьте отключить или удалить исходный Terrain, чтобы не рисовать его дважды.",
            "OK"
        );
    }

    private static Mesh BuildTerrainChunk(
        TerrainData data,
        int totalCells,
        int startCellX,
        int startCellZ,
        int cellCountX,
        int cellCountZ,
        string baseName,
        int chunkIndex)
    {
        int vertsX = cellCountX + 1;
        int vertsZ = cellCountZ + 1;
        int vertexCount = vertsX * vertsZ;

        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] triangles = new int[cellCountX * cellCountZ * 6];

        int vertexIndex = 0;

        for (int z = 0; z < vertsZ; z++)
        {
            float normalizedZ = (startCellZ + z) / (float)totalCells;

            for (int x = 0; x < vertsX; x++)
            {
                float normalizedX = (startCellX + x) / (float)totalCells;

                // GetInterpolatedHeight возвращает нормализованную высоту 0..1
                float normalizedHeight = data.GetInterpolatedHeight(normalizedX, normalizedZ);

                vertices[vertexIndex] = new Vector3(
                    normalizedX * data.size.x,
                    normalizedHeight * data.size.y,
                    normalizedZ * data.size.z
                );

                uvs[vertexIndex] = new Vector2(normalizedX, normalizedZ);

                vertexIndex++;
            }
        }

        int triangleIndex = 0;

        for (int z = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                int bl = z * vertsX + x;
                int br = bl + 1;
                int tl = bl + vertsX;
                int tr = tl + 1;

                // Первый треугольник
                triangles[triangleIndex++] = bl;
                triangles[triangleIndex++] = tl;
                triangles[triangleIndex++] = br;

                // Второй треугольник
                triangles[triangleIndex++] = br;
                triangles[triangleIndex++] = tl;
                triangles[triangleIndex++] = tr;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = $"{baseName}_LowPoly_{chunkIndex:000}";

        if (vertexCount > 65535)
        {
            mesh.indexFormat = IndexFormat.UInt32;
        }
        else
        {
            mesh.indexFormat = IndexFormat.UInt16;
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Если планируете запекать лайтмапы, можно раскомментировать:
        // UnityEditor.Unwrapping.GenerateSecondaryUVSet(mesh);

        return mesh;
    }

    private static Material CreateSharedMaterial(string materialName)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit");

        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            return null;

        Material material = new Material(shader);

        string materialPath = AssetDatabase.GenerateUniqueAssetPath(
            $"{OutputFolder}/{materialName}.mat"
        );

        AssetDatabase.CreateAsset(material, materialPath);

        return AssetDatabase.LoadAssetAtPath<Material>(materialPath);
    }
}