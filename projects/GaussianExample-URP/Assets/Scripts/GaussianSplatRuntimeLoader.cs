using System.IO;
using UnityEngine;

namespace GaussianSplatting.Runtime
{
    /// <summary>
    /// High-level component that:
    /// 1. Uses a runtime loader to read a PLY/SPZ file.
    /// 2. Creates a GaussianSplatAsset in memory.
    /// 3. Assigns it to the GaussianSplatRenderer on the same GameObject.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GaussianSplatRenderer))]
    public class GaussianSplatRuntimeLoader : MonoBehaviour
    {
        [Header("Input File")]
        [Tooltip("Path to the Gaussian Splat file (PLY or SPZ). " +
                 "Can be absolute or relative to Application.dataPath / persistentDataPath, etc.")]
        [SerializeField]
        private string m_FilePath;

        [Header("Loading Options")]
        [Tooltip("If true, automatically load and assign the asset on Start().")]
        [SerializeField]
        private bool m_LoadOnStart = true;

        [Tooltip("Optional: reference to the runtime loader that actually builds the asset. " +
                 "If left null, this component will try to GetComponent<RuntimePLYLoader>().")]
        [SerializeField]
        private RuntimePLYLoader m_RuntimeLoader;

        private GaussianSplatRenderer m_Renderer;

        private void Awake()
        {
            m_Renderer = GetComponent<GaussianSplatRenderer>();

            if (m_RuntimeLoader == null)
            {
                m_RuntimeLoader = GetComponent<RuntimePLYLoader>();
                if (m_RuntimeLoader == null)
                {
                    Debug.LogError(
                        "[GaussianSplatRuntimeLoader] No RuntimePLYLoader found on this GameObject. " +
                        "Please add one or assign it explicitly.",
                        this);
                }
            }
        }

        private void Start()
        {
            if (m_LoadOnStart)
            {
                LoadAndAssign();
            }
        }

        /// <summary>
        /// Public entry point: load the file at m_FilePath and assign the resulting asset
        /// to the GaussianSplatRenderer on this GameObject.
        /// </summary>
        [ContextMenu("Load And Assign")]
        public void LoadAndAssign()
        {
            if (m_RuntimeLoader == null)
            {
                Debug.LogError("[GaussianSplatRuntimeLoader] Runtime loader is not set.", this);
                return;
            }

            if (string.IsNullOrEmpty(m_FilePath))
            {
                Debug.LogError("[GaussianSplatRuntimeLoader] File path is empty.", this);
                return;
            }

            // You can customize how you resolve relative paths here.
            string resolvedPath = ResolvePath(m_FilePath);

            if (!File.Exists(resolvedPath))
            {
                Debug.LogError($"[GaussianSplatRuntimeLoader] File not found: {resolvedPath}", this);
                return;
            }

            Debug.Log($"[GaussianSplatRuntimeLoader] Loading Gaussian splat from: {resolvedPath}", this);

            // Call into your runtime loader. Adjust the method name if needed.
            GaussianSplatAsset asset = m_RuntimeLoader.LoadPLYFile(resolvedPath);
            // or: m_RuntimeLoader.LoadGaussianFile(resolvedPath);

            if (asset == null)
            {
                Debug.LogError("[GaussianSplatRuntimeLoader] Failed to create GaussianSplatAsset.", this);
                return;
            }

            m_Renderer.m_Asset = asset;
            Debug.Log("[GaussianSplatRuntimeLoader] Successfully assigned GaussianSplatAsset to renderer.", this);
        }

        /// <summary>
        /// Helper for resolving simple relative paths. 
        /// You can adapt this to your actual storage strategy (StreamingAssets, persistentDataPath, etc.).
        /// </summary>
        private static string ResolvePath(string rawPath)
        {
            // Absolute path?
            if (Path.IsPathRooted(rawPath))
                return rawPath;

            // Example strategy: treat paths starting with "persistent:" as relative to persistentDataPath
            const string persistentPrefix = "persistent:";
            if (rawPath.StartsWith(persistentPrefix))
            {
                string relative = rawPath.Substring(persistentPrefix.Length);
                return Path.Combine(Application.persistentDataPath, relative);
            }

            // Default: relative to Application.dataPath
            return Path.Combine(Application.dataPath, rawPath);
        }

        /// <summary>
        /// Allow changing the file path from other scripts, then reloading.
        /// </summary>
        public void SetFilePathAndReload(string newPath)
        {
            m_FilePath = newPath;
            LoadAndAssign();
        }
    }
}
