using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerStats : MonoBehaviour
{
    [Header("吃一个食物加的分")]
    public int scorePerFood = 10;

    [Header("无敌帧时长")]
    public float invincibilityTime = 1.5f;

    [Header("危险障碍击退力度")]
    public float knockbackForce = 8f;

    [Header("进化后先去的过场场景，留空则直接进下一关")]
    public string cutsceneName = "进化动画";

    [Header("最终下一关场景名")]
    public string nextSceneName = "Story1";

    [Header("进化动画名 / 时长")]
    public string evolveAnimName = "Cell_Evolve";
    public float evolveAnimLength = 1.5f;

    private bool _isInvincible;
    private bool _hasEvolved;
    private Rigidbody2D _rb;
    private Animator _anim;
    private PlayerMovement _movement;

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponentInChildren<Animator>();
        _movement = GetComponent<PlayerMovement>();
        RefreshUI();
    }

    void RefreshUI()
    {
        if (GameManager.Instance != null && UIManager.Instance != null)
        {
            UIManager.Instance.RefreshScore(GameManager.Instance.CurrentScore);
            UIManager.Instance.RefreshHealth(GameManager.Instance.CurrentHealth);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Food"))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.AddScore(scorePerFood);
            AudioManager.Instance?.PlayEat();
            RefreshUI();
            Destroy(other.gameObject);
            CheckEvolve();
        }

        if (other.CompareTag("Hazard") && !_isInvincible)
        {
            TakeDamage();
            Destroy(other.gameObject);
        }
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.collider.CompareTag("Obstacle") && !_isInvincible)
        {
            TakeDamage();
            _rb.linearVelocity = Vector2.zero;
            Vector2 dir = other.contacts[0].normal;
            _rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
        }
    }

    void CheckEvolve()
    {
        if (_hasEvolved || GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentScore >= GameManager.Instance.GetEvolveScore())
            StartCoroutine(EvolveRoutine());
    }

    IEnumerator EvolveRoutine()
    {
        _hasEvolved = true;
        if (_movement != null) _movement.enabled = false;
        _rb.linearVelocity = Vector2.zero;
        _isInvincible = true;

        if (_anim != null) _anim.Play(evolveAnimName);
        yield return new WaitForSeconds(evolveAnimLength);

        if (GameManager.Instance != null)
            GameManager.Instance.AdvanceStage();

        // 关键分支：有过场先去 Story1，没有再直接 Level2
        if (!string.IsNullOrEmpty(cutsceneName))
        {
            SceneManager.LoadScene(cutsceneName);
        }
        else if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            GameManager.Instance.RestartRun();
        }
    }

    void TakeDamage()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.Damage();
        AudioManager.Instance?.PlayImpact();
        _isInvincible = true;
        RefreshUI();
        Invoke(nameof(EndInvincible), invincibilityTime);
        if (GameManager.Instance.CurrentHealth <= 0)
            GameManager.Instance.RestartRun();
    }

    void EndInvincible() => _isInvincible = false;
}
