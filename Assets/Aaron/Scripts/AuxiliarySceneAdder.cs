#nullable enable

using UnityEngine;

public class AuxiliarySceneAdder : MonoBehaviour
{
    [SerializeField] private SceneTransitioner? sceneTransitioner;
    [SerializeField] private SceneReference auxiliaryScene = new SceneReference();

    private void Awake()
    {
        if (sceneTransitioner == null)
        {
            Debug.LogError("SceneTransitioner is null! Can't add auxiliary scene.");
            return;
        }
        
        sceneTransitioner.AddAuxiliaryScene(auxiliaryScene.sceneName);
    }
}
