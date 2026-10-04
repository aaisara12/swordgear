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
    [SerializeField] private PlayerAimIndicator? aimIndicator;
    [SerializeField] private SpriteRenderer? playerRenderer;
    public Transform DirectionTransform { get { return playerDirectionReference == null ? transform : playerDirectionReference; } }
    public float DashDistance => dashSpeed * dashDuration;
    private float EffectiveDashCooldown => dashCooldown *
        (PlayerStatModifiers.Instance != null ? Mathf.Max(0.05f, PlayerStatModifiers.Instance.DashCooldownMultiplier) : 1f);
    
    [Header("Combat")]
    [SerializeField] private float swordCatchRadius = 1f;
    [SerializeField] private float iFrameDuration = 1f;
    [SerializeField] private float iFrameBlinkInterval = 0.1f;
    [SerializeField] private GameObject? playerDamageFX;
    [SerializeField] private GameObject? catchExplosionFX;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 10f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private string enemyPhysicsLayer = "Enemies";

    [Header("Dash Juice")]
    [SerializeField] private float afterimageFadeTime = 0.2f;     // how long each echo lingers
    [SerializeField] private float afterimageStartAlpha = 0.5f;   // echo opacity at spawn
    // 4 echoes at these fractions of the dash. Gaps shrink over the dash (0.4, 0.3, 0.2), so the ghosts are
    // spaced wide at the launch and bunch up toward the end — reads like a decelerating burst.
    private static readonly float[] AfterimageTimes = { 0f, 0.4f, 0.7f, 0.9f };

    [Header("Movement")]
    [SerializeField] private float speed = 3f;

    [Header("Weapon Management")]
    [SerializeField] private SwordController? sword;
    [SerializeField] private GearController? gear;
    [Tooltip("Optional Light2D flash popped on dash/blink for a bit of motion juice.")]
    [SerializeField] private LightFlash? dashLight;
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
    private static readonly int AnimAttackSideHash = Animator.StringToHash("PlayerAttackSide");

    private Animator? animator;
    private int _currentAnimStateHash;
    private bool _isAttacking = false;
    private Coroutine? _attackAnimationCoroutine;
    private Rigidbody2D? rb;
    private float _attackCooldownRemaining = 0f;
    private float _dashCooldownRemaining = 0f;
    private float _iFrameRemaining = 0f;
    private bool _isDashing = false;
    private Coroutine? _dashCoroutine;
    private Vector2 _lastMoveDirection = Vector2.zero;
    private Vector2 _lastFacingDir = Vector2.up;   // last non-zero move dir — the dash's fallback when idle
    private bool _isAimLocked = false;
    private Vector2 _stickDirection = Vector2.zero; // raw movement stick, kept live through the root so
                                                    // releasing it can resume movement without new input
    private bool _swordHasLeftCatchRadius = false;

    public override Vector2 MoveDirection => _lastMoveDirection;

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

    /// <summary>
    /// Instantly hands control and visibility back. Safety net for an ultimate whose sequence is cut short —
    /// without it the player is left frozen, invincible and invisible for the rest of the run.
    /// </summary>
    public void CancelUltimateState()
    {
        SetUltimateInvincible(false);
        SetUltimateFrozen(false);
        if (playerRenderer != null)
            playerRenderer.enabled = true;
    }

    public IEnumerator PlayVanishAndHide()
    {
        yield return PlayAnimationState(AnimUltVanishHash);
        if (playerRenderer != null)
            playerRenderer.enabled = false;
    }

    public IEnumerator PlayAppearAndShow()
    {
        if (playerRenderer != null)
            playerRenderer.enabled = true;
        yield return PlayAnimationState(AnimUltAppearHash);
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

        UpdateAimLock();
    }

    /// <summary>
    /// Drives the movement lock for elements that root the player while charging (Earth's ballista).
    /// </summary>
    /// <remarks>
    /// The lock is PULLED from the active weapon every frame rather than pushed by a charge start/end
    /// event. However the charge ends — release, cancel, node change, death, element switch, a future
    /// dash — the weapon stops reporting the lock and movement returns here, so none of those paths need
    /// to know the root exists.
    /// <para>
    /// Both edges have to act immediately because movement is event-driven: HandleMove only fires when
    /// the stick CHANGES. Waiting for the next input event would let the player slide through the root on
    /// entry, and leave them frozen after it ends until they physically moved the stick again.
    /// </para>
    /// </remarks>
    private void UpdateAimLock()
    {
        bool locked = ElementManager.Instance != null && ElementManager.Instance.IsAimLocked;

        if (locked != _isAimLocked)
        {
            _isAimLocked = locked;

            // Every charge starts on auto-aim, and normal movement must never inherit a charge's aim.
            weaponIndicator?.ClearManualAim();

            if (locked)
            {
                HaltMovement();
                UpdateMovementAnimation(Vector2.zero);
            }
            else
            {
                aimIndicator?.Clear();

                // Replay the stick, the same way DashCoroutine resumes movement when the dash ends.
                MoveInDirection(_stickDirection);
                return;
            }
        }

        if (!_isAimLocked)
        {
            return;
        }

        // Hold position, and draw the aim line wherever the weapon indicator is pointing. The indicator is
        // the single source of truth for facing across every element — including its auto-aim onto a
        // nearby enemy — and SyncMeleeFacingFromIndicator hands that same direction to the weapon on
        // release, so the line can't disagree with where the shot goes.
        if (rb != null) rb.linearVelocity = Vector2.zero;

        Vector2 aim = weaponIndicator != null ? weaponIndicator.GetFacingDirection() : (Vector2)transform.up;
        if (aim.sqrMagnitude > 0.001f)
        {
            aimIndicator?.SetAim(aim, PlayerAimIndicator.AimMode.Ranged);
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
        _dashCooldownRemaining = EffectiveDashCooldown;

        AudioSystem.Play(AudioSystem.Sound.Player_Dash);
        dashLight?.Flash();

        int playerLayer = gameObject.layer;
        int enemyLayer = LayerMask.NameToLayer(enemyPhysicsLayer);
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);

        float elapsed = 0f;
        int nextImage = 0;
        while (elapsed < dashDuration)
        {
            rb!.linearVelocity = direction * dashSpeed;
            while (nextImage < AfterimageTimes.Length && elapsed >= AfterimageTimes[nextImage] * dashDuration)
            {
                SpawnAfterimage();
                nextImage++;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        while (nextImage < AfterimageTimes.Length) // guarantee all 4 echoes even on a very short dash
        {
            SpawnAfterimage();
            nextImage++;
        }

        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        _isDashing = false;
        _dashCoroutine = null;
        MoveInDirection(_lastMoveDirection);
    }

    // The dash goes where the MOVEMENT stick points; with no movement, it goes where the player last faced.
    // Deliberately ignores the throw aim-assist (which snaps toward the nearest enemy) — dashing should never
    // yank the player toward an enemy they didn't aim at.
    private Vector2 GetDashDirection()
    {
        if (_lastMoveDirection.sqrMagnitude > 0.001f)
        {
            return _lastMoveDirection.normalized;
        }

        return _lastFacingDir.sqrMagnitude > 0.001f ? _lastFacingDir.normalized : Vector2.up;
    }

    // Instant reposition primitive for element dash-overrides (e.g. Thunderstep's blink-to-sword): teleport,
    // kill momentum, leave a ghost at the launch point, and put the dash on cooldown with brief i-frames.
    public void BlinkTo(Vector2 position)
    {
        _dashCooldownRemaining = EffectiveDashCooldown;
        AudioSystem.Play(AudioSystem.Sound.Player_Dash);
        dashLight?.Flash();
        SpawnAfterimage();
        transform.position = position;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        _iFrameRemaining = Mathf.Max(_iFrameRemaining, 0.3f);
    }

    // Grants i-frames without a dash (used by weapon-driven repositions like Lightning's iaido dash, which
    // moves the player itself rather than going through DashCoroutine). Never shortens an active window.
    public void GrantIFrames(float seconds)
    {
        _iFrameRemaining = Mathf.Max(_iFrameRemaining, seconds);
    }

    /// <summary>Shows/hides the equipped blade — Lightning sheathes it while the attack is held.</summary>
    public void SetWeaponVisible(bool visible)
    {
        weaponIndicator?.SetEquippedVisible(visible);
    }

    // Force-catch the thrown sword now (used by dash-overrides that blink to it): cancel any in-progress
    // recall channel, guarantee the auto-catch gate passes, then run the normal catch (cleave + pickup).
    public void CatchThrownSword()
    {
        if (playerState != PlayerState.SwordThrown)
        {
            return;
        }

        CancelRecallChannel();
        _swordHasLeftCatchRadius = true;
        CatchSword();
    }

    // Fading ghost silhouette of the player body, tinted to the live element — the dash's motion echo.
    private void SpawnAfterimage()
    {
        if (playerRenderer == null || playerRenderer.sprite == null)
        {
            return;
        }

        var go = new GameObject("DashAfterimage");
        Transform src = playerRenderer.transform;
        go.transform.SetPositionAndRotation(src.position, src.rotation);
        go.transform.localScale = src.lossyScale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = playerRenderer.sprite;
        sr.flipX = playerRenderer.flipX;
        sr.flipY = playerRenderer.flipY;
        sr.sortingLayerID = playerRenderer.sortingLayerID;
        sr.sortingOrder = playerRenderer.sortingOrder - 1; // just behind the player

        Element el = ElementManager.Instance != null ? ElementManager.Instance.ActiveElement : Element.Physical;
        Color tint = ElementVisuals.GetGlowColor(el);
        tint.a = afterimageStartAlpha;
        sr.color = tint;

        StartCoroutine(FadeAfterimage(sr));
    }

    private IEnumerator FadeAfterimage(SpriteRenderer sr)
    {
        float t = 0f;
        float startAlpha = sr.color.a;
        while (t < afterimageFadeTime)
        {
            if (sr == null)
            {
                yield break;
            }

            Color c = sr.color;
            c.a = Mathf.Lerp(startAlpha, 0f, t / afterimageFadeTime);
            sr.color = c;
            t += Time.deltaTime;
            yield return null;
        }

        if (sr != null)
        {
            Destroy(sr.gameObject);
        }
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
            // Continuous collision so the fast dash can't tunnel straight through thin wall colliders.
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
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

    // The aimed flick no longer throws the sword — it picks an arc off the gear ring. Cancels any melee
    // charge so the flick can't double as a charged strike, and gives no-ops (empty arc) a silent whiff.
    void GrabElementFromGear(Vector2 direction)
    {
        GearManager? gear = GearManager.Instance;
        if (gear == null || direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        if (!gear.TryGrantElementFromDirection(direction))
        {
            return;
        }

        ElementManager.Instance.OnCharge(transform, true);
        AudioSystem.Play(AudioSystem.Sound.Bounce);
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
            ElementManager.Instance.Cleave(transform);
        }
    }

    void CatchSword()
    {
        FinishRecall(countAsCatch: true);
    }

    void PlayCatchExplosion()
    {
        // The catch is the identity payoff of the throw->recall loop — give it a brief hit-stop so it lands
        // with weight. Camera shake / SFX are intentionally left to the sword catch attack, which owns that beat.
        HitStop.Do(0.045f);

        if (catchExplosionFX == null)
        {
            return;
        }

        GameObject fx = Instantiate(catchExplosionFX, transform.position, Quaternion.identity, transform);
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
        CancelAttackAnimation();

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
            TryAttack();
        }
        else if (playerState == PlayerState.SwordThrown && !IsOnDashCooldown)
        {
            // Let the active element override the dash (e.g. Lightning's Thunderstep blink); else normal dash.
            if (!(ElementManager.Instance != null && ElementManager.Instance.TryOverrideDash(this)))
            {
                _dashCoroutine = StartCoroutine(DashCoroutine(GetDashDirection()));
            }
        }
    }

    /// <summary>
    /// Runs the active element's attack, and animates only if it actually attacked.
    /// </summary>
    /// <remarks>
    /// A zero cooldown means the weapon declined — Earth returns it for a press released before its
    /// ballista finished building. Swinging for a shot that never fired would read as a bug.
    /// </remarks>
    private void TryAttack()
    {
        float cooldown = ElementManager.Instance.OnTap(transform);
        if (cooldown <= 0f)
        {
            return;
        }

        ApplyAttackCooldown(cooldown);
        PlayAttackAnimation();
    }

    public override void BeginPressCharge()
    {
        if (IsGameplayBlocked)
        {
            return;
        }

        // Only for elements with no tap attack. Everything else must keep waiting for the tap/hold split,
        // or a tap would root the player and fire a charge.
        if (playerState != PlayerState.MeleeReady
            || ElementManager.Instance == null
            || !ElementManager.Instance.ChargesOnPress)
        {
            return;
        }

        // Idempotent: BeginChargeAttack still arrives when the hold validates, and the weapon ignores it
        // because it is already charging — so the ramp keeps counting from the press, not from the split.
        ElementManager.Instance.OnCharge(transform);
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
            // Tint the charge VFX to the live imbue so it matches the sword's element colour (trails inherit it).
            var recallMain = recallParticles.main;
            recallMain.startColor = ElementVisuals.GetColor(
                ElementManager.Instance != null ? ElementManager.Instance.ActiveElement : Element.Physical);
            recallParticles.Play();
            recallSwordCoroutine = StartCoroutine(RecallSwordAfterDelayCoroutine(recallTime));
        }

        else if (playerState == PlayerState.MeleeReady)
        {
            ElementManager.Instance.OnCharge(transform);
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
                TryAttack();
            }
            else
            {
                // The attack was swallowed — on cooldown, or the sword is out — but the charge still
                // ended. Without this the weapon would keep reporting its charge, and an element that
                // roots while charging (Earth) would strand the player with no way to move.
                ElementManager.Instance.OnCharge(transform, cancel: true);
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
        ElementManager.Instance.OnCharge(transform, true);
    }

    public override void AimInDirection(Vector2 direction)
    {
        if (IsGameplayBlocked)
        {
            return;
        }

        weaponIndicator?.UpdateThrowAim(direction);

        if (IsSwordOut)
        {
            GearManager.Instance?.ClearArcHighlight();

            if (direction.sqrMagnitude > 0.001f)
            {
                aimIndicator?.SetAim(direction, PlayerAimIndicator.AimMode.Dash);
            }

            return;
        }

        // Sword stays equipped, so the aimed flick is a gear grab, not a throw: preview it by lighting up
        // the arc it would land on instead of drawing the (now meaningless) throw trajectory.
        aimIndicator?.Clear();
        GearManager.Instance?.HighlightArcForDirection(direction);
    }

    public override void DoAimedAttackInDirection(Vector2 direction)
    {
        if (IsGameplayBlocked)
        {
            return;
        }

        // RETROFIT: From OnReleaseInMove
        if (playerState == PlayerState.MeleeReady)
        {
            GrabElementFromGear(direction);
        }
        else if (playerState == PlayerState.SwordThrown && !IsOnDashCooldown && direction.sqrMagnitude > 0.001f)
        {
            // Aimed dash (right-stick hold-drag): honour the explicit aim direction. GetDashDirection() is
            // only for the no-aim tap-dash — here the player is actively steering the dash.
            if (!(ElementManager.Instance != null && ElementManager.Instance.TryOverrideDash(this)))
            {
                _dashCoroutine = StartCoroutine(DashCoroutine(direction.normalized));
            }
        }
    }

    public override void StopAiming()
    {
        weaponIndicator?.EndThrowAim();
        aimIndicator?.Clear();
        GearManager.Instance?.ClearArcHighlight();
    }

    int walkSoundLoop = -1;

    private void StopWalkSound()
    {
        if (walkSoundLoop == -1)
        {
            return;
        }

        AudioSystem.StopLoop(walkSoundLoop);
        walkSoundLoop = -1;
    }

    /// <summary>Brings the player to a stop this frame.</summary>
    /// <remarks>
    /// Movement is event-driven — HandleMove only fires when the stick CHANGES — so anything that stops
    /// the player has to zero the velocity itself rather than wait for the next input event.
    /// </remarks>
    private void HaltMovement()
    {
        _lastMoveDirection = Vector2.zero;
        StopWalkSound();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    void SetAnimationState(int stateHash)
    {
        if (animator == null || _currentAnimStateHash == stateHash)
        {
            return;
        }

        _currentAnimStateHash = stateHash;
        animator.Play(stateHash);
    }

    void PlayAttackAnimation()
    {
        if (animator == null)
        {
            return;
        }

        if (_attackAnimationCoroutine != null)
        {
            StopCoroutine(_attackAnimationCoroutine);
        }

        _attackAnimationCoroutine = StartCoroutine(AttackAnimationRoutine());
    }

    private IEnumerator AttackAnimationRoutine()
    {
        _isAttacking = true;
        yield return PlayAnimationState(AnimAttackSideHash);
        _isAttacking = false;
        _attackAnimationCoroutine = null;
        UpdateMovementAnimation(_lastMoveDirection);
    }

    void CancelAttackAnimation()
    {
        if (_attackAnimationCoroutine != null)
        {
            StopCoroutine(_attackAnimationCoroutine);
            _attackAnimationCoroutine = null;
        }

        _isAttacking = false;
    }

    void UpdateMovementAnimation(Vector2 direction)
    {
        if (_isAttacking)
        {
            return;
        }

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

        // Kept live even while rooted, so ending the root can resume movement from the stick's real
        // position without waiting for the player to move it again.
        _stickDirection = direction;

        if (_isUltimateFrozen)
        {
            HaltMovement();
            return;
        }

        // While a charging element roots the player, the movement stick aims instead of moving. Same
        // shape as the ultimate freeze above — swallow the input and hold still — but the direction is
        // handed to the weapon rather than discarded.
        if (_isAimLocked)
        {
            // Steer the weapon indicator rather than the weapon: the indicator resolves the final facing
            // and the weapon reads it back at release. Steering counts as MANUAL aim, which outranks
            // auto-aim — otherwise a nearby enemy would quietly steal the shot the player is lining up,
            // and the root would exist to enable an aim the player can't actually control.
            if (direction.sqrMagnitude > 0.001f)
            {
                _lastFacingDir = direction.normalized;
                weaponIndicator?.SetMoveFallbackDirection(direction);
                weaponIndicator?.SetManualAim(direction);
            }
            else
            {
                // Stick released mid-charge: hand the aim back to auto-aim rather than freezing on the
                // last direction pushed.
                weaponIndicator?.ClearManualAim();
            }

            HaltMovement();
            UpdateMovementAnimation(Vector2.zero);
            return;
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

            _lastFacingDir = direction.normalized;
            weaponIndicator?.SetMoveFallbackDirection(direction);
        }
        else
        {
            StopWalkSound();
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

        // Cancel an in-progress attack animation so it doesn't keep blocking movement anims after reset.
        CancelAttackAnimation();

        // Clear any held melee charge. Without this a weapon holding charge state across the reset (e.g.
        // Lightning's sheathed katana) would fire its release attack on the next tap in the new node.
        if (ElementManager.Instance != null)
        {
            ElementManager.Instance.OnCharge(transform, cancel: true);
        }

        StopWalkSound();

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
        aimIndicator?.Clear();
        GearManager.Instance?.ClearArcHighlight();
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
        CancelAttackAnimation();

        if (ElementManager.Instance != null)
        {
            ElementManager.Instance.OnCharge(transform, cancel: true);
        }

        StopWalkSound();

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