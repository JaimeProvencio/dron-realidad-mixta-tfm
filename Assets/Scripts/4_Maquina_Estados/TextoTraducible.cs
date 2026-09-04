using UnityEngine;
using TMPro;

// TextoTraducible — se coloca en cada texto fijo de la interfaz e indica su
// clave de traduccion. Se actualiza al arrancar y cuando cambia el idioma
// (suscrito a GestorIdioma). No usar en textos dinamicos (bateria, altura):
// esos los gestiona su propio script.
[RequireComponent(typeof(TextMeshProUGUI))]
public class TextoTraducible : MonoBehaviour
{
    [Tooltip("Clave del diccionario de GestorIdioma (ej. 'definir_ruta').")]
    [SerializeField] private string clave;

    private TextMeshProUGUI texto;

    void Awake()
    {
        texto = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        if (GestorIdioma.Instancia != null)
        {
            GestorIdioma.Instancia.OnIdiomaCambiado += Refrescar;
            Refrescar();
        }
    }

    void OnDisable()
    {
        if (GestorIdioma.Instancia != null)
            GestorIdioma.Instancia.OnIdiomaCambiado -= Refrescar;
    }

    private void Refrescar()
    {
        if (texto != null && GestorIdioma.Instancia != null)
            texto.text = GestorIdioma.Instancia.Traducir(clave);
    }
}