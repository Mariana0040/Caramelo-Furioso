using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Use se o rosto for Image (UI)

public class DogAnimator : MonoBehaviour
{
    [Header("Referências da Animação")]
    [Tooltip("Arraste o componente SpriteRenderer do rosto do cachorro aqui.")]
    [SerializeField] private SpriteRenderer dogSpriteRenderer;

    [Tooltip("Arraste todos os frames da animação de game over, em ordem.")]
    [SerializeField] private Sprite[] gameOverFrames;

    [Header("Configurações de Movimento de Game Over")]
    [Tooltip("Ponto no centro da tela para onde o cachorro vai se mover antes de animar.")]
    [SerializeField] private Transform animationStartPoint;
    [SerializeField] private float moveToPointSpeed = 20f;

    [Header("Configurações da Animação")]
    [SerializeField] private float timePerFrame = 0.15f;
    [SerializeField] private float zoomDuration = 1.5f;
    [SerializeField] private Vector3 endScale = new Vector3(50, 50, 1);

    private Coroutine animationCoroutine;
    public AudioSource audioSource;
    public AudioClip clip;
    public void PlayGameOverAnimation()
    {
        if (animationCoroutine != null) return;
        animationCoroutine = StartCoroutine(AnimateAndEngulfScreen());
    }

    private IEnumerator AnimateAndEngulfScreen()
    {
        // Impede que o GameManager continue trocando as faces do cachorro
        if (GameManager.Instance != null)
        {
            GameManager.Instance.enabled = false;
        }
        audioSource.PlayOneShot(clip);
        // --- ETAPA 1: Mover o cachorro para a posição de início da animação ---
        if (animationStartPoint != null)
        {
            while (Vector3.Distance(transform.position, animationStartPoint.position) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, animationStartPoint.position, moveToPointSpeed * Time.deltaTime);
                yield return null; // Espera o próximo frame
            }
            // Garante que o cachorro chegue exatamente na posição final
            transform.position = animationStartPoint.position;
        }

        // --- ETAPA 2: Iniciar o zoom e a troca de frames ---
        Vector3 startScale = transform.localScale;
        float elapsedTime = 0f;

        CameraShake.Instance.StartShake(zoomDuration + 1f, 0.1f);

        while (elapsedTime < zoomDuration)
        {
            // Calcula o frame atual
            int frameIndex = Mathf.FloorToInt(elapsedTime / timePerFrame);
            frameIndex = Mathf.Min(frameIndex, gameOverFrames.Length - 1);

            // Atualiza o sprite do cachorro
            if (dogSpriteRenderer != null) dogSpriteRenderer.sprite = gameOverFrames[frameIndex];

            // Interpola a escala para criar o efeito de zoom
            transform.localScale = Vector3.Lerp(startScale, endScale, elapsedTime / zoomDuration);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // --- ETAPA 3: Garantir o estado final da animação ---
        transform.localScale = endScale;
        if (dogSpriteRenderer != null) dogSpriteRenderer.sprite = gameOverFrames[gameOverFrames.Length - 1];

        Debug.Log("Animação de Game Over finalizada!");
    }
}