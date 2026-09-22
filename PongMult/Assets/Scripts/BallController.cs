using UnityEngine;

public class BallController : MonoBehaviour
{
    public float velocidade = 5f;

    public UDPServer servidor;

    private Vector2 direcao;
    private Rigidbody2D rb;

    private Vector3 posicaoInicial;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        posicaoInicial = transform.position;

        direcao = new Vector2(1f, 0.5f).normalized;
    }

    void FixedUpdate()
    {
        rb.MovePosition(
            rb.position +
            direcao * velocidade * Time.fixedDeltaTime
        );
    }

    void OnCollisionEnter2D(Collision2D colisao)
    {
        if (colisao.gameObject.CompareTag("Wall"))
        {
            direcao.y *= -1f;
        }

        if (colisao.gameObject.CompareTag("Player"))
        {
            direcao.x *= -1f;
        }
    }

    void OnTriggerEnter2D(Collider2D colisao)
    {
        Debug.Log("Trigger detectado: " + colisao.gameObject.name);

        if (colisao.gameObject.CompareTag("GoalLeft"))
        {
            Debug.Log("GoalLeft detectado!");

            if (servidor != null)
            {
                servidor.RegistrarGol(false);
            }

            ResetarBola();
        }
        else if (colisao.gameObject.CompareTag("GoalRight"))
        {
            Debug.Log("GoalRight detectado!");

            if (servidor != null)
            {
                servidor.RegistrarGol(true);
            }

            ResetarBola();
        }
    }

    void ResetarBola()
    {
        transform.position = posicaoInicial;

        rb.position = posicaoInicial;

        if (direcao.x > 0)
        {
            direcao = new Vector2(-1f, 0.5f).normalized;
        }
        else
        {
            direcao = new Vector2(1f, 0.5f).normalized;
        }

        Debug.Log("Bola resetada para o centro!");
    }
}