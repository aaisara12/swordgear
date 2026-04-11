#nullable enable

using UnityEngine;

namespace Aaron.Scripts
{
    /// <summary>
    /// Toggle Assets based on platform
    /// </summary>
    public class PlatformAssetSwitcher : MonoBehaviour
    {
        [SerializeField] private bool forceMobileControls = true;
        [SerializeField] private GameObject[]? pcAssets;
        [SerializeField] private GameObject[]? mobileAssets;
        
        private void Awake()
        {
            if (forceMobileControls || Application.isMobilePlatform)
            {
                if (mobileAssets != null)
                {
                    foreach (var a in mobileAssets)
                    {
                        if (a != null) a.SetActive(true);
                    }
                }

                if (pcAssets != null)
                {
                    foreach (var a in pcAssets)
                    {
                        if (a != null) a.SetActive(false);
                    }
                }
            }
            else
            {
                if (mobileAssets != null)
                {
                    foreach (var a in mobileAssets)
                    {
                        if (a != null) a.SetActive(false);
                    }
                }

                if (pcAssets != null)
                {
                    foreach (var a in pcAssets)
                    {
                        if (a != null) a.SetActive(true);
                    }
                }
            }
        }
    }
}
