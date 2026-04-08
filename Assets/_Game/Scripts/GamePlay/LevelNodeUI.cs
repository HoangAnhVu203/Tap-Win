using UnityEngine;
using Spine.Unity;

public class LevelNodeUI : MonoBehaviour
{
    [Header("Spine")]
    [SerializeField] private SkeletonGraphic skeletonGraphic;
    [SerializeField] private SkeletonGraphic skeletonGraphic2;

    [Header("Animation")]
    [SerializeField] private string normalAnim = "idle2";
    [SerializeField] private bool normalLoop = true;

    [SerializeField] private string disabledAnim = "idle1";
    [SerializeField] private bool disabledLoop = false;

    [Header("Particle")]
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private Transform particleSpawnPoint;
    [SerializeField] private Transform particleSpawnPoint2;
    [SerializeField] private bool playParticleWhenChangeState = true;
    [SerializeField] private float particleDestroyDelay = 2f;

    public bool IsDisabled { get; private set; }
    public RectTransform Rect => transform as RectTransform;

    private bool hasInitialized;

    private void Awake()
    {
        AutoAssignReferences();
    }

    private void Reset()
    {
        AutoAssignReferences();
    }

    private void Start()
    {
        RefreshVisual(false);
        hasInitialized = true;
    }

    public void SetDisabled(bool value)
    {
        if (IsDisabled == value && hasInitialized)
            return;

        bool willPlayParticle = playParticleWhenChangeState && !IsDisabled && value;

        IsDisabled = value;
        RefreshVisual(willPlayParticle);
    }

    private void RefreshVisual(bool spawnParticle)
    {
        PlayAnimation(skeletonGraphic);
        PlayAnimation(skeletonGraphic2);

        if (spawnParticle)
        {
            PlayParticleAt(particleSpawnPoint);
            PlayParticleAt(particleSpawnPoint2);
        }
    }

    private void PlayAnimation(SkeletonGraphic graphic)
    {
        if (graphic == null || graphic.AnimationState == null)
            return;

        if (IsDisabled)
            graphic.AnimationState.SetAnimation(0, disabledAnim, disabledLoop);
        else
            graphic.AnimationState.SetAnimation(0, normalAnim, normalLoop);
    }

    private void PlayParticleAt(Transform spawnRoot)
    {
        if (particlePrefab == null || spawnRoot == null)
            return;

        GameObject fx = Instantiate(particlePrefab, spawnRoot.position, Quaternion.identity, spawnRoot);

        RectTransform fxRect = fx.GetComponent<RectTransform>();
        if (fxRect != null)
        {
            fxRect.localPosition = Vector3.zero;
            fxRect.localRotation = Quaternion.identity;
            fxRect.localScale = Vector3.one;
        }

        ParticleSystem ps = fx.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();

            float lifeTime = ps.main.duration;

            if (ps.main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
                lifeTime += ps.main.startLifetime.constantMax;
            else
                lifeTime += ps.main.startLifetime.constant;

            Destroy(fx, lifeTime + 0.2f);
        }
        else
        {
            Destroy(fx, particleDestroyDelay);
        }
    }

    private void AutoAssignReferences()
    {
        SkeletonGraphic[] graphics = GetComponentsInChildren<SkeletonGraphic>(true);

        if (skeletonGraphic == null && graphics.Length > 0)
            skeletonGraphic = graphics[0];

        if (skeletonGraphic2 == null && graphics.Length > 1)
            skeletonGraphic2 = graphics[1];

        if (particleSpawnPoint == null)
            particleSpawnPoint = transform;

        if (particleSpawnPoint2 == null)
            particleSpawnPoint2 = transform;
    }
}