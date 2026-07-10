#if UNITY_EDITOR
#nullable enable

using UnityEditor;
using UnityEngine;

public static class EnemyBeamLaserPrefabGenerator
{
    private const string OutputPath = "Assets/Visuals/Prefabs/Enemies/EnemyBeamLaser.prefab";

    [MenuItem("Henry/Generate Enemy Beam Laser Prefab")]
    public static void GenerateFromMenu()
    {
        Sprite? sprite = CombatVfxPrefabBuilder.LoadSquareSprite();
        if (sprite == null)
        {
            Debug.LogError("EnemyBeamLaserPrefabGenerator: could not resolve a square sprite for the beam visuals.");
            return;
        }

        GameObject root = new GameObject("EnemyBeamLaser");
        try
        {
            EnemyBeamLaser laser = root.AddComponent<EnemyBeamLaser>();
            root.AddComponent<PooledInstance>();

            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.enabled = false;

            SpriteRenderer telegraphGlow = CombatVfxPrefabBuilder.CreateSpriteChild(
                root.transform, "TelegraphGlow", sprite, new Color(1f, 0.45f, 0.08f, 0.28f), SpriteDrawMode.Sliced);
            SpriteRenderer telegraphEdge = CombatVfxPrefabBuilder.CreateSpriteChild(
                root.transform, "TelegraphEdge", sprite, new Color(1f, 0.62f, 0.12f, 0.92f), SpriteDrawMode.Sliced);
            SpriteRenderer telegraphFill = CombatVfxPrefabBuilder.CreateSpriteChild(
                root.transform, "TelegraphFill", sprite, new Color(0.55f, 0.08f, 0.02f, 0.38f), SpriteDrawMode.Sliced);
            SpriteRenderer outerGlow = CombatVfxPrefabBuilder.CreateSpriteChild(
                root.transform, "BeamOuterGlow", sprite, new Color(1f, 0.35f, 0.08f, 0.32f), SpriteDrawMode.Sliced);
            outerGlow.enabled = false;
            SpriteRenderer midGlow = CombatVfxPrefabBuilder.CreateSpriteChild(
                root.transform, "BeamMidGlow", sprite, new Color(1f, 0.45f, 0.1f, 0.58f), SpriteDrawMode.Sliced);
            midGlow.enabled = false;
            SpriteRenderer core = CombatVfxPrefabBuilder.CreateSpriteChild(
                root.transform, "BeamCore", sprite, new Color(1f, 0.55f, 0.15f, 0.95f), SpriteDrawMode.Sliced);
            core.enabled = false;
            SpriteRenderer hotCore = CombatVfxPrefabBuilder.CreateSpriteChild(
                root.transform, "BeamHotCore", sprite, new Color(1f, 0.82f, 0.35f, 1f), SpriteDrawMode.Sliced);
            hotCore.enabled = false;

            ParticleSystem telegraphParticles = CombatVfxPrefabBuilder.CreateBeamParticleSystem(
                root.transform, "TelegraphParticles", new Color(1f, 0.55f, 0.2f, 1f),
                looping: true, rate: 48f, shapeScale: new Vector3(0.55f, 16.5f, 0f),
                startSize: 0.18f, startSpeed: 1.1f, lifetime: 0.28f);
            ParticleSystem beamParticles = CombatVfxPrefabBuilder.CreateBeamParticleSystem(
                root.transform, "BeamParticles", new Color(1f, 0.75f, 0.25f, 1f),
                looping: false, rate: 0f, shapeScale: new Vector3(0.4f, 16.5f, 0f),
                startSize: 0.22f, startSpeed: 3f, lifetime: 0.2f);
            beamParticles.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 36) });
            ParticleSystem muzzleFlash = CombatVfxPrefabBuilder.CreateMuzzleFlash(
                root.transform, "MuzzleFlash", new Color(1f, 0.9f, 0.45f, 1f));

            SerializedObject serializedLaser = new SerializedObject(laser);
            serializedLaser.FindProperty("telegraphGlowRenderer").objectReferenceValue = telegraphGlow;
            serializedLaser.FindProperty("telegraphEdgeRenderer").objectReferenceValue = telegraphEdge;
            serializedLaser.FindProperty("telegraphFillRenderer").objectReferenceValue = telegraphFill;
            serializedLaser.FindProperty("beamOuterGlowRenderer").objectReferenceValue = outerGlow;
            serializedLaser.FindProperty("beamMidGlowRenderer").objectReferenceValue = midGlow;
            serializedLaser.FindProperty("beamCoreRenderer").objectReferenceValue = core;
            serializedLaser.FindProperty("beamHotCoreRenderer").objectReferenceValue = hotCore;
            serializedLaser.FindProperty("damageCollider").objectReferenceValue = collider;
            serializedLaser.FindProperty("telegraphParticles").objectReferenceValue = telegraphParticles;
            serializedLaser.FindProperty("beamParticles").objectReferenceValue = beamParticles;
            serializedLaser.FindProperty("muzzleFlashParticles").objectReferenceValue = muzzleFlash;
            serializedLaser.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, OutputPath, out bool success);
            if (success)
            {
                Debug.Log($"EnemyBeamLaserPrefabGenerator: wrote {OutputPath}.");
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif
