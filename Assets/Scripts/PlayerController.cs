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
    [SerializeField] private SpriteRenderer? playerRenderer;
    public Transform DirectionTransform { get { return playerDirectionReference == null ? transform : playerDirectionReference; } }
    
    [Header("Combat")]
    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private float swordCatchRadius = 1f;
    [SerializeField] private float iFrameDuration = 1f;
    [SerializeField] private float iFrameBlinkInterval = 0.1f;
    [SerializeField] private GameObject? playerDamageFX;
    [SerializeField] private GameObject? catchExplosionFX;

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
    public float SwordCatchRadius => swordCatchRadius;
    public bool IsRecallChannelActive => recallSwordCoroutine != null;
    public bool IsSwordOut => playerState == PlayerState.SwordThrown;
    private static readonly int AnimIdleHash = Animator.StringToHash("PlayerIdle");
    private static readonly int AnimWalkSideHash = Animator.StringToHash("PlayerWalkSide");
    private static readonly int AnimWalkUpHash = Animator.StringToHash("PlayerWalkUp");
    private static readonly int AnimWalkDownHash = Animator.StringToHash("PlayerWalkDown");
    private static readonly int AnimUltVanishHash = Animator.StringToHash("PlayerUltVanish");
    private static readonly int AnimUltAppearHash = Animator.StringToHash("PlayerUltAppear");

    private Animator? animator;
    private int _currentAnimStateHash;
    private Rigidbody2D? rb;
    private float _attackCooldownRemaining = 0f;
    private float _dashCooldownRemaining = 0f;
    private float _iFrameRemaining = 0f;
    private bool _isDashing = false;
    private Coroutine? _dashCoroutine;
    private Vector2 _lastMoveDirection = Vector2.zero;
    private bool _swordHasLeftCatchRadius = false;

    private bool _isUltimateInvincible = false;
    private bool _isUltimateFrozen = false;

    private bool IsOnAttackCooldown => _attackCooldownRemaining > 0f;
    private bool IsOnDashCooldown => _dashCooldownRemaining > 0f;
    private bool IsInvincible => _isDashing || _iFrameRemaining > 0f || _isUltimateInvincible;

    public void SetUltimateInvincible(bool invincible) => _isUltimateInvincible = invincible;
    public void SetUltimateFrozen(bool frozen)
    {
        _isUltimateFrozen = frozen;
        if (frozen)
        {
            _lastMoveDirection = Vector2.zero;
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }
    }

    public IEnumerator PlayVanishAndHide()
    {
        yield return PlayAnimationState(AnimUltVanishHash);
        if (playerRenderer != null)
            playerRenderer.enabled = false;
    }

    public IEnumerator PlayAppearAndShow()
    {
        yield return PlayAnimationState(AnimUltAppearHash);
        if (playerRenderer != null)
            playerRenderer.enabled = true;
    }

    private IEnumerator PlayAnimationState(int stateHash)
    {
        if (animator == null)
            yield break;

        _currentAnimStateHash = stateHash;
        animator.Play(stateHash, 0, 0f);
        yield return null;
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
    }

    public void ApplyAttackCooldown(float seconds)
    {
        _attackCooldownRemaining = seconds;
    }

    private bool IsGameplayBlocked => PlayerGameplayManager.Instance?.IsDefeated == true;

    private void Update()
    {
        if (_attackCooldownRemaining > 0f)
            _attackCooldownRemaining -= Time.deltaTime;
        if (_dashCooldownRemaining > 0f)
            _dashCooldownRemaining -= Time.deltaTime;
        if (_iFrameRemaining > 0f)
        {
            _iFrameRemaining -= Time.deltaTime;
            if (playerRenderer != null)
            {
                Color c = playerRenderer.color;
                c.a = (int)(_iFrameRemaining / iFrameBlinkInterval) % 2 == 0 ? 1f : 0.5f;
                playerRenderer.color = c;
            }
        }
        else if (playerRenderer != null && playerRenderer.color.a < 1f)
        {
            Color c = playerRenderer.color;
            c.a = 1f;
            playerRenderer.color = c;
        }

        if (playerState == PlayerState.SwordThrown)
        {
            UpdateSwordAutoCatch();
        }
    }

    // Catches the sword automatically once it's back in range. _swordHasLeftCatchRadius gates this
    // so the sword thrown from right next to the player doesn't get caught the instant it's thrown.
    private void UpdateSwordAutoCatch()
    {
        float distance = Vector2.Distance(transform.position, SwordProjectile.Instance.transform.position);

        if (!_swordHasLeftCatchRadius)
        {
            if (distance >= swordCatchRadius)
            {
                _swordHasLeftCatchRadius = true;
            }
            return;
        }

        if (distance < swordCatchRadius)
        {
            CatchSword();
        }
    }

    private IEnumerator DashCoroutine(Vector2 direction)
    {
        _isDashing = true;
        _dashCooldownRemaining = dashCooldown;

        AudioSystem.Play(AudioSystem.Sound.Player_Dash);

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
        _isDashing = false;
        _dashCoroutine = null;
        MoveInDirection(_lastMoveDirection);
    }

    // aisara => Cancels an in-progress dash and restores the enemy-collision ignore that DashCoroutine toggles,
    // so resetting mid-dash doesn't leave the player phasing through enemies.
    private void CancelDash()
    {
        if (_dashCoroutine != null)
        {
            StopCoroutine(_dashCoroutine);
            _dashCoroutine = null;
        }

        if (_isDashing)
        {
            int playerLayer = gameObject.layer;
            int enemyLayer = LayerMask.NameToLayer(enemyPhysicsLayer);
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
            _isDashing = false;
        }
    }

    [Header("Sword Recall")]
    [SerializeField] ParticleSystem? recallParticles;
    [SerializeField] float recallTime = 1f;
    [SerializeField] float recallSpeed = 16f;
    [SerializeField] float recallMaxDuration = 3f;
    private Coroutine? recallSwordCoroutine;
    static bool _recallParticlesWarmed;

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
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component is missing!");
        }
        if (!_recallParticlesWarmed && recallParticles != null)
        {
            recallParticles.Simulate(1f, true, true);
            recallParticles.Clear(true);
            _recallParticlesWarmed = true;
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

    void OnEnable()
    {
        SwordLodgedIndicator.OnSwordLodged += HandleSwordLodged;
    }

    void HandleSwordLodged()
    {
        if (playerState != PlayerState.SwordThrown)
        {
            return;
        }

        if (swordFlightSound != -1)
        {
            AudioSystem.StopLoop(swordFlightSound);
            swordFlightSound = -1;
        }
    }

    void ForceResetThrownSword()
    {
        CancelRecallChannel();
        if (swordFlightSound != -1)
        {
            AudioSystem.StopLoop(swordFlightSound);
            swordFlightSound = -1;
        }

        if (SwordProjectile.Instance != null)
        {
            SwordProjectile.Instance.StopFlight();
        }

        playerState = PlayerState.MeleeReady;
        weaponIndicator?.SetEquippedVisible(true);
    }
    
    public void TakeDamage(float damage)
    {
        if (PlayerGameplayManager.Instance?.IsDefeated == true) return;
        if (IsInvincible) return;
        _iFrameRemaining = iFrameDuration;
        PlayDamageEffect();
        RegisterDamage(damage);
    }

    void PlayDamageEffect()
    {
        AudioSystem.Play(AudioSystem.Sound.Player_Hurt);
        Testing.CinemachineTrackingTargetFromGameManagerSetter.Shake();
        if (playerDamageFX == null) return;
        IAttackAnimator effect = PrefabPool.Instance!.Spawn(playerDamageFX, transform.position, Quaternion.identity).GetComponent<IAttackAnimator>();
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
        _swordHasLeftCatchRadius = false;
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

        if (recallParticles != null)
        {
            recallParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        AudioSystem.StopLoop(recallSoundLoop);

        SwordProjectile.Instance.StopFlight();
        playerState = PlayerState.MeleeReady;
        AudioSystem.StopLoop(swordFlightSound);
        weaponIndicator?.SetEquippedVisible(true);

        if (countAsCatch)
        {
            PlayCatchExplosion();
        }
    }

    void CatchSword()
    {
        FinishRecall(countAsCatch: true);
    }

    void PlayCatchExplosion()
    {
        if (catchExplosionFX == null)
        {
            return;
        }

        GameObject fx = Instantiate(catchExplosionFX, transform.position, Quaternion.identity);
        CatchExplosionFX? explosion = fx.GetComponent<CatchExplosionFX>();
        if (explosion != null)
        {
            explosion.Play(ElementVisuals.GetGlowColor(ElementVisuals.GetCurrentElement()));
        }
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
        if (recallParticles != null)
        {
            recallParticles.Stop();
        }

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
        SwordLodgedIndicator.OnSwordLodged -= HandleSwordLodged;

        if (playerState == PlayerState.SwordThrown)
        {
            ForceResetThrownSword();
            return;
        }

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
        if (IsGameplayBlocked)
        {
            return;
        }

        // RETROFIT: From OnReleaseInIdle

        if (playerState == PlayerState.MeleeReady && !IsOnAttackCooldown)
        {
            SyncMeleeFacingFromIndicator(direction);
            ApplyAttackCooldown(ElementManager.Instance.MeleeStrike(transform));
        }
        else if (playerState == PlayerState.SwordThrown && !IsOnDashCooldown)
        {
            // Prefer the movement joystick's current direction so this can't get dragged into an
            // enemy the weapon indicator has auto-locked onto; only fall back to facing when idle.
            Vector2 dashDirection = _lastMoveDirection.sqrMagnitude > 0.001f
                ? _lastMoveDirection
                : (weaponIndicator != null ? weaponIndicator.GetFacingDirection() : (Vector2)transform.up);
            _dashCoroutine = StartCoroutine(DashCoroutine(dashDirection.normalized));
        }
    }

    public override void BeginChargeAttack()
    {
        if (IsGameplayBlocked)
        {
            return;
        }

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
        if (IsGameplayBlocked)
        {
            return;
        }

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
        if (IsGameplayBlocked)
        {
            return;
        }

        CancelRecallChannel();
        ElementManager.Instance.MeleeCharge(transform, true);
    }

    public override void AimInDirection(Vector2 direction)
    {
        if (IsGameplayBlocked)
        {
            return;
        }

        weaponIndicator?.UpdateThrowAim(direction);
    }

    public override void DoAimedAttackInDirection(Vector2 direction)
    {
        if (IsGameplayBlocked)
        {
            return;
        }

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
            _dashCoroutine = StartCoroutine(DashCoroutine(direction.normalized));
        }
    }

    public override void StopAiming()
    {
        weaponIndicator?.EndThrowAim();
    }

    int walkSoundLoop = -1;

    void SetAnimationState(int stateHash)
    {
        if (animator == null || _currentAnimStateHash == stateHash)
        {
            return;
        }

        _currentAnimStateHash = stateHash;
        animator.Play(stateHash);
    }

    void UpdateMovementAnimation(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
        {
            SetAnimationState(AnimIdleHash);
            return;
        }

        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            SetAnimationState(AnimWalkSideHash);
        }
        else
        {
            SetAnimationState(direction.y > 0f ? AnimWalkUpHash : AnimWalkDownHash);
        }
    }

    public override void MoveInDirection(Vector2 direction)
    {
        if (IsGameplayBlocked)
        {
            return;
        }

        if (_isUltimateFrozen)
        {
            direction = Vector2.zero;
        }

        // RETROFIT: From OnMove
        _lastMoveDirection = direction;
        UpdateMovementAnimation(direction);
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

    public override void ResetForNode()
    {
        // Stop any thrown/recalling sword, cancel the recall channel + flight audio, and return to MeleeReady.
        ForceResetThrownSword();

        // Cancel an in-progress dash and restore enemy-collision ignore.
        CancelDash();

        // Stop the looping walk SFX if it was playing.
        if (walkSoundLoop != -1)
        {
            AudioSystem.StopLoop(walkSoundLoop);
            walkSoundLoop = -1;
        }

        // Clear cooldowns and movement state.
        _attackCooldownRemaining = 0f;
        _dashCooldownRemaining = 0f;
        _lastMoveDirection = Vector2.zero;
        _iFrameRemaining = 0f;
        _isUltimateInvincible = false;
        _isUltimateFrozen = false;

        if (playerRenderer != null)
        {
            Color c = playerRenderer.color;
            c.a = 1f;
            playerRenderer.color = c;
            playerRenderer.enabled = true;
        }

        // Zero out physics velocity so the pawn isn't drifting at the new spawn.
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>())
        {
            collider.enabled = true;
        }

        // Reset facing/aim to a default.
        weaponIndicator?.EndThrowAim();
        transform.up = Vector2.up;

        SetAnimationState(AnimIdleHash);

        playerState = PlayerState.MeleeReady;
    }

    public override void UseUltimate()
    {
        if (IsGameplayBlocked)
        {
            return;
        }

        UltimateChargeTracker.Instance?.TryActivate();
    }

    public override void DoSpawnAnimation()
    {
        // TODO: Implement spawn animation
    }

    private Coroutine? _defeatAnimationCoroutine;

    public override void DoDefeatAnimation()
    {
        _lastMoveDirection = Vector2.zero;
        ForceResetThrownSword();
        CancelDash();

        if (walkSoundLoop != -1)
        {
            AudioSystem.StopLoop(walkSoundLoop);
            walkSoundLoop = -1;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>())
        {
            collider.enabled = false;
        }

        weaponIndicator?.EndThrowAim();
        weaponIndicator?.SetEquippedVisible(false);

        Testing.CinemachineTrackingTargetFromGameManagerSetter.Shake(2.5f);
        AudioSystem.Play(AudioSystem.Sound.Player_Defeat);

        if (_defeatAnimationCoroutine != null)
        {
            StopCoroutine(_defeatAnimationCoroutine);
        }

        _defeatAnimationCoroutine = StartCoroutine(DefeatFadeRoutine());
    }

    private IEnumerator DefeatFadeRoutine()
    {
        if (playerRenderer == null)
        {
            yield break;
        }

        Color color = playerRenderer.color;
        float startAlpha = color.a;
        const float targetAlpha = 0.5f;
        const float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
            playerRenderer.color = color;
            yield return null;
        }

        color.a = targetAlpha;
        playerRenderer.color = color;
        _defeatAnimationCoroutine = null;
    }
}