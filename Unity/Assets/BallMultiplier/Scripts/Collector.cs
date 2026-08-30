using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Collector : MonoBehaviour
{
    [SerializeField] private int collected;

    public int Collected => collected;

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Ball ball = other.GetComponent<Ball>();
        if (ball == null)
            return;

        collected++;
        Destroy(ball.gameObject);
    }

    public void ResetCounter()
    {
        collected = 0;
    }
}
