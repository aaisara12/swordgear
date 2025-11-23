using System.Collections.Generic;
using UnityEngine;

public class Bumper : MonoBehaviour
{
    [SerializeField] float forceMultiplier = 1.2f;

    // Bumper will reflect an incoming object with increased speed.
    //private void OnTriggerEnter2D(Collider2D collision)
    //{
    //    Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();

    //    if (!rb)
    //    {
    //        return;
    //    }

    //    Debug.Log(collision.gameObject.name);

    //    Vector2 incomingVelocity = rb.linearVelocity;
    //    // Vector2 normal = (collision.transform.position - transform.position).normalized;
    //    Vector2 contact = collision.GetComponent<Collider2D>().ClosestPoint(transform.position);
    //    Vector2 normal = (collision.transform.position - (Vector3)contact).normalized;
    //    Vector2 reflectedVelocity = Vector2.Reflect(incomingVelocity, normal) * forceMultiplier;

    //    rb.linearVelocity = reflectedVelocity;
    //}

    Vector2 GetVelocity(Collision2D col)
    {
        BallController bc = col.transform.GetComponent<BallController>();
        SwordProjectile sw = col.transform.GetComponent<SwordProjectile>();
        if (bc)
            return bc.prevVelocity;
        if (sw)
            return sw.prevVelocity;
        return Vector2.zero;
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        Rigidbody2D rb = collision.rigidbody;
        if (!rb)
        {
            return;
        }

        Vector2 incomingVelocity = GetVelocity(collision);
        Vector2 normal = collision.contacts[0].normal;
        Vector2 reflectedVelocity = Vector2.Reflect(incomingVelocity, normal) * forceMultiplier;

        rb.linearVelocity = reflectedVelocity;
        collision.transform.up = reflectedVelocity;
    }
}
