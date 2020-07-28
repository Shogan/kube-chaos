using UnityEngine;
using DG.Tweening;
using UnityThreading;
using System.Diagnostics;

[RequireComponent(typeof(Rigidbody2D))]
public class PodMovement : MonoBehaviour
{
    public Vector2 randomStartDirection;
    public float speed = 2f;
    private Rigidbody2D rb;
    private ActionThread killPodThread;
    public Vector2 newDirection;
    public float directionAngle;
    public GameObject hitEffectPrefab;
    public GameObject destroyEffectPrefab;
    public int hitEffectCount = 3;
    public int health = 30;
    public string podName;
    private bool canHit = true;
    public GameObject canvasGo;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Start is called before the first frame update
    void Start()
    {
        randomStartDirection = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f));
    }

    private void FixedUpdate()
    {
        rb.velocity = new Vector2(randomStartDirection.x * speed, randomStartDirection.y * speed);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Wall" || collision.gameObject.tag == "Pod" || collision.gameObject.tag == "Player")
        {
            var heading = collision.contacts[0].point - (Vector2)transform.position;
            newDirection = Vector3.Reflect(heading, collision.contacts[0].normal).normalized;
            randomStartDirection = newDirection;

        }
        else if (collision.gameObject.tag == "Bullet")
        {
            if (!canHit)
                return;

            health--;
            RegisterHitAt(collision.GetContact(0).point);
            if (health <= 0)
            {
                canHit = false;
                var explosion = (GameObject)Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);
                Destroy(explosion, 4f);
                killPodThread = UnityThreadHelper.CreateThread(KillPod);
                Destroy(canvasGo);
                Destroy(gameObject, 0.2f);
            }
        }
    }

    private void KillPod()
    {
        KubeDestroy(podName);
        if (NodeManager.Instance.podNamesInPlay.Contains(podName))
        {
            NodeManager.Instance.podNamesInPlay.Remove(podName);
        }
    }

    private void RegisterHitAt(Vector2 location)
    {
        for (var i = 1; i <= hitEffectCount; i++)
        {
            var hitEffect = (GameObject)Instantiate(hitEffectPrefab, location, Quaternion.identity);
            var rndScale = transform.localScale.x * Random.Range(0.3f, 0.8f);
            hitEffect.transform.localScale = new Vector3(rndScale, rndScale, rndScale);
            hitEffect.transform.DOScale(0f, 3f);

            var hitEffectRb = hitEffect.GetComponent<Rigidbody2D>();
            hitEffectRb.angularVelocity = Random.Range(5f, 50f);
            hitEffectRb.AddForce(new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f)), ForceMode2D.Impulse);

            var hitEffectSprite = hitEffect.GetComponent<SpriteRenderer>();
            hitEffectSprite
                .DOFade(0f, 3f).SetEase(Ease.OutSine)
                .OnComplete(() =>
                {
                    Destroy(hitEffect, 1f);
                });
        }
    }

    public void KubeDestroy(string args)
    {
        UnityEngine.Debug.Log($"executing: {KubeManager.Instance.kubectlExecutableName} --namespace {KubeManager.Instance.kubeNamespace} delete pod {args}");
        Process process = new Process();
        
        process.StartInfo.FileName = KubeManager.Instance.kubectlExecutableName;
        process.StartInfo.Arguments = $"--context {KubeManager.Instance.kubeContextName} --namespace {KubeManager.Instance.kubeNamespace} delete pod {args}";
        process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.UseShellExecute = false;
        process.Start();
    }
}
