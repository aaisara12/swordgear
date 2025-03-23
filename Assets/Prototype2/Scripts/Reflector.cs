using UnityEngine;

public class Reflector : MonoBehaviour
{
    // Weird naming, but Reflector redirects the ball towards the direction the reflector is facing, regardless of the incoming angle.


    private void OnCollisionEnter2D(Collision2D collision)
    {
        Rigidbody2D rb = collision.rigidbody;
        BallController bc = collision.transform.GetComponent<BallController>();
        if (!rb || !bc)
        {
            return;
        }

        float mag = bc.prevVelocity.magnitude;

        rb.linearVelocity = transform.up * mag;
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
