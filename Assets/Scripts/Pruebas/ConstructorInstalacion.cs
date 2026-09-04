using System.Collections.Generic;
using UnityEngine;

// ConstructorInstalacion — genera por codigo los dos escenarios virtuales de
// la prueba con usuarios y permite alternar entre ellos (excluyentes):
//
//   Escenario 1 (tarea simple): rack de almacen con 5 esferas de inspeccion
//   coplanares en su cara frontal. Se trata como un bloque macizo (un unico
//   BoxCollider envolvente): los waypoints quedan en un plano.
//
//   Escenario 2 (tarea compleja, 3D): estructura de techo de vigas a dos
//   niveles con cruces de San Andres. Cada viga conserva su collider, asi que
//   el dron vuela entre los huecos y solo choca al tocar una viga. Cuatro nudos
//   llevan marcas de interes esfericas sin collider.
//
// Se ancla al OriginPoint en Start(). El escenario 1 usa offsetDesdeOrigen; el 2
// usa distanciaTechoAlQR (queda mas cerca del QR que el rack). SetModoTest()
// controla la visibilidad global y AlternarEscenario() cambia cual esta activo.
public class ConstructorInstalacion : MonoBehaviour
{
    [Header("Anclaje al Origen del Dron")]
    [Tooltip("OriginPoint calibrado por QR. La instalacion se cuelga de el.")]
    [SerializeField] private OriginPoint anclaOrigen;

    [Tooltip("Z+ va mas alla del QR. Lo usa el escenario 1 (rack). El transform se ancla aqui.")]
    [SerializeField] private Vector3 offsetDesdeOrigen = new Vector3(0f, 0f, 2.672f);

    [Tooltip("Rotacion del conjunto sobre el eje vertical central (grados). Solo escenario 1.")]
    [SerializeField] private float rotacionY = 325f;

    [Header("Materiales")]
    [SerializeField] private Material materialEstructura;
    [SerializeField] private Material materialCaja;
    [SerializeField] private Material materialEsfera;
    [Tooltip("Material de las marcas de interes del escenario 2. Si es null, usa materialEsfera.")]
    [SerializeField] private Material materialMarcaInteres;

    [Header("Escenario 1 — Rack (metros)")]
    [SerializeField] private float ancho = 3.0f;
    [SerializeField] private float altura = 3.2f;
    [SerializeField] private float profundidad = 0.6f;
    [SerializeField] private float grosorPerfil = 0.08f;
    [SerializeField] private int numNiveles = 3;

    [Header("Escenario 1 — Esferas")]
    [Tooltip("Diametro de cada esfera de inspeccion del rack (metros).")]
    [SerializeField] private float diametroEsfera = 0.35f;
    [Tooltip("Distancia de las esferas por delante de la cara frontal del rack (metros).")]
    [SerializeField] private float margenFrontal = 0.45f;
    [Tooltip("Altura del nivel inferior de esferas del rack (metros).")]
    [SerializeField] private float alturaNivelInferior = 1.3f;
    [Tooltip("Altura del nivel superior de esferas del rack (metros).")]
    [SerializeField] private float alturaNivelSuperior = 2.4f;

    [Header("Escenario 2 — Posicion respecto al QR")]
    [Tooltip("Distancia del frente de la estructura al QR (metros). DEBE ser menor que el offset del rack: el techo esta mas cerca que la estanteria.")]
    [SerializeField] private float distanciaTechoAlQR = 2.0f;

    [Header("Escenario 2 — Estructura de vigas (metros)")]
    [Tooltip("Altura del nivel INFERIOR de vigas (metros).")]
    [SerializeField] private float alturaNivelBajo = 2.7f;
    [Tooltip("Altura del nivel SUPERIOR de vigas (metros).")]
    [SerializeField] private float alturaNivelAlto = 3.4f;
    [Tooltip("Ancho de la estructura (eje X, metros).")]
    [SerializeField] private float anchoTecho = 2.4f;
    [Tooltip("Fondo de la estructura (eje Z, metros).")]
    [SerializeField] private float fondoTecho = 2.0f;
    [Tooltip("Lado de la seccion cuadrada de cada viga (metros).")]
    [SerializeField] private float grosorViga = 0.12f;
    [Tooltip("Numero de vigas del nivel inferior en cada direccion (pocas, despejado).")]
    [SerializeField] private int vigasNivelBajo = 2;
    [Tooltip("Numero de vigas del nivel superior en cada direccion (mas entramado).")]
    [SerializeField] private int vigasNivelAlto = 4;
    [Tooltip("Grosor de las diagonales (cruces de San Andres) respecto al de las vigas.")]
    [SerializeField] private float factorGrosorDiagonal = 0.6f;

    [Header("Escenario 2 — Marcas de interes")]
    [Tooltip("Diametro de las marcas de interes esfericas sobre los nudos (metros).")]
    [SerializeField] private float diametroMarca = 0.14f;

    [Header("Comun")]
    [Tooltip("Capa del suelo para el raycast del depth helper (solo escenario 1).")]
    [SerializeField] private LayerMask capaSueloFisico;
    [Tooltip("Capa fisica que detecta el validador de ruta.")]
    [SerializeField] private string nombreCapaFisica = "EntornoReal";

    // Estado interno
    private readonly List<Renderer> renderersEsc1 = new List<Renderer>();
    private readonly List<GameObject> objetosEsc2 = new List<GameObject>();
    private readonly List<LineRenderer> depthHelpersEsc1 = new List<LineRenderer>();
    private GameObject colliderEsc1;

    private bool modoTestActivo = false;
    private int escenarioActivo = 1;   // 1 = rack, 2 = techo
    private int capaFisica;

    public bool ModoTestActivo => modoTestActivo;
    public int EscenarioActivo => escenarioActivo;

    void Start()
    {
        capaFisica = LayerMask.NameToLayer(nombreCapaFisica);
        AnclarAlOrigen();

        ConstruirEscenario1();
        ConstruirEscenario2();

        AplicarVisibilidad();
    }

    void Update()
    {
        if (!modoTestActivo || escenarioActivo != 1) return;

        foreach (LineRenderer lr in depthHelpersEsc1)
        {
            if (lr == null) continue;
            Vector3 origen = lr.transform.position;
            lr.SetPosition(0, origen);
            if (Physics.Raycast(origen, Vector3.down, out RaycastHit hit, 10f, capaSueloFisico))
                lr.SetPosition(1, hit.point);
            else
                lr.SetPosition(1, origen + Vector3.down * 1.5f);
        }
    }

    // --- API publica ---

    public void SetModoTest(bool activo)
    {
        modoTestActivo = activo;
        AplicarVisibilidad();
    }

    public void AlternarEscenario()
    {
        escenarioActivo = (escenarioActivo == 1) ? 2 : 1;
        AplicarVisibilidad();
    }

    // --- Anclaje y helpers de rotacion (escenario 1) ---

    private void AnclarAlOrigen()
    {
        if (anclaOrigen == null)
        {
            Debug.LogError("[ConstructorInstalacion] OriginPoint no asignado.");
            return;
        }
        transform.SetParent(anclaOrigen.transform, false);
        transform.localPosition = offsetDesdeOrigen;
        transform.localRotation = Quaternion.identity;
    }

    private Vector3 EjeCentralRack => new Vector3(0f, 0f, profundidad * 0.5f);

    private Vector3 RotarSobreCentro(Vector3 posLocal)
    {
        Quaternion giro = Quaternion.Euler(0f, rotacionY, 0f);
        Vector3 centro = EjeCentralRack;
        Vector3 relativo = posLocal - centro;
        Vector3 rotado = giro * new Vector3(relativo.x, 0f, relativo.z);
        return new Vector3(centro.x + rotado.x, posLocal.y, centro.z + rotado.z);
    }

    private Quaternion GiroConjunto => Quaternion.Euler(0f, rotacionY, 0f);

    // --- Escenario 1: rack ---

    private void ConstruirEscenario1()
    {
        float xIzq = -ancho * 0.5f;
        float xDer = ancho * 0.5f;
        float zFrontal = 0f;
        float zFondo = profundidad;

        ConstruirBastidor(xIzq, zFrontal, zFondo);
        ConstruirBastidor(xDer, zFrontal, zFondo);

        for (int i = 0; i < numNiveles; i++)
        {
            float t = (i + 1f) / (numNiveles + 1f);
            float y = Mathf.Lerp(0f, altura, t);
            CrearPerfil(new Vector3(xIzq, y, zFrontal), new Vector3(xDer, y, zFrontal), grosorPerfil);
            CrearPerfil(new Vector3(xIzq, y, zFondo), new Vector3(xDer, y, zFondo), grosorPerfil);
            ColocarCajasEnNivel(xIzq, xDer, y, zFrontal, zFondo);
        }

        ConstruirEsferasRack();
        colliderEsc1 = ConstruirColliderSolido(
            new Vector3(0f, altura * 0.5f, profundidad * 0.5f),
            new Vector3(ancho, altura, profundidad),
            GiroConjunto, "ColliderSolidoRack");
    }

    private void ConstruirBastidor(float x, float zFrontal, float zFondo)
    {
        CrearPerfil(new Vector3(x, 0f, zFrontal), new Vector3(x, altura, zFrontal), grosorPerfil);
        CrearPerfil(new Vector3(x, 0f, zFondo), new Vector3(x, altura, zFondo), grosorPerfil);

        int tramos = 6;
        float paso = altura / tramos;
        for (int i = 0; i < tramos; i++)
        {
            float y0 = paso * i;
            float y1 = paso * (i + 1);
            float zA = (i % 2 == 0) ? zFrontal : zFondo;
            float zB = (i % 2 == 0) ? zFondo : zFrontal;
            CrearPerfil(new Vector3(x, y0, zA), new Vector3(x, y1, zB), grosorPerfil * 0.55f);
        }
    }

    private void ColocarCajasEnNivel(float xIzq, float xDer, float yNivel, float zFrontal, float zFondo)
    {
        float zCentro = (zFrontal + zFondo) * 0.5f;
        float anchoCaja = (xDer - xIzq) * 0.38f;
        float fondoCaja = (zFondo - zFrontal) * 0.75f;
        float altoCaja = 0.18f;
        float separacion = 0.02f;

        float[] centrosX = { xIzq + anchoCaja * 0.6f, xDer - anchoCaja * 0.6f };

        foreach (float cx in centrosX)
        {
            for (int k = 0; k < 3; k++)
            {
                float yBase = yNivel + grosorPerfil + k * (altoCaja + separacion);
                GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
                caja.name = $"Caja_{k}";
                caja.transform.SetParent(transform, false);
                caja.transform.localPosition = RotarSobreCentro(new Vector3(cx, yBase + altoCaja * 0.5f, zCentro));
                caja.transform.localRotation = GiroConjunto;
                caja.transform.localScale = new Vector3(anchoCaja, altoCaja, fondoCaja);
                Destroy(caja.GetComponent<Collider>());
                ConfigurarObjeto(caja, materialCaja != null ? materialCaja : materialEstructura, 1);
            }
        }
    }

    private void ConstruirEsferasRack()
    {
        float z = -margenFrontal;
        int cantidad = 5;

        float margenLateral = ancho * 0.10f;
        float xInicio = -ancho * 0.5f + margenLateral;
        float xFin = ancho * 0.5f - margenLateral;

        for (int i = 0; i < cantidad; i++)
        {
            float t = i / (float)(cantidad - 1);
            float x = Mathf.Lerp(xInicio, xFin, t);
            float y = (i % 2 == 0) ? alturaNivelInferior : alturaNivelSuperior;

            Vector3 pos = RotarSobreCentro(new Vector3(x, y, z));
            CrearEsferaRack(pos, $"Inspeccion_Rack_{i + 1}");
        }
    }

    private void CrearEsferaRack(Vector3 posLocal, string nombre)
    {
        GameObject esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        esfera.name = nombre;
        esfera.transform.SetParent(transform, false);
        esfera.transform.localPosition = posLocal;
        esfera.transform.localScale = Vector3.one * diametroEsfera;

        Collider col = esfera.GetComponent<Collider>();
        if (col != null) Destroy(col);

        ConfigurarObjeto(esfera, materialEsfera, 1);

        LineRenderer lr = esfera.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = 0.005f;
        lr.endWidth = 0.005f;
        lr.startColor = Color.white;
        lr.endColor = Color.white;
        lr.useWorldSpace = true;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        depthHelpersEsc1.Add(lr);
    }

    // --- Escenario 2: estructura de vigas a dos niveles con cruces de San Andres ---

    // El frente de la estructura se situa a 'distanciaTechoAlQR' del QR. Como el
    // transform esta anclado en offsetDesdeOrigen.z, restamos ese offset para que
    // la distancia al QR sea exactamente la pedida (mas cerca que el rack).
    private float ZFrenteLocal => distanciaTechoAlQR - offsetDesdeOrigen.z;

    private void ConstruirEscenario2()
    {
        float xIzq = -anchoTecho * 0.5f;
        float xDer = anchoTecho * 0.5f;
        float zFrente = ZFrenteLocal;
        float zFondo = zFrente + fondoTecho;

        // Reticulas horizontales: nivel bajo (despejado) y alto (entramado)
        ConstruirReticula(xIzq, xDer, zFrente, zFondo, alturaNivelBajo, vigasNivelBajo);
        ConstruirReticula(xIzq, xDer, zFrente, zFondo, alturaNivelAlto, vigasNivelAlto);

        // Montantes verticales en las cuatro esquinas
        CrearViga(new Vector3(xIzq, alturaNivelBajo, zFrente), new Vector3(xIzq, alturaNivelAlto, zFrente), grosorViga);
        CrearViga(new Vector3(xDer, alturaNivelBajo, zFrente), new Vector3(xDer, alturaNivelAlto, zFrente), grosorViga);
        CrearViga(new Vector3(xIzq, alturaNivelBajo, zFondo), new Vector3(xIzq, alturaNivelAlto, zFondo), grosorViga);
        CrearViga(new Vector3(xDer, alturaNivelBajo, zFondo), new Vector3(xDer, alturaNivelAlto, zFondo), grosorViga);

        // Cruces de San Andres en los dos laterales (planos X constante)
        ConstruirCruzSanAndres(xIzq, zFrente, zFondo);
        ConstruirCruzSanAndres(xDer, zFrente, zFondo);

        // Marcas de interes sobre nudos de la estructura (sin collider)
        ConstruirMarcasInteres(xIzq, xDer, zFrente, zFondo);

        // Sin collider solido envolvente: cada viga lleva su propio collider,
        // de modo que el dron puede volar entre los huecos de la estructura.
    }

    private void ConstruirReticula(float xIzq, float xDer, float zFrente, float zFondo, float y, int numVigas)
    {
        for (int i = 0; i < numVigas; i++)
        {
            float t = (numVigas == 1) ? 0.5f : i / (float)(numVigas - 1);
            float z = Mathf.Lerp(zFrente, zFondo, t);
            CrearViga(new Vector3(xIzq, y, z), new Vector3(xDer, y, z), grosorViga);
        }
        for (int i = 0; i < numVigas; i++)
        {
            float t = (numVigas == 1) ? 0.5f : i / (float)(numVigas - 1);
            float x = Mathf.Lerp(xIzq, xDer, t);
            CrearViga(new Vector3(x, y, zFrente), new Vector3(x, y, zFondo), grosorViga);
        }
    }

    // Cruz de San Andres en un lateral (plano X = const), entre los dos niveles.
    private void ConstruirCruzSanAndres(float x, float zFrente, float zFondo)
    {
        float g = grosorViga * factorGrosorDiagonal;
        CrearViga(new Vector3(x, alturaNivelBajo, zFrente), new Vector3(x, alturaNivelAlto, zFondo), g);
        CrearViga(new Vector3(x, alturaNivelBajo, zFondo), new Vector3(x, alturaNivelAlto, zFrente), g);
    }

    // Marcas esfericas sobre nudos reales de la estructura (4 puntos),
    // superpuestas a la viga. Sin collider.
    private void ConstruirMarcasInteres(float xIzq, float xDer, float zFrente, float zFondo)
    {
        Material mat = materialMarcaInteres != null ? materialMarcaInteres : materialEsfera;
        float zMedio = (zFrente + zFondo) * 0.5f;
        float yMedio = (alturaNivelBajo + alturaNivelAlto) * 0.5f;

        var nudos = new Vector3[]
        {
            new Vector3(xIzq, alturaNivelAlto, zFrente),   // esquina superior frontal izq
            new Vector3(xDer, alturaNivelAlto, zFondo),    // esquina superior trasera der
            new Vector3(xIzq, yMedio,          zMedio),    // centro de la cruz lateral izq
            new Vector3(0f,   alturaNivelBajo, zFondo),    // nudo central del nivel bajo (fondo)
        };

        int i = 1;
        foreach (Vector3 nudo in nudos)
        {
            GameObject marca = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marca.name = $"MarcaInteres_{i}";
            marca.transform.SetParent(transform, false);
            marca.transform.localPosition = nudo;
            marca.transform.localScale = Vector3.one * diametroMarca;

            Collider col = marca.GetComponent<Collider>();
            if (col != null) Destroy(col);

            ConfigurarObjeto(marca, mat, 2);
            i++;
        }
    }

    // --- Primitivas comunes ---

    // Viga como prisma (cubo escalado) entre dos puntos, con seccion cuadrada 'g'.
    // CONSERVA su BoxCollider para que el dron pueda volar entre los huecos de la
    // estructura y solo colisione al tocar una viga concreta.
    private void CrearViga(Vector3 inicio, Vector3 fin, float g)
    {
        GameObject viga = GameObject.CreatePrimitive(PrimitiveType.Cube);
        viga.name = "Viga";
        viga.transform.SetParent(transform, false);

        Vector3 centro = (inicio + fin) * 0.5f;
        Vector3 dir = fin - inicio;
        float longitud = dir.magnitude;

        viga.transform.localPosition = centro;
        if (dir.sqrMagnitude > 1e-6f)
            viga.transform.localRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        viga.transform.localScale = new Vector3(g, g, longitud);

        // No se destruye el collider: cada viga es un obstaculo independiente.
        ConfigurarObjeto(viga, materialEstructura, 2);
    }

    // Perfil cilindrico (rack). Aplica rotacion sobre el centro del rack.
    private void CrearPerfil(Vector3 inicio, Vector3 fin, float diametro)
    {
        GameObject perfil = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        perfil.name = "Perfil";
        perfil.transform.SetParent(transform, false);

        Vector3 inicioRot = RotarSobreCentro(inicio);
        Vector3 finRot = RotarSobreCentro(fin);

        Vector3 centro = (inicioRot + finRot) * 0.5f;
        Vector3 direccion = finRot - inicioRot;

        perfil.transform.localPosition = centro;
        perfil.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direccion.normalized);
        perfil.transform.localScale = new Vector3(diametro, direccion.magnitude * 0.5f, diametro);

        Destroy(perfil.GetComponent<CapsuleCollider>());
        ConfigurarObjeto(perfil, materialEstructura, 1);
    }

    private GameObject ConstruirColliderSolido(Vector3 centroLocal, Vector3 tamano, Quaternion giro, string nombre)
    {
        GameObject solido = new GameObject(nombre);
        solido.transform.SetParent(transform, false);
        solido.transform.localPosition = centroLocal;
        solido.transform.localRotation = giro;

        BoxCollider box = solido.AddComponent<BoxCollider>();
        box.size = tamano;

        if (capaFisica != -1) solido.layer = capaFisica;
        return solido;
    }

    // --- Visibilidad ---

    private void ConfigurarObjeto(GameObject obj, Material material, int escenario)
    {
        if (capaFisica != -1) obj.layer = capaFisica;

        Renderer rend = obj.GetComponent<Renderer>();
        if (rend == null) return;
        if (material != null) rend.material = material;

        if (escenario == 1) renderersEsc1.Add(rend);
        else objetosEsc2.Add(obj);   // escenario 2 se gestiona por GameObject (incluye colliders)
    }

    private void AplicarVisibilidad()
    {
        bool ver1 = modoTestActivo && escenarioActivo == 1;
        bool ver2 = modoTestActivo && escenarioActivo == 2;

        // Escenario 1: collider solido unico; basta alternar Renderers y el collider.
        foreach (Renderer r in renderersEsc1) if (r != null) r.enabled = ver1;
        foreach (LineRenderer lr in depthHelpersEsc1) if (lr != null) lr.enabled = ver1;
        if (colliderEsc1 != null) colliderEsc1.SetActive(ver1);

        // Escenario 2: cada viga tiene su collider, asi que se activa/desactiva
        // el GameObject completo para que los colliders no queden activos ocultos.
        foreach (GameObject obj in objetosEsc2) if (obj != null) obj.SetActive(ver2);
    }
}