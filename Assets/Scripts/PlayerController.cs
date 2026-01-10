#nullable enable

using AYellowpaper.SerializedCollections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : PlayerGameplayPawn
{
    [Header("Combat")]
    [SerializeField] private float attackRadius = 5f;
    [SerializeField] private float dashFactor = 0.2f;
    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private float flickThreshold = 50f;
    [SerializeField] private float swordCatchRadius = 1f;
    [SerializeField] private GameObject? playerDamageFX;

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
        Testing.CinemachineTrackingTargetFromGameManagerSetter.Shake();
        if (playerDamageFX == null) return;
        IAttackAnimator effect = Instantiate(playerDamageFX, transform.position, Quaternion.identity).GetComponent<IAttackAnimator>();
        if (effect != null) effect.PlayAnimation();
    }

    void SwordThrow(Vector2 direction)
    {
        ElementManager.Instance.MeleeCharge(transform, true);
        SwordProjectile.Instance.StartFlight(transform.position, direction * projectileSpeed);
        playerState = PlayerState.SwordThrown;
    }

    void RecallSword()
    {
        recallParticles.ThrowIfNull(nameof(recallParticles));
        SwordProjectile.Instance.StopFlight();
        playerState = PlayerState.MeleeReady;
        recallParticles.Stop();
    }

    void CatchSword()
    {
        SwordProjectile.Instance.StopFlight();
        playerState = PlayerState.MeleeReady;
    }
    
    private IEnumerator RecallSwordAfterDelayCoroutine(float delaySecs)
    {
        // RETROFIT: From OnHoldInIdle
        
        yield return new WaitForSeconds(delaySecs);
        
        RecallSword();
    }

    
    // PlayerGameplayPawn
    
    public override void Attack()
    {
        // RETROFIT: From OnReleaseInIdle
        
        if (playerState == PlayerState.MeleeReady)
        {
            //MeleeAttack();
            ElementManager.Instance.MeleeStrike(transform);
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
            recallSwordCoroutine = null;
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
    }

    public override void AimInDirection(Vector2 direction)
    {
        // RETROFIT: No corresponding functionality from old code
    }

    public override void DoAimedAttackInDirection(Vector2 direction)
    {
        // RETROFIT: From OnReleaseInMove

        if (playerState == PlayerState.MeleeReady)
        {
            SwordThrow(direction.normalized);
        }
    }

    public override void MoveInDirection(Vector2 direction)
    {
        // RETROFIT: From OnMove
        
        rb.ThrowIfNull(nameof(rb));

        if (direction.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        rb.linearVelocity = direction * speed;
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