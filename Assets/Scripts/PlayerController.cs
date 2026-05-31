#nullable enable

using AYellowpaper.SerializedCollections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : PlayerGameplayPawn
{
    [Header("References")]
    [SerializeField] private Transform? playerDirectionReference;  // Need this since we are no longer turning the player object when moving
    public Transform DirectionTransform { get { return playerDirectionReference == null ? transform : playerDirectionReference; } }
    
    [Header("Combat")]
    [SerializeField] private float attackRadius = 5f;
    [SerializeField] private float dashFactor = 0.2f;
    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private float flickThreshold = 50f;
    [SerializeField] private float swordCatchRadius = 1f;
    [SerializeField] private GameObject? playerDamageFX;

    [Header("Attack Cooldowns")]
    [SerializeField] private float swordThrowCooldown = 0.5f;

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
    private Rigidbody2D? rb;
    private float _attackCooldownRemaining = 0f;

    private bool IsOnAttackCooldown => _attackCooldownRemaining > 0f;

    public void ApplyAttackCooldown(float seconds)
    {
        _attackCooldownRemaining = seconds;
    }

    private void Update()
    {
        if (_attackCooldownRemaining > 0f)
            _attackCooldownRemaining -= Time.deltaTime;
    }

    [Header("Sword Recall")]
    [SerializeField] ParticleSystem? recallParticles;
    [SerializeField] float recallTime = 1f;
    private Coroutine? recallSwordCoroutine;

    private void Awake()
    {
        elementWeaponDict.ThrowIfNull(nameof(elementWeaponDict));
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D component is missing!");
        }
        foreach (Element elem in elementWeaponDict.Keys)
        {
            GameObject weaponObj = Instantiate(elementWeaponDict[elem]);
            IMeleeWeapon weapon = weaponObj.GetComponent<IMeleeWeapon>();
            elementToWeapon[elem] = weapon;
        }
    }
    
    public void TakeDamage(float damage)
    {
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
        AudioSystem.Play(AudioSystem.Sound.Throw);
        ElementManager.Instance.MeleeCharge(transform, true);
        float effectiveProjectileSpeed = projectileSpeed * (PlayerStatModifiers.Instance != null ? PlayerStatModifiers.Instance.ProjectileSpeedMultiplier : 1f);
        SwordProjectile.Instance.StartFlight(transform.position, direction * effectiveProjectileSpeed);
        playerState = PlayerState.SwordThrown;
        // TODO: Add per-element handling for starting & stopping flight
        swordFlightSound = AudioSystem.PlayLoop(AudioSystem.Sound.Basic_Flight);
    }

    void RecallSword()
    {
        recallParticles.ThrowIfNull(nameof(recallParticles));
        SwordProjectile.Instance.StopFlight();
        playerState = PlayerState.MeleeReady;
        recallParticles.Stop();
        AudioSystem.StopLoop(swordFlightSound);
    }

    void CatchSword()
    {
        SwordProjectile.Instance.StopFlight();
        playerState = PlayerState.MeleeReady;
        AudioSystem.StopLoop(swordFlightSound);
    }

    int recallSoundLoop;
    private IEnumerator RecallSwordAfterDelayCoroutine(float delaySecs)
    {
        // RETROFIT: From OnHoldInIdle
        recallSoundLoop = AudioSystem.PlayLoop(AudioSystem.Sound.Player_Recall);
        
        yield return new WaitForSeconds(delaySecs);
        AudioSystem.StopLoop(recallSoundLoop);
        
        RecallSword();
    }

    
    // PlayerGameplayPawn
    
    public override void Attack(Vector2 direction)
    {
        // RETROFIT: From OnReleaseInIdle

        if (playerState == PlayerState.MeleeReady && !IsOnAttackCooldown)
        {
            //MeleeAttack();
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
        if (playerState == PlayerState.SwordThrown)
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
        recallParticles?.Stop();
        
        if (recallSwordCoroutine != null)
        {
            StopCoroutine(recallSwordCoroutine);
            AudioSystem.StopLoop(recallSoundLoop);
            recallSwordCoroutine = null;
        }
        else
        {
            if (playerState == PlayerState.MeleeReady && !IsOnAttackCooldown)
            {
                //MeleeAttack();
                ApplyAttackCooldown(ElementManager.Instance.MeleeStrike(transform));
            }
        }
    }

    public override void CancelChargeAttack()
    {
        recallParticles?.Stop();

        if (recallSwordCoroutine != null)
        {
            StopCoroutine(recallSwordCoroutine);
            recallSwordCoroutine = null;
        }
        ElementManager.Instance.MeleeCharge(transform, true);
    }

    public override void AimInDirection(Vector2 direction)
    {
        // RETROFIT: No corresponding functionality from old code
    }

    public override void DoAimedAttackInDirection(Vector2 direction)
    {
        // RETROFIT: From OnReleaseInMove

        if (playerState == PlayerState.MeleeReady && !IsOnAttackCooldown)
        {
            SwordThrow(direction.normalized);
            ApplyAttackCooldown(swordThrowCooldown);
        }
    }

    public override void StopAiming()
    {
        // RETROFIT: No corresponding functionality from old code
    }

    int walkSoundLoop = -1;

    public override void MoveInDirection(Vector2 direction)
    {
        // RETROFIT: From OnMove
        
        rb.ThrowIfNull(nameof(rb));

        if (direction.sqrMagnitude > 0.001f)
        {
            if (walkSoundLoop == -1)  // No sound playing, so start sound
            {
                walkSoundLoop = AudioSystem.PlayLoop(AudioSystem.Sound.Player_Walking);
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
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