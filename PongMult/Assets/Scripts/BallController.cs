using UnityEngine;

public class BallController : MonoBehaviour
{
    public float velocidade = 5f;

    private Vector2 direcao;

    void Start()
    {
        // Direção inicial da bola
        direcao = new Vector2(1f, 0.5f).normalized;
    }

    void Update()
    {
        // Movimenta a bola
        transform.position +=
            (Vector3)(direcao * velocidade * Time.deltaTime);
    }

    void OnCollisionEnter2D(Collision2D colisao)
    {
        // Bateu na parede de cima ou de baixo
        if (colisao.gameObject.CompareTag("Wall"))
        {
            direcao.y *= -1;
        }

        // Bateu em um dos jogadores
        if (colisao.gameObject.CompareTag("Player"))
        {
            direcao.x *= -1;
        }
    }

    void OnTriggerEnter2D(Collider2D outro)
    {
        // A bola saiu pelo lado esquerdo
        if (outro.CompareTag("GoalLeft"))
        {
            Debug.Log("PONTO DO PLAYER 2");

            ReiniciarBola();
        }

        // A bola saiu pelo lado direito
        if (outro.CompareTag("GoalRight"))
        {
            Debug.Log("PONTO DO PLAYER 1");

            ReiniciarBola();
        }
    }

    void ReiniciarBola()
    {
        // Volta para o centro
        transform.position = Vector3.zero;

        // Escolhe uma nova direção aleatória
        direcao = new Vector2(
            Random.Range(-1f, 1f),
            Random.Range(-0.5f, 0.5f)
        ).normalized;
    }
}