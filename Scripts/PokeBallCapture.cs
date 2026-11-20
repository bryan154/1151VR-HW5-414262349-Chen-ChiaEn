using System.Collections;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PokeBallCapture : MonoBehaviour
{
    public GameObject pokemon;

    private bool busy;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 originalScale;

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;

        if (pokemon != null)
        {
            originalScale = pokemon.transform.localScale;
        }
    }

    void Update()
    {
        if (busy || pokemon == null) return;

        
#if ENABLE_INPUT_SYSTEM
        bool pressed =
            (Touchscreen.current != null &&
             Touchscreen.current.primaryTouch.press.wasPressedThisFrame) ||
            (Mouse.current != null &&
             Mouse.current.leftButton.wasPressedThisFrame) ||
            (Keyboard.current != null &&
             Keyboard.current.spaceKey.wasPressedThisFrame);
#else
        bool pressed =
            (Input.touchCount > 0 &&
             Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began) ||
            Input.GetMouseButtonDown(0) ||
            Input.GetKeyDown(KeyCode.Space);
#endif

        if (!pressed) return;

        
        if (!pokemon.activeSelf)
        {
            pokemon.transform.localScale = originalScale;
            pokemon.SetActive(true);
            transform.SetPositionAndRotation(startPosition, startRotation);
            return;
        }

        StartCoroutine(Throw());
    }

    IEnumerator Throw()
    {
        busy = true;

        
        Bounds bounds = new Bounds(pokemon.transform.position, Vector3.zero);
        Renderer[] meshes = pokemon.GetComponentsInChildren<Renderer>();

        if (meshes.Length > 0)
        {
            bounds = meshes[0].bounds;
            foreach (Renderer mesh in meshes)
            {
                bounds.Encapsulate(mesh.bounds);
            }
        }

        Vector3 end = bounds.center;
        float distance = Vector3.Distance(startPosition, end);
        float height = Mathf.Max(0.5f, distance * 0.23f);

        
        const float flightTime = 1.2f;
        for (float time = 0; time < flightTime; time += Time.deltaTime)
        {
            float t = time / flightTime;
            float arc = 4f * height * t * (1f - t);

            transform.position = Vector3.Lerp(startPosition, end, t)
                               + Vector3.up * arc;
            transform.rotation = startRotation * Quaternion.Euler(360f * t, 0f, 0f);

            yield return null;
        }

        transform.SetPositionAndRotation(end, startRotation);

        
        Camera cam = Camera.main;
        Vector3 right = cam != null ? cam.transform.right : Vector3.right;
        Vector3 up = cam != null ? cam.transform.up : Vector3.up;

        GameObject[] sparks = new GameObject[14];
        for (int i = 0; i < sparks.Length; i++)
        {
            sparks[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sparks[i].name = "Capture spark";
            sparks[i].GetComponent<Collider>().enabled = false;
        }

        const float captureTime = 0.65f;
        for (float time = 0; time < captureTime; time += Time.deltaTime)
        {
            float t = time / captureTime;
            pokemon.transform.localScale =
                originalScale * (1f - Mathf.SmoothStep(0f, 1f, t));

            for (int i = 0; i < sparks.Length; i++)
            {
                float angle = 2f * Mathf.PI * i / sparks.Length;
                Vector3 direction = right * Mathf.Cos(angle) + up * Mathf.Sin(angle);
                float radius = height * (0.14f + t * 0.65f);
                float size = height * 0.085f * (1f - t);

                sparks[i].transform.position = end + direction * radius;
                sparks[i].transform.localScale = Vector3.one * size;
            }

            yield return null;
        }

        foreach (GameObject spark in sparks)
        {
            Destroy(spark);
        }

        pokemon.SetActive(false);

        
        Vector3 landing = new Vector3(end.x, bounds.min.y + GetBallRadius(), end.z);

        const float dropTime = 0.35f;
        for (float time = 0; time < dropTime; time += Time.deltaTime)
        {
            float t = time / dropTime;
            transform.position = Vector3.Lerp(end, landing, t * t);
            yield return null;
        }

        transform.position = landing;
        busy = false;
    }

    float GetBallRadius()
    {
        Renderer mesh = GetComponentInChildren<Renderer>();
        return mesh != null ? mesh.bounds.extents.y : 0.1f;
    }
}