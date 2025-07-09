using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    // Usaremos um Singleton para que qualquer script possa chamar o tremor facilmente.
    public static CameraShake Instance { get; private set; }

    private Vector3 originalPosition;
    private Coroutine shakeCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        // Salva a posição original da câmera.
        // Usamos localPosition para o caso de a câmera ser filha de outro objeto.
        originalPosition = transform.localPosition;
    }

    // Método público que outros scripts chamarão para iniciar o tremor
    public void StartShake(float duration, float magnitude)
    {
        // Se já estiver tremendo, para a corrotina antiga antes de começar uma nova
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }

        shakeCoroutine = StartCoroutine(Shake(duration, magnitude));
    }

    private IEnumerator Shake(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        // Enquanto a duração do tremor não tiver acabado
        while (elapsed < duration)
        {
            // Gera um deslocamento aleatório
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            // Aplica o deslocamento à posição original da câmera
            transform.localPosition = new Vector3(originalPosition.x + x, originalPosition.y + y, originalPosition.z);

            // Incrementa o tempo decorrido
            elapsed += Time.deltaTime;

            // Espera até o próximo frame
            yield return null;
        }

        // Ao final, garante que a câmera volte exatamente para a posição original
        transform.localPosition = originalPosition;
        shakeCoroutine = null; // Limpa a referência da corrotina
    }
}