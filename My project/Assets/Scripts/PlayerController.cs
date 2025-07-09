using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Adicionado para o caso de precisar interagir com componentes de UI

public class PlayerController : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private Transform centralPosition;
    [SerializeField] private Transform dogDeliveryPosition;

    [Header("Sprites Direcionais")]
    [Tooltip("O componente que mostra o sprite do motoqueiro.")]
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [SerializeField] private Sprite spriteUp;
    [SerializeField] private Sprite spriteDown;
    [SerializeField] private Sprite spriteLeft;
    [SerializeField] private Sprite spriteRight;
    [SerializeField] private Sprite spriteIdle;

    private bool isMoving = false;
    private Sprite lastDirectionSprite;

    void Start()
    {
        if (playerSpriteRenderer != null)
        {
            playerSpriteRenderer.sprite = spriteIdle;
        }
    }

    // O MÉTODO UPDATE FOI REMOVIDO COMPLETAMENTE.
    // A LÓGICA AGORA ESTÁ EM FUNÇÕES PÚBLICAS ABAIXO.

    // --- FUNÇÕES PÚBLICAS PARA OS BOTÕES DA UI ---

    public void OnPressMoveUp()
    {
        if (isMoving) return; // Se já está se movendo, não faz nada
        SetSpriteAndSelectStation(spriteUp, GameManager.FoodStation.Up);
    }

    public void OnPressMoveDown()
    {
        if (isMoving) return;
        SetSpriteAndSelectStation(spriteDown, GameManager.FoodStation.Down);
    }

    public void OnPressMoveLeft()
    {
        if (isMoving) return;
        SetSpriteAndSelectStation(spriteLeft, GameManager.FoodStation.Left);
    }

    public void OnPressMoveRight()
    {
        if (isMoving) return;
        SetSpriteAndSelectStation(spriteRight, GameManager.FoodStation.Right);
    }

    public void OnPressDeliver()
    {
        if (isMoving) return;

        // Ao entregar, ele deve olhar para cima (em direção ao cachorro)
        playerSpriteRenderer.sprite = spriteUp;
        lastDirectionSprite = spriteUp; // <<< IMPORTANTE: Define o lastDirectionSprite para a volta
        GameManager.Instance.SubmitSequence();
        StartCoroutine(MoveToTargetAndBack(dogDeliveryPosition));
    }

    // --- FUNÇÕES PRIVADAS DE LÓGICA (permanecem iguais) ---

    private void SetSpriteAndSelectStation(Sprite directionSprite, GameManager.FoodStation station)
    {
        lastDirectionSprite = directionSprite; // Guarda o sprite de ida
        playerSpriteRenderer.sprite = directionSprite;

        GameManager.Instance.PlayerSelectedStation(station);
        Transform targetTransform = GameManager.Instance.GetTransformForStation(station);
        if (targetTransform != null)
        {
            StartCoroutine(MoveToTargetAndBack(targetTransform));
        }
    }

    private IEnumerator MoveToTargetAndBack(Transform target)
    {
        isMoving = true;
        Vector3 startPosition = centralPosition.position;

        // --- IDA PARA O ALVO ---
        while (Vector3.Distance(transform.position, target.position) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = target.position;

        yield return new WaitForSeconds(0.1f);

        // --- VOLTA PARA O CENTRO ---
        if (lastDirectionSprite == spriteUp) playerSpriteRenderer.sprite = spriteDown;
        else if (lastDirectionSprite == spriteDown) playerSpriteRenderer.sprite = spriteUp;
        else if (lastDirectionSprite == spriteLeft) playerSpriteRenderer.sprite = spriteRight;
        else if (lastDirectionSprite == spriteRight) playerSpriteRenderer.sprite = spriteLeft;

        while (Vector3.Distance(transform.position, startPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, startPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = startPosition;

        playerSpriteRenderer.sprite = spriteIdle;

        isMoving = false;
    }
}