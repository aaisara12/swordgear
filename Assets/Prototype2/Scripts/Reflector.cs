using UnityEngine;

public class Reflector : MonoBehaviour
{
    // Weird naming, but Reflector redirects the ball towards the direction the reflector is facing, regardless of the incoming angle.

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

        float mag = GetVelocity(collision).magnitude;

        rb.linearVelocity = transform.up * mag;
        collision.transform.up = transform.up;
    }

    //private void OnTriggerEnter2D(Collider2D collision)
    //{
    //    Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();

    //    if (!rb)
    //    {
    //        return;
    //    }

    //    float mag = rb.linearVelocity.magnitude;
    //    rb.linearVelocity = transform.up * mag;
    //}
}
