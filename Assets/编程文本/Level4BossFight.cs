using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class Level4BossFight : MonoBehaviour
{
    [SerializeField] private Camera arenaCamera;
    [SerializeField] private SpriteRenderer player;
    [SerializeField] private SpriteRenderer boss;
    [SerializeField] private Sprite[] playerAttackFrames;
    [SerializeField] private Sprite[] bossIdleFrames;
    [SerializeField] private Sprite[] bossAttackFrames;
    [SerializeField] private Sprite playerShot;
    [SerializeField] private Sprite[] bossShots;
    [SerializeField] private TMP_Text playerHealthText;
    [SerializeField] private TMP_Text bossHealthText;

    private const int PlayerMaxHealth = 3;
    private const int BossMaxHealth = 10;
    private const float PlayerMoveSpeed = 5f;
    private const float BossSwimSpeed = 2.1f;
    private const float PlayerShotInterval = 2.15f;
    private const float BossShotInterval = 1.4f;
    private const float BarrageInterval = 1.05f;
    private const float PlayerShotSpeed = 11f;
    private const float BossShotSpeed = 7.5f;
    private const float BarrageSpeed = 12f;
    private const float AnimationFps = 8f;
    private const float ReleaseTime = 4f / AnimationFps; // The fifth attack frame.
    private const float BarrageRowSpacing = 0.55f;
    private const string EndingScene = "Level4Ending";

    private enum Phase { Duel, Barrage, Finished }

    private sealed class Shot
    {
        public SpriteRenderer Renderer;
        public bool FromPlayer;
    }

    private sealed class Wave
    {
        public Transform Transform;
        public bool HitPlayer;
    }

    private readonly List<Shot> _shots = new();
    private readonly List<Wave> _waves = new();
    private Phase _phase;
    private int _playerHealth = PlayerMaxHealth;
    private int _bossHealth = BossMaxHealth;
    private int _bossShotIndex;
    private float _time;
    private float _playerAttackStart = 0.65f;
    private float _bossAttackStart = 0.25f;
    private bool _playerReleased;
    private bool _bossReleased;

    private void Start()
    {
        Rect arena = ArenaBounds();
        player.transform.position = new Vector3(arena.xMin + 1.1f, arena.center.y, 0f);
        MoveBoss(arena);
        RefreshHealth();
    }

    private void Update()
    {
        float step = Time.deltaTime;
        if (step <= 0f || _phase == Phase.Finished) return;

        _time += step;
        Rect arena = ArenaBounds();
        MovePlayer(arena, step);
        MoveBoss(arena);

        if (_phase == Phase.Barrage)
        {
            if (AdvanceAttack(boss, bossAttackFrames, bossIdleFrames[0], BarrageInterval,
                    ref _bossAttackStart, ref _bossReleased))
                SpawnWave(arena);
            MoveWaves(arena, step);
            return;
        }

        if (AdvanceAttack(player, playerAttackFrames, playerAttackFrames[0], PlayerShotInterval,
                ref _playerAttackStart, ref _playerReleased))
            SpawnShot(playerShot, (Vector2)player.transform.position + new Vector2(0.96f, -0.20f), true);

        int idleFrame = Mathf.FloorToInt(_time * AnimationFps) % bossIdleFrames.Length;
        if (AdvanceAttack(boss, bossAttackFrames, bossIdleFrames[idleFrame], BossShotInterval,
                ref _bossAttackStart, ref _bossReleased))
            SpawnShot(NextBossShot(), (Vector2)boss.transform.position + new Vector2(-1.42f, -0.42f), false);

        MoveShots(arena, step);
    }

    private Rect ArenaBounds()
    {
        Vector2 center = arenaCamera.transform.position;
        float halfHeight = arenaCamera.orthographicSize;
        float halfWidth = halfHeight * arenaCamera.aspect;
        return Rect.MinMaxRect(center.x - halfWidth, center.y - halfHeight,
            center.x + halfWidth, center.y + halfHeight);
    }

    private void MovePlayer(Rect arena, float step)
    {
        Vector2 input = Vector2.zero;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed) input.x--;
            if (keyboard.dKey.isPressed) input.x++;
            if (keyboard.sKey.isPressed) input.y--;
            if (keyboard.wKey.isPressed) input.y++;
        }

        Vector2 position = (Vector2)player.transform.position + input.normalized * (PlayerMoveSpeed * step);
        float edge = Mathf.Min(1.1f, arena.width / 2f);
        position.x = Mathf.Clamp(position.x, arena.xMin + edge, arena.xMax - edge);
        position.y = Mathf.Clamp(position.y, arena.yMin + 0.85f, arena.yMax - 1.25f);
        player.transform.position = position;
    }

    private void MoveBoss(Rect arena)
    {
        float bottom = arena.yMin + 1.15f;
        float travel = arena.height - 2.6f;
        float y = bottom + Mathf.PingPong(travel / 2f + _time * BossSwimSpeed, travel);
        boss.transform.position = new Vector3(arena.xMax - 1.25f, y, 0f);
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
        }

        int frame = Mathf.FloorToInt(elapsed * AnimationFps);
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

    private void SpawnShot(Sprite sprite, Vector2 muzzle, bool fromPlayer)
    {
        SpriteRenderer renderer = CreateProjectile(sprite, muzzle, fromPlayer ? 0.46f : 0.58f, transform);
        _shots.Add(new Shot { Renderer = renderer, FromPlayer = fromPlayer });
    }

    private void MoveShots(Rect arena, float step)
    {
        for (int i = _shots.Count - 1; i >= 0; i--)
        {
            Shot shot = _shots[i];
            Vector2 position = shot.Renderer.transform.position;
            float nextX = position.x + (shot.FromPlayer ? PlayerShotSpeed : -BossShotSpeed) * step;
            if (HitsTarget(shot.FromPlayer, position, nextX))
            {
                Destroy(shot.Renderer.gameObject);
                _shots.RemoveAt(i);
                if (shot.FromPlayer) HitBoss();
                else HitPlayer();
                if (_phase != Phase.Duel) return;
            }
            else if (nextX < arena.xMin - 1f || nextX > arena.xMax + 1f)
            {
                Destroy(shot.Renderer.gameObject);
                _shots.RemoveAt(i);
            }
            else
            {
                shot.Renderer.transform.position = new Vector2(nextX, position.y);
            }
        }
    }

    private bool HitsTarget(bool fromPlayer, Vector2 position, float nextX)
    {
        // Sweep the horizontal segment so a fast projectile cannot skip past the body.
        Vector2 center = fromPlayer
            ? (Vector2)boss.transform.position + new Vector2(-0.53f, -0.03f)
            : (Vector2)player.transform.position + new Vector2(0.18f, -0.12f);
        Vector2 half = fromPlayer ? new Vector2(1.19f, 0.86f) : new Vector2(0.94f, 0.69f);
        return Mathf.Abs(position.y - center.y) <= half.y
            && Mathf.Max(position.x, nextX) >= center.x - half.x
            && Mathf.Min(position.x, nextX) <= center.x + half.x;
    }

    private void HitBoss()
    {
        _bossHealth--;
        AudioManager.Instance?.PlayImpact();
        RefreshHealth();
        if (_bossHealth > 0) return;

        _phase = Phase.Barrage;
        _bossAttackStart = _time;
        _bossReleased = false;
        player.sprite = playerAttackFrames[0];
        foreach (Shot shot in _shots) Destroy(shot.Renderer.gameObject);
        _shots.Clear();
    }

    private void SpawnWave(Rect arena)
    {
        var wall = new GameObject("Boss Barrage").transform;
        wall.SetParent(transform, false);
        float x = arena.xMax + 0.6f;
        wall.position = new Vector2(x, arena.center.y);
        for (float y = arena.yMin; y <= arena.yMax + BarrageRowSpacing / 2f; y += BarrageRowSpacing)
            CreateProjectile(NextBossShot(), new Vector2(x, y), 0.7f, wall);
        _waves.Add(new Wave { Transform = wall });
    }

    private void MoveWaves(Rect arena, float step)
    {
        for (int i = _waves.Count - 1; i >= 0; i--)
        {
            Wave wave = _waves[i];
            Vector3 position = wave.Transform.position;
            position.x -= BarrageSpeed * step;
            wave.Transform.position = position;

            // The wave spans the entire arena and overtakes the player at any height.
            if (!wave.HitPlayer && position.x <= player.transform.position.x + 0.85f)
            {
                wave.HitPlayer = true;
                HitPlayer();
                if (_phase == Phase.Finished) return;
            }
            if (position.x >= arena.xMin - 1f) continue;
            Destroy(wave.Transform.gameObject);
            _waves.RemoveAt(i);
        }
    }

    private void HitPlayer()
    {
        _playerHealth--;
        AudioManager.Instance?.PlayImpact();
        RefreshHealth();
        if (_playerHealth > 0) return;

        Phase previousPhase = _phase;
        _phase = Phase.Finished;
        if (previousPhase == Phase.Barrage)
            SceneManager.LoadScene(EndingScene);
        else if (GameManager.Instance != null)
            GameManager.Instance.RestartRun();
        else
            SceneManager.LoadScene(gameObject.scene.name);
    }

    private void RefreshHealth()
    {
        playerHealthText.text = $"HP {_playerHealth}/{PlayerMaxHealth}";
        bossHealthText.text = $"BOSS {_bossHealth}/{BossMaxHealth}";
    }
}
