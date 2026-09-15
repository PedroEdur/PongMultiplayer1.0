using UnityEngine;

public class BallController : MonoBehaviour
{
    public float velocidade = 5f;

    private Vector2 direcao;

    void Start()
    {
        direcao = new Vector2(1f, 0.5f).normalized;
    }

    void Update()
    {
        transform.position +=
            (Vector3)(direcao * velocidade * Time.deltaTime);
    }

    void OnCollisionEnter2D(Collision2D colisao)
    {
        if (colisao.gameObject.CompareTag("Wall"))
        {
            direcao.y *= -1;
        }

        if (colisao.gameObject.CompareTag("Player"))
        {
            direcao.x *= -1;
        }
    }
}