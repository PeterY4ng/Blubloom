using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class Level4BossFight : MonoBehaviour
{
    [SerializeField] private Camera arenaCamera;
    [SerializeField] private SpriteRenderer arenaBackground;
    [SerializeField] private SpriteRenderer player;
    [SerializeField] private SpriteRenderer boss;
    [SerializeField] private Sprite[] playerAttackFrames;
    [SerializeField] private Sprite[] bossIdleFrames;
    [SerializeField] private Sprite[] bossAttackFrames;
    [SerializeField] private Sprite playerShot;
    [SerializeField] private Sprite[] bossShots;
    [SerializeField] private TMP_Text bossHealthText;
    [SerializeField] private GameObject bossHealthPanel;

    private const int BossMaxHealth = 6;
    private const float BossSwimSpeed = 2.1f;
    private const float PlayerShotInterval = 1.5f;
    private const float BossShotInterval = 1.4f;
    private const float BarrageInterval = 0.6f;
    private const float PlayerShotSpeed = 11f;
    private const float BossShotSpeed = 7.5f;
    private const float BarrageSpeed = 12f;
    private const float AnimationFps = 8f;
    private const float ReleaseTime = 4f / AnimationFps; // The fifth attack frame.
    private const int BarrageShotCount = 121;
    private const float BarrageFanAngle = 200f;
    private const float BarrageShotSize = 0.6f;
    private const string EndingScene = "Level4Ending";
    private const string PlayerSwimState = "Player_Swim";
    private const string PlayerAttackState = "Player_Attack";

    private enum Phase { Approach, Duel, Barrage, Finished }

    private sealed class Shot
    {
        public SpriteRenderer Renderer;
        public bool FromPlayer;
        public Vector2 Velocity;
        public float Radius;
    }

    private readonly List<Shot> _shots = new();
    private Phase _phase;
    private PlayerStats _playerStats;
    private Animator _playerAnimator;
    private int _bossHealth = BossMaxHealth;
    private int _bossShotIndex;
    private int _barrageBurstIndex;
    private float _time;
    private float _playerAttackStart;
    private float _bossAttackStart;
    private float _nextBarrageAt;
    private bool _playerReleased;
    private bool _bossReleased;

    private void Start()
    {
        _playerStats = player.GetComponent<PlayerStats>();
        _playerAnimator = player.GetComponent<Animator>();
        bossHealthPanel.SetActive(false);
        MoveBoss(ArenaBounds());
        RefreshHealth();
    }

    private void Update()
    {
        float step = Time.deltaTime;
        if (step <= 0f || _phase == Phase.Finished || _playerStats.IsDead) return;

        _time += step;
        Rect arena = ArenaBounds();
        MoveBoss(arena);
        RefreshHealth();

        if (_phase == Phase.Approach)
        {
            boss.sprite = bossIdleFrames[Mathf.FloorToInt(_time * AnimationFps) % bossIdleFrames.Length];
            if (!ActorsShareView()) return;

            _phase = Phase.Duel;
            _playerAttackStart = _time;
            _bossAttackStart = _time;
            _playerAnimator.Play(PlayerAttackState, 0, 0f);
            bossHealthPanel.SetActive(true);
        }

        if (_phase == Phase.Barrage)
        {
            AdvanceAttack(boss, bossAttackFrames, bossIdleFrames[0], bossAttackFrames.Length / AnimationFps,
                ref _bossAttackStart, ref _bossReleased);
            if (_time >= _nextBarrageAt)
            {
                SpawnBarrage();
                _nextBarrageAt = _time + BarrageInterval;
            }
            MoveShots(arena, step);
            return;
        }

        if (AdvanceAttack(player, playerAttackFrames, playerAttackFrames[0], PlayerShotInterval,
                ref _playerAttackStart, ref _playerReleased))
            SpawnShot(playerShot, (Vector2)player.transform.position + new Vector2(0.96f, -0.20f),
                true, Vector2.right * PlayerShotSpeed, 0.46f);

        int idleFrame = Mathf.FloorToInt(_time * AnimationFps) % bossIdleFrames.Length;
        if (AdvanceAttack(boss, bossAttackFrames, bossIdleFrames[idleFrame], BossShotInterval,
                ref _bossAttackStart, ref _bossReleased))
            SpawnShot(NextBossShot(), (Vector2)boss.transform.position + new Vector2(-1.42f, -0.42f),
                false, Vector2.left * BossShotSpeed, 0.58f);

        MoveShots(arena, step);
    }

    private Rect ArenaBounds()
    {
        Bounds bounds = arenaBackground.bounds;
        return Rect.MinMaxRect(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y);
    }

    private bool ActorsShareView()
    {
        Vector3 playerView = arenaCamera.WorldToViewportPoint(player.transform.position);
        Vector3 bossView = arenaCamera.WorldToViewportPoint(boss.transform.position);
        return playerView.z > 0f && playerView.x >= 0f && playerView.x <= 1f
            && playerView.y >= 0f && playerView.y <= 1f
            && bossView.z > 0f && bossView.x >= 0f && bossView.x <= 1f
            && bossView.y >= 0f && bossView.y <= 1f;
    }

    private void MoveBoss(Rect arena)
    {
        float bottom = arena.yMin + 1.15f;
        float travel = arena.height - 2.6f;
        float y = bottom + Mathf.PingPong(travel / 2f + _time * BossSwimSpeed, travel);
        boss.transform.position = new Vector3(arena.xMax - 1.5f, y, 0f);
    }

    private bool AdvanceAttack(SpriteRenderer actor, Sprite[] frames, Sprite idle, float interval,
        ref float startedAt, ref bool released)
    {
        float elapsed = _time - startedAt;
        if (elapsed >= interval)
        {
            startedAt = _time;
            elapsed = 0f;
            released = false;
            if (actor == player) _playerAnimator.Play(PlayerAttackState, 0, 0f);
        }

        int frame = Mathf.FloorToInt(elapsed * AnimationFps);
        if (actor != player)
            actor.sprite = frame >= 0 && frame < frames.Length ? frames[frame] : idle;
        if (elapsed < ReleaseTime || released) return false;
        released = true;
        return true;
    }

    private Sprite NextBossShot()
    {
        Sprite sprite = bossShots[_bossShotIndex];
        _bossShotIndex = (_bossShotIndex + 1) % bossShots.Length;
        return sprite;
    }

    private SpriteRenderer CreateProjectile(Sprite sprite, Vector2 position, float size, Transform parent)
    {
        var projectile = new GameObject(sprite.name, typeof(SpriteRenderer));
        projectile.transform.SetParent(parent, false);
        projectile.transform.position = position;
        Vector3 spriteSize = sprite.bounds.size;
        projectile.transform.localScale = Vector3.one * (size / Mathf.Max(spriteSize.x, spriteSize.y));

        SpriteRenderer renderer = projectile.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = player.sharedMaterial;
        renderer.sortingOrder = 3;
        return renderer;
    }

    private void SpawnShot(Sprite sprite, Vector2 muzzle, bool fromPlayer, Vector2 velocity, float size)
    {
        SpriteRenderer renderer = CreateProjectile(sprite, muzzle, size, transform);
        _shots.Add(new Shot
        {
            Renderer = renderer,
            FromPlayer = fromPlayer,
            Velocity = velocity,
            Radius = size / 2f
        });
    }

    private void MoveShots(Rect arena, float step)
    {
        for (int i = _shots.Count - 1; i >= 0; i--)
        {
            Shot shot = _shots[i];
            Vector2 position = shot.Renderer.transform.position;
            Vector2 next = position + shot.Velocity * step;
            if (HitsTarget(shot, position, next))
            {
                Destroy(shot.Renderer.gameObject);
                _shots.RemoveAt(i);
                Phase phaseBeforeHit = _phase;
                if (shot.FromPlayer) HitBoss();
                else HitPlayer();
                if (_phase != phaseBeforeHit) return;
            }
            else if (next.x < arena.xMin - 1f || next.x > arena.xMax + 1f ||
                next.y < arena.yMin - 1f || next.y > arena.yMax + 1f)
            {
                Destroy(shot.Renderer.gameObject);
                _shots.RemoveAt(i);
            }
            else
            {
                shot.Renderer.transform.position = next;
            }
        }
    }

    private bool HitsTarget(Shot shot, Vector2 position, Vector2 next)
    {
        Vector2 center = shot.FromPlayer
            ? (Vector2)boss.transform.position + new Vector2(-0.53f, -0.03f)
            : (Vector2)player.transform.position + new Vector2(0.18f, -0.12f);
        Vector2 half = shot.FromPlayer ? new Vector2(1.19f, 0.86f) : new Vector2(0.94f, 0.69f);
        Bounds target = new Bounds(center, new Vector3(half.x * 2f, half.y * 2f, 1f));
        target.Expand(shot.Radius * 2f);
        if (target.Contains(position)) return true;

        // Sweep the entire movement segment, including diagonal barrage projectiles.
        Vector3 travel = next - position;
        float distance = travel.magnitude;
        if (distance <= Mathf.Epsilon) return false;
        return target.IntersectRay(new Ray(position, travel / distance), out float hitDistance)
            && hitDistance <= distance;
    }

    private void HitBoss()
    {
        _bossHealth--;
        AudioManager.Instance?.PlayImpact();
        RefreshHealth();
        if (_bossHealth > 0) return;

        _phase = Phase.Barrage;
        _playerStats.DeathSceneName = EndingScene;
        _bossAttackStart = _time;
        _nextBarrageAt = _time + ReleaseTime;
        _bossReleased = false;
        _playerAnimator.Play(PlayerSwimState, 0, 0f);
        foreach (Shot shot in _shots) Destroy(shot.Renderer.gameObject);
        _shots.Clear();
    }

    private void SpawnBarrage()
    {
        Vector2 origin = boss.transform.position;
        Vector2 aim = (Vector2)player.transform.position - origin;
        float centerAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        float angleStep = BarrageFanAngle / (BarrageShotCount - 1);
        float offset = (_barrageBurstIndex % 2) * angleStep / 2f;
        for (int i = 0; i < BarrageShotCount; i++)
        {
            float angle = (centerAngle - BarrageFanAngle / 2f + i * angleStep + offset) * Mathf.Deg2Rad;
            Vector2 velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * BarrageSpeed;
            SpawnShot(NextBossShot(), origin, false, velocity, BarrageShotSize);
        }
        _barrageBurstIndex++;
    }

    private void HitPlayer()
    {
        _playerStats.TakeDamage();
        if (_playerStats.IsDead)
        {
            _phase = Phase.Finished;
            return;
        }
        RefreshHealth();
    }

    private void RefreshHealth()
    {
        bossHealthText.text = $"BOSS HP {_bossHealth}/{BossMaxHealth}";
    }
}
