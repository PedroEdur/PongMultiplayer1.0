
using System.Collections;
using UnityEngine;

public class BallController : MonoBehaviour
{
    [Header("Movimento da bola")]
    public float velocidade = 5f;

    [Header("Servidor")]
    public UDPServer servidor;

    private Vector2 direcao;
    private Rigidbody2D rb;

    private Vector3 posicaoInicial;

    private bool golEmProcessamento = false;

    // =====================================================
    // INICIALIZAÇÃO
    // =====================================================

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError(
                "ERRO: a bola precisa de um Rigidbody2D."
            );

            return;
        }

        posicaoInicial = transform.position;

        direcao =
            new Vector2(1f, 0.5f).normalized;
    }

    // =====================================================
    // MOVIMENTO
    // =====================================================

    void FixedUpdate()
    {
        if (rb == null ||
            golEmProcessamento)
        {
            return;
        }

        rb.MovePosition(
            rb.position +
            direcao *
            velocidade *
            Time.fixedDeltaTime
        );
    }

    // =====================================================
    // COLISÕES
    // =====================================================

    void OnCollisionEnter2D(Collision2D colisao)
    {
        if (golEmProcessamento)
            return;

        if (colisao.gameObject.CompareTag("Wall"))
        {
            direcao.y *= -1f;
        }

        if (colisao.gameObject.CompareTag("Player"))
        {
            direcao.x *= -1f;
        }
    }

    // =====================================================
    // DETECTAR GOL
    // =====================================================

    void OnTriggerEnter2D(Collider2D colisao)
    {
        if (golEmProcessamento)
            return;

        if (colisao.CompareTag("GoalLeft"))
        {
            // A bola entrou no gol esquerdo.
            // O jogador 2 marcou.
            ProcessarGol(false);
        }
        else if (colisao.CompareTag("GoalRight"))
        {
            // A bola entrou no gol direito.
            // O jogador 1 marcou.
            ProcessarGol(true);
        }
    }

    // =====================================================
    // PROCESSAR GOL
    // =====================================================

    void ProcessarGol(bool golDoJogador1)
    {
        if (golEmProcessamento)
            return;

        golEmProcessamento = true;

        if (servidor == null)
        {
            Debug.LogError(
                "ERRO: o servidor não está conectado " +
                "no BallController."
            );

            ResetarBola();

            StartCoroutine(
                LiberarProcessamentoDoGol()
            );

            return;
        }

        servidor.RegistrarGol(
            golDoJogador1
        );

        ResetarBola();

        StartCoroutine(
            LiberarProcessamentoDoGol()
        );
    }

    // =====================================================
    // RESETAR BOLA
    // =====================================================

    void ResetarBola()
    {
        if (rb == null)
            return;

        transform.position = posicaoInicial;
        rb.position = posicaoInicial;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        if (direcao.x > 0)
        {
            direcao =
                new Vector2(-1f, 0.5f).normalized;
        }
        else
        {
            direcao =
                new Vector2(1f, 0.5f).normalized;
        }

        Debug.Log(
            "Bola resetada para o centro."
        );
    }

    // =====================================================
    // PROTEÇÃO CONTRA DUPLO GOL
    // =====================================================

    IEnumerator LiberarProcessamentoDoGol()
    {
        yield return new WaitForSeconds(0.3f);

        golEmProcessamento = false;
    }
}