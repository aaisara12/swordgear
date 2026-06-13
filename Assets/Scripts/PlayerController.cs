#nullable enable

using AYellowpaper.SerializedCollections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : PlayerGameplayPawn
{
    [Header("References")]
    [SerializeField] private Transform? playerDirectionReference;  // Need this since we are no longer turning the player object when moving
    [SerializeField] private PlayerWeaponIndicator? weaponIndicator;
    public Transform DirectionTransform { get { return playerDirectionReference == null ? transform : playerDirectionReference; } }
    
    [Header("Combat")]
    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private float swordCatchRadius = 1f;
    [SerializeField] private GameObject? playerDamageFX;

    [Header("Attack Cooldowns")]
    [SerializeField] private float swordThrowCooldown = 0.5f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 10f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private string enemyPhysicsLayer = "Enemies";

    [Header("Movement")]
    [SerializeField] private float speed = 3f;

    [Header("Weapon Management")]
    [SerializeField] private SwordController? sword;
    [SerializeField] private GearController? gear;
    [SerializedDictionary("Element Type", "Weapon Prefab")]
    [SerializeField] private SerializedDictionary<Element, GameObject>? elementWeaponDict;
    Dictionary<Element, IMeleeWeapon> elementToWeapon = new Dictionary<Element, IMeleeWeapon>();

    public enum PlayerState
    {
        MeleeReady,
        SwordThrown
    }

    PlayerState playerState = PlayerState.MeleeReady;
    public bool IsMeleeReady => playerState == PlayerState.MeleeReady;
    private Rigidbody2D? rb;
    private float _attackCooldownRemaining = 0f;
    private float _dashCooldownRemaining = 0f;
    private bool _isDashing = false;

    private bool IsOnAttackCooldown => _attackCooldownRemaining > 0f;
    private bool IsOnDashCooldown => _dashCooldownRemaining > 0f;

    public void ApplyAttackCooldown(float seconds)
    {
        _attackCooldownRemaining = seconds;
    }

    private void Update()
    {
        if (_attackCooldownRemaining > 0f)
            _attackCooldownRemaining -= Time.deltaTime;
        if (_dashCooldownRemaining > 0f)
            _dashCooldownRemaining -= Time.deltaTime;
    }

    private IEnumerator DashCoroutine(Vector2 direction)
    {
        _isDashing = true;
        _dashCooldownRemaining = dashCooldown;

        int playerLayer = gameObject.layer;
        int enemyLayer = LayerMask.NameToLayer(enemyPhysicsLayer);
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            rb!.linearVelocity = direction * dashSpeed;
            elapsed += Time.deltaTime;
            yield return null;
        }

        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        rb!.linearVelocity = Vector2.zero;
        _isDashing = false;
    }

    [Header("Sword Recall")]
    [SerializeField] ParticleSystem? recallParticles;
    [SerializeField] float recallTime = 1f;
    [SerializeField] float recallSpeed = 8f;
    [SerializeField] float recallMaxDuration = 3f;
    private Coroutine? recallSwordCoroutine;

    private void Awake()
    {
        elementWeaponDict.ThrowIfNull(nameof(elementWeaponDict));
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D component is missing!");
        }
        else
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
        foreach (Element elem in elementWeaponDict.Keys)
        {
            GameObject weaponObj = Instantiate(elementWeaponDict[elem]);
            IMeleeWeapon weapon = weaponObj.GetComponent<IMeleeWeapon>();
            elementToWeapon[elem] = weapon;
        }

        if (weaponIndicator == null)
        {
            Debug.LogError("PlayerController: weaponIndicator is null");
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (_isDashing) return;
        PlayDamageEffect();
        RegisterDamage(damage);
    }

    void PlayDamageEffect()
    {
        AudioSystem.Play(AudioSystem.Sound.Player_Hurt);
        Testing.CinemachineTrackingTargetFromGameManagerSetter.Shake();
        if (playerDamageFX == null) return;
        IAttackAnimator effect = Instantiate(playerDamageFX, transform.position, Quaternion.identity).GetComponent<IAttackAnimator>();
        if (effect != null) effect.PlayAnimation();
    }

    int swordFlightSound = -1;

    void SwordThrow(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.001f && weaponIndicator != null)
        {
            direction = weaponIndicator.GetFacingDirection();
        }

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        direction = direction.normalized;

        AudioSystem.Play(AudioSystem.Sound.Throw);
        ElementManager.Instance.MeleeCharge(transform, true);
        float effectiveProjectileSpeed = projectileSpeed * (PlayerStatModifiers.Instance != null ? PlayerStatModifiers.Instance.ProjectileSpeedMultiplier : 1f);
        Vector3 throwOrigin = weaponIndicator != null ? weaponIndicator.GetThrowOrigin() : transform.position;
        weaponIndicator?.SetEquippedVisible(false);
        SwordProjectile.Instance.StartFlight(throwOrigin, direction * effectiveProjectileSpeed);
        playerState = PlayerState.SwordThrown;
        swordFlightSound = AudioSystem.PlayLoop(AudioSystem.Sound.Basic_Flight);
    }

    void SyncMeleeFacingFromIndicator(Vector2 attackDirection = default)
    {
        if (weaponIndicator == null)
        {
            return;
        }

        Vector2 direction = attackDirection.sqrMagnitude > 0.001f
            ? attackDirection.normalized
            : weaponIndicator.GetFacingDirection();

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.up = direction;
        }
    }

    void FinishRecall(bool countAsCatch)
    {
        if (playerState == PlayerState.MeleeReady)
        {
            return;
        }

        if (recallSwordCoroutine != null)
        {
            StopCoroutine(recallSwordCoroutine);
            recallSwordCoroutine = null;
        }

        recallParticles?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        AudioSystem.StopLoop(recallSoundLoop);

        SwordProjectile.Instance.StopFlight();
        playerState = PlayerState.MeleeReady;
        AudioSystem.StopLoop(swordFlightSound);
        weaponIndicator?.SetEquippedVisible(true);

        if (countAsCatch)
        {
            // Hook for future catch feedback / rewards.
        }
    }

    void CatchSword()
    {
        FinishRecall(countAsCatch: true);
    }

    int recallSoundLoop = -1;
    private IEnumerator RecallSwordAfterDelayCoroutine(float delaySecs)
    {
        // RETROFIT: From OnHoldInIdle
        recallSoundLoop = AudioSystem.PlayLoop(AudioSystem.Sound.Player_Recall);
        
        yield return new WaitForSeconds(delaySecs);
        AudioSystem.StopLoop(recallSoundLoop);
        recallSoundLoop = -1;

        recallParticles?.Stop();

        recallSwordCoroutine = null;

        float effectiveRecallSpeed = recallSpeed *
            (PlayerStatModifiers.Instance != null ? PlayerStatModifiers.Instance.ProjectileSpeedMultiplier : 1f);
        SwordProjectile.Instance.StartRecallFlight(
            transform,
            effectiveRecallSpeed,
            swordCatchRadius,
            recallMaxDuration,
            FinishRecall);
    }

    void CancelRecallChannel()
    {
        recallParticles?.Stop();

        if (recallSwordCoroutine != null)
        {
            StopCoroutine(recallSwordCoroutine);
            recallSwordCoroutine = null;
        }

        AudioSystem.StopLoop(recallSoundLoop);
        recallSoundLoop = -1;
    }

    private void OnDisable()
    {
        bool recallChannelActive = recallSwordCoroutine != null;
        bool recallFlightActive = SwordProjectile.Instance != null && SwordProjectile.Instance.IsRecalling;

        CancelRecallChannel();
        AudioSystem.StopLoop(swordFlightSound);

        if ((recallChannelActive || recallFlightActive) && SwordProjectile.Instance != null)
        {
            SwordProjectile.Instance.StopFlight();
        }
    }

    
    // PlayerGameplayPawn
    
    public override void Attack(Vector2 direction)
    {
        // RETROFIT: From OnReleaseInIdle

        if (playerState == PlayerState.MeleeReady && !IsOnAttackCooldown)
        {
            SyncMeleeFacingFromIndicator(direction);
            ApplyAttackCooldown(ElementManager.Instance.MeleeStrike(transform));
        }

        if (playerState == PlayerState.SwordThrown &&
            Vector2.Distance(transform.position, SwordProjectile.Instance.transform.position) < swordCatchRadius)
        {
            CatchSword();
        }
    }

    public override void BeginChargeAttack()
    {
        // RETROFIT: From OnTapInIdle
        
        recallParticles.ThrowIfNull(nameof(recallParticles));
        if (playerState == PlayerState.SwordThrown && !SwordProjectile.Instance.IsRecalling)
        {
            recallParticles.Play();
            recallSwordCoroutine = StartCoroutine(RecallSwordAfterDelayCoroutine(recallTime));
        }
        else if (playerState == PlayerState.MeleeReady)
        {
            //MeleeCharge();
            ElementManager.Instance.MeleeCharge(transform);
        }
    }

    public override void ReleaseChargeAttack()
    {
        bool hadRecallChannel = recallSwordCoroutine != null;
        CancelRecallChannel();

        if (!hadRecallChannel)
        {
            if (playerState == PlayerState.MeleeReady && !IsOnAttackCooldown)
            {
                SyncMeleeFacingFromIndicator();
                ApplyAttackCooldown(ElementManager.Instance.MeleeStrike(transform));
            }
        }
    }

    public override void CancelChargeAttack()
    {
        CancelRecallChannel();
        ElementManager.Instance.MeleeCharge(transform, true);
    }

    public override void AimInDirection(Vector2 direction)
    {
        weaponIndicator?.UpdateThrowAim(direction);
    }

    public override void DoAimedAttackInDirection(Vector2 direction)
    {
        // RETROFIT: From OnReleaseInMove
        if (playerState == PlayerState.MeleeReady && !IsOnAttackCooldown)
        {
            if (direction.sqrMagnitude < 0.001f && weaponIndicator != null)
            {
                direction = weaponIndicator.GetFacingDirection();
            }

            SwordThrow(direction);
            ApplyAttackCooldown(swordThrowCooldown);
        }
        else if (playerState == PlayerState.SwordThrown && !IsOnDashCooldown && direction.sqrMagnitude > 0.001f)
        {
            StartCoroutine(DashCoroutine(direction.normalized));
        }
    }

    public override void StopAiming()
    {
        weaponIndicator?.EndThrowAim();
    }

    int walkSoundLoop = -1;

    public override void MoveInDirection(Vector2 direction)
    {
        // RETROFIT: From OnMove
        if (_isDashing) return;
        rb.ThrowIfNull(nameof(rb));

        if (direction.sqrMagnitude > 0.001f)
        {
            if (walkSoundLoop == -1)
            {
                walkSoundLoop = AudioSystem.PlayLoop(AudioSystem.Sound.Player_Walking);
            }

            weaponIndicator?.SetMoveFallbackDirection(direction);
        }
        else
        {
            if (walkSoundLoop != -1)  // Sound playing, so stop sound
            {
                AudioSystem.StopLoop(walkSoundLoop);
                walkSoundLoop = -1;
            }
        }
        float effectiveSpeed = speed * (PlayerStatModifiers.Instance != null ? PlayerStatModifiers.Instance.MoveSpeedMultiplier : 1f);
        rb.linearVelocity = direction * effectiveSpeed;
    }

    public override void DoSpawnAnimation()
    {
        // TODO: Implement spawn animation
    }

    public override void DoDefeatAnimation()
    {
        // TODO: Implement defeat animation
    }
}