using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace RapidOCRWinform;

/// <summary>
/// Materializes the OCR models embedded in the executable into a versioned user cache.
/// OcrLite/ONNX Runtime require file-system paths, so the embedded files cannot be used
/// directly as streams. The cache is an implementation detail and is not a deployment
/// dependency: a clean machine only needs the executable.
/// </summary>
internal static class EmbeddedModelProvider
{
    private const string ResourcePrefix = "RapidOCRWinform.EmbeddedModels.";

    private static readonly string[] ModelFileNames =
    [
        "ch_PP-OCRv5_mobile_det.onnx",
        "ch_ppocr_mobile_v2.0_cls_infer.onnx",
        "ch_PP-OCRv5_rec_mobile_infer.onnx",
        "ppocrv5_dict.txt",
    ];

    public static string EnsureExtracted()
    {
        Assembly assembly = typeof(EmbeddedModelProvider).Assembly;
        ModelManifest manifest = ReadManifest(assembly);
        List<Exception> errors = [];

        foreach (string root in GetCandidateRoots())
        {
            try
            {
                return EnsureCache(assembly, manifest, root);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                errors.Add(new IOException($"无法写入模型缓存目录：{root}", exception));
            }
        }

        throw new InvalidOperationException(
            "用户数据目录和临时目录均不可写，请检查磁盘空间及目录权限。",
            new AggregateException(errors));
    }

    private static ModelManifest ReadManifest(Assembly assembly)
    {
        using IncrementalHash manifestHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        List<ModelResource> resources = [];

        foreach (string fileName in ModelFileNames)
        {
            string resourceName = ResourcePrefix + fileName;
            using Stream stream = OpenResource(assembly, resourceName);
            long length = stream.Length;
            byte[] contentHash = SHA256.HashData(stream);

            manifestHash.AppendData(Encoding.UTF8.GetBytes(fileName));
            manifestHash.AppendData([0]);
            manifestHash.AppendData(contentHash);
            resources.Add(new ModelResource(fileName, resourceName, length, contentHash));
        }

        string version = Convert.ToHexString(manifestHash.GetHashAndReset())
            .ToLowerInvariant()[..16];
        return new ModelManifest(version, resources);
    }

    private static IEnumerable<string> GetCandidateRoots()
    {
        string localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string? localCacheRoot = null;
        if (!string.IsNullOrWhiteSpace(localData))
        {
            localCacheRoot = Path.Combine(localData, "RapidOCRSchedule", "model-cache");
            yield return localCacheRoot;
        }

        string tempRoot = Path.Combine(Path.GetTempPath(), "RapidOCRSchedule", "model-cache");
        if (localCacheRoot is null || !PathsEqual(tempRoot, localCacheRoot))
        {
            yield return tempRoot;
        }
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);

    private static string EnsureCache(Assembly assembly, ModelManifest manifest, string root)
    {
        string cacheDirectory = Path.Combine(root, manifest.Version);
        Directory.CreateDirectory(cacheDirectory);

        foreach (ModelResource resource in manifest.Resources)
        {
            string targetPath = Path.Combine(cacheDirectory, resource.FileName);
            if (FileMatches(targetPath, resource))
            {
                continue;
            }

            string temporaryPath = targetPath + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                using (Stream source = OpenResource(assembly, resource.ResourceName))
                using (FileStream destination = new(
                           temporaryPath,
                           FileMode.CreateNew,
                           FileAccess.Write,
                           FileShare.None,
                           bufferSize: 1024 * 1024,
                           FileOptions.SequentialScan))
                {
                    source.CopyTo(destination);
                    destination.Flush(flushToDisk: true);
                }

                if (!FileMatches(temporaryPath, resource))
                {
                    throw new IOException($"内置模型释放后校验失败：{resource.FileName}");
                }

                // Another process may have completed the same cache while this process
                // was extracting. Keep its valid file; otherwise replace atomically.
                if (!FileMatches(targetPath, resource))
                {
                    File.Move(temporaryPath, targetPath, overwrite: true);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        return cacheDirectory;
    }

    private static bool FileMatches(string path, ModelResource resource)
    {
        try
        {
            FileInfo file = new(path);
            if (!file.Exists || file.Length != resource.Length)
            {
                return false;
            }

            using FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            return CryptographicOperations.FixedTimeEquals(SHA256.HashData(stream), resource.ContentHash);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static Stream OpenResource(Assembly assembly, string resourceName) =>
        assembly.GetManifestResourceStream(resourceName)
        ?? throw new InvalidOperationException($"程序缺少内置资源：{resourceName}");

    private sealed record ModelManifest(string Version, IReadOnlyList<ModelResource> Resources);

    private sealed record ModelResource(
        string FileName,
        string ResourceName,
        long Length,
        byte[] ContentHash);
}
