using UnityEngine;

// HeightCanvas — mantiene la interfaz flotando a una altura fija sobre el
// dron y la orienta hacia el usuario (billboarding) para que el texto sea
// siempre legible.
public class HeightCanvas : MonoBehaviour
{
    [Header("Referencias Espaciales")]
    [Tooltip("El objeto base (Dron) sobre el que flotará este Canvas.")]
    [SerializeField] private Transform objetivoDron;

    [Tooltip("La cámara del usuario. Se auto-asigna si se deja vacía.")]
    [SerializeField] private Transform camaraUsuario;

    [Header("Parámetros Geométricos")]
    [Tooltip("Distancia vertical absoluta (en metros) sobre el dron.")]
    [SerializeField] private float altitudAbsoluta = 0.4f;

    [Tooltip("Bloquea la inclinación vertical para que el cartel no rote hacia el suelo/techo.")]
    [SerializeField] private bool bloquearEjeVertical = false;

    void Start()
    {
        if (camaraUsuario == null && Camera.main != null)
        {
            camaraUsuario = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (objetivoDron == null || camaraUsuario == null) return;

        // Anclaje en Y absoluto: Vector3.up ignora la rotacion local del dron
        transform.position = objetivoDron.position + (Vector3.up * altitudAbsoluta);

        // Billboarding: se resta la camara (no al reves) para evitar el efecto espejo
        Vector3 vectorDireccion = transform.position - camaraUsuario.position;

        if (bloquearEjeVertical)
        {
            vectorDireccion.y = 0;
        }

        if (vectorDireccion != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(vectorDireccion);
        }
    }
}