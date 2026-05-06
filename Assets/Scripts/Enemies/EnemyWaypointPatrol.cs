using System;
using System.Collections;
using Nivelo.Animations;
using Nivelo.Player;
using Nivelo.PlayerWorld;
using Nivelo.Singletons.GameManagement;
using UnityEngine;
using UnityEngine.Events;

namespace Nivelo.Enemies
{
  [Serializable]
  public class WaypointIndexEvent : UnityEvent<int>
  {
  }

  [RequireComponent(typeof(Rigidbody2D))]
  public class EnemyWaypointPatrol : MonoBehaviour
  {
    [Header("Route")]
    [SerializeField, Tooltip("Route asset with ordered waypoints.")]
    EnemyWaypointRoute route;

    [SerializeField, Tooltip("If true, the route restarts after the last waypoint.")]
    bool loopRoute = true;

    [SerializeField, Tooltip("If true, patrol starts automatically in Start.")]
    bool startPatrolOnStart = false;

    [Header("Movement")]
    [SerializeField, Tooltip("Base movement speed before waypoint multiplier is applied.")]
    float baseSpeed = 3f;

    [SerializeField, Tooltip("Distance threshold used to consider a waypoint reached.")]
    float waypointReachDistance = 0.08f;

    [SerializeField, Tooltip("If true, the enemy teleports to the first waypoint when patrol starts.")]
    bool snapToFirstWaypointOnStart;

    [Header("Look At Player")]
    [SerializeField, Tooltip("If true, while in LookAtPlayer state the enemy ignores the route and follows the player.")]
    bool followPlayerWhileLooking = true;

    [SerializeField, Tooltip("Optional extra speed multiplier used while following the player in LookAtPlayer state.")]
    float lookAtPlayerSpeedMultiplier = 1f;

    [SerializeField, Tooltip("Distance to the player where movement stops while following in LookAtPlayer state.")]
    float lookAtPlayerStopDistance = 0.08f;

    [Header("Start Movement Animation")]
    [SerializeField, Tooltip("If enabled, plays a startup animation before the patrol starts moving.")]
    bool playStartAnimationBeforeMoving;

    [SerializeField, Tooltip("Idle animation played from Awake while the enemy is not moving.")]
    string preStartIdleAnimationName;

    [SerializeField, Tooltip("Animation name played before movement starts.")]
    string startMovementAnimationName;

    [SerializeField, Tooltip("Optional sound played when the enemy leaves idle and starts moving.")]
    AudioClip startMovementSound;

    [SerializeField, Tooltip("Optional override duration for startup animation in seconds. Set 0 to use SpriteAnimator animation duration.")]
    float startMovementAnimationDurationOverride;

    [SerializeField, Tooltip("Optional SpriteAnimator used to play the startup animation. If null, one will be searched in children.")]
    SpriteAnimator startMovementSpriteAnimator;

    [SerializeField, Tooltip("Optional movement animation controller to block while startup animation plays.")]
    CharacterMovementAnimatorController movementAnimatorController;

    [SerializeField, Tooltip("Fallback wait in seconds when an animation duration cannot be resolved.")]
    float unresolvedAnimationFallbackWait = 0.1f;

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, patrol flow logs are printed to the console.")]
    bool enablePatrolLogs = true;

    [Header("Callbacks")]
    [SerializeField]
    UnityEvent onJourneyStarted;

    [SerializeField]
    UnityEvent onJourneyFinished;

    [SerializeField]
    UnityEvent onLoopCompleted;

    [SerializeField]
    WaypointIndexEvent onWaypointReached;

    [SerializeField]
    WaypointIndexEvent onWaypointStarted;

    Rigidbody2D rb;
    AudioSource startMovementAudioSource;
    Coroutine _startPatrolRoutine;
    Coroutine _lookAtPlayerFollowRoutine;
    Coroutine _waypointWaitRoutine;

    bool isMoving;
    bool isWaitingAtWaypoint;
    int currentWaypointIndex;
    bool isFollowingPlayer;
    bool isPausedLookingAtPlayer;
    bool resumePatrolAfterLookAtPlayer;
    bool hasSavedScaleBeforeLookAtPlayer;
    Vector3 savedScaleBeforeLookAtPlayer;

    PlayerManager cachedPlayerManager;
    Transform cachedPlayerTransform;

    public event Action JourneyStarted;
    public event Action JourneyFinished;
    public event Action LoopCompleted;
    public event Action<int> WaypointReached;
    public event Action<int> WaypointStarted;

    public bool IsMoving => isMoving;
    public int CurrentWaypointIndex => currentWaypointIndex;

    void Awake()
    {
      rb = GetComponent<Rigidbody2D>();
      rb.gravityScale = 0f;
      rb.freezeRotation = true;
      EnsureStartMovementAudioSource();
      ResolveAnimationDependencies();
      SetMovementAnimationBlocked(true);
      PlayIdleFromAwake();
    }

    void Start()
    {
      if (startPatrolOnStart)
      {
        StartPatrol();
      }
    }

    void FixedUpdate()
    {
      if (isPausedLookingAtPlayer)
      {
        TickLookAtPlayerState();
        return;
      }

      TickMovement();
    }

    public void EnterLookAtPlayerState()
    {
      if (isPausedLookingAtPlayer)
      {
        return;
      }

      if (_lookAtPlayerFollowRoutine != null)
      {
        StopCoroutine(_lookAtPlayerFollowRoutine);
        _lookAtPlayerFollowRoutine = null;
      }

      isFollowingPlayer = false;

      isPausedLookingAtPlayer = true;
      resumePatrolAfterLookAtPlayer = isMoving || isWaitingAtWaypoint || _startPatrolRoutine != null;
      savedScaleBeforeLookAtPlayer = transform.localScale;
      hasSavedScaleBeforeLookAtPlayer = true;

      if (_startPatrolRoutine != null)
      {
        StopCoroutine(_startPatrolRoutine);
        _startPatrolRoutine = null;
        SetMovementAnimationBlocked(false);
        if (startMovementSpriteAnimator != null)
        {
          startMovementSpriteAnimator.SetPlayLocked(false);
        }
      }

      StopWaypointWaitRoutine();
      isWaitingAtWaypoint = false;
      isMoving = false;
      rb.linearVelocity = Vector2.zero;

      SetMovementAnimationBlocked(true);

      TryResolvePlayerTransform();
      LogPatrol($"Estado mirar jugador activado. Reanudar patrulla={resumePatrolAfterLookAtPlayer}.");
    }

    public void ExitLookAtPlayerState()
    {
      if (!isPausedLookingAtPlayer)
      {
        return;
      }

      isPausedLookingAtPlayer = false;

      if (_lookAtPlayerFollowRoutine != null)
      {
        StopCoroutine(_lookAtPlayerFollowRoutine);
        _lookAtPlayerFollowRoutine = null;
      }

      if (hasSavedScaleBeforeLookAtPlayer)
      {
        transform.localScale = savedScaleBeforeLookAtPlayer;
        hasSavedScaleBeforeLookAtPlayer = false;
      }

      SetMovementAnimationBlocked(false);
      if (startMovementSpriteAnimator != null)
      {
        startMovementSpriteAnimator.SetPlayLocked(false);
      }

      if (resumePatrolAfterLookAtPlayer && route != null && route.WaypointCount > 0)
      {
        isWaitingAtWaypoint = false;
        isMoving = true;
        LogPatrol($"Estado mirar jugador desactivado. Patrulla reanudada en waypoint {currentWaypointIndex}.");
      }
      else
      {
        rb.linearVelocity = Vector2.zero;
        LogPatrol("Estado mirar jugador desactivado. Patrulla permanece detenida.");
      }

      resumePatrolAfterLookAtPlayer = false;
    }

    public void EnterLookAtPlayerFollowMode()
    {
      StartLookAtPlayerFollowMode();
    }

    public void ExitLookAtPlayerFollowMode()
    {
      isFollowingPlayer = false;
      rb.linearVelocity = Vector2.zero;
      SetMovementAnimationBlocked(false);
      LogPatrol("Seguimiento del jugador desactivado.");
    }

    public void StartLookAtPlayerFollowMode()
    {
      if (isFollowingPlayer || _lookAtPlayerFollowRoutine != null)
      {
        LogPatrol("StartLookAtPlayerFollowMode cancelado: el seguimiento del jugador ya está activo o en preparación.");
        return;
      }

      if (isPausedLookingAtPlayer)
      {
        ExitLookAtPlayerState();
      }

      followPlayerWhileLooking = true;
      isFollowingPlayer = true;
      PlayStartMovementSound();

      if (ShouldPlayStartMovementAnimation())
      {
        _lookAtPlayerFollowRoutine = StartCoroutine(StartLookAtPlayerFollowAfterStartupAnimation());
        return;
      }

      BeginFollowMovement();
    }

    public void SetFollowPlayerWhileLooking(bool enabled)
    {
      followPlayerWhileLooking = enabled;

      if (isPausedLookingAtPlayer && !followPlayerWhileLooking)
      {
        rb.linearVelocity = Vector2.zero;
      }
    }

    public void StartPatrol()
    {
      if (isMoving || _startPatrolRoutine != null)
      {
        LogPatrol("StartPatrol cancelado: patrulla ya iniciada.");
        return;
      }

      if (route == null ||
          route.WaypointCount == 0)
      {
        LogPatrol("StartPatrol cancelado: no hay ruta o la ruta no tiene waypoints.");
        return;
      }

      currentWaypointIndex = Mathf.Clamp(currentWaypointIndex, 0, route.WaypointCount - 1);

      if (snapToFirstWaypointOnStart)
      {
        Vector2 firstPoint = route.GetWaypointPosition(0);
        rb.position = firstPoint;
        transform.position = new Vector3(firstPoint.x, firstPoint.y, transform.position.z);
        LogPatrol($"Teleport al primer waypoint en {firstPoint}.");
      }

      if (_startPatrolRoutine != null)
      {
        StopCoroutine(_startPatrolRoutine);
        _startPatrolRoutine = null;
      }

      StopWaypointWaitRoutine();
      isWaitingAtWaypoint = false;
      PlayStartMovementSound();

      if (ShouldPlayStartMovementAnimation())
      {
        _startPatrolRoutine = StartCoroutine(StartPatrolAfterStartupAnimation());
        return;
      }

      BeginMovement();
    }

    public void StopPatrol()
    {
      if (_startPatrolRoutine != null)
      {
        StopCoroutine(_startPatrolRoutine);
        _startPatrolRoutine = null;
      }

      StopWaypointWaitRoutine();
      isWaitingAtWaypoint = false;
      isMoving = false;
      if (!rb)
      {
        rb = GetComponent<Rigidbody2D>();
      }
      rb.linearVelocity = Vector2.zero;
      LogPatrol("Patrulla detenida manualmente.");
    }

    public void ResetPatrolProgress(bool stopMovement = true)
    {
      currentWaypointIndex = 0;
      LogPatrol($"Progreso de patrulla reiniciado. stopMovement={stopMovement}.");

      if (stopMovement)
      {
        StopPatrol();
      }
    }

    void TickMovement()
    {
      if (!isMoving || isWaitingAtWaypoint)
      {
        rb.linearVelocity = Vector2.zero;
        return;
      }

      if (isFollowingPlayer)
      {
        TickFollowPlayerMovement();
      }
      else
      {
        TickPatrolMovement();
      }
    }

    void TickPatrolMovement()
    {
      if (route == null || route.WaypointCount == 0)
      {
        rb.linearVelocity = Vector2.zero;
        return;
      }

      Vector2 currentPosition = rb.position;
      Vector2 target = route.GetWaypointPosition(currentWaypointIndex);
      Vector2 delta = target - currentPosition;
      float sqrDistance = delta.sqrMagnitude;

      if (sqrDistance <= waypointReachDistance * waypointReachDistance)
      {
        ReachCurrentWaypoint(currentPosition);
        return;
      }

      ApplyVelocityTowardsWaypoint(currentPosition);
    }

    void TickFollowPlayerMovement()
    {
      if (!TryResolvePlayerTransform())
      {
        rb.linearVelocity = Vector2.zero;
        return;
      }

      Vector2 toPlayer = GetVectorToPlayer();
      ApplyFollowVelocity(toPlayer);
      UpdateFacingTowardsPlayer(toPlayer.x, false);
    }

    void TickLookAtPlayerState()
    {
      if (!TryResolvePlayerTransform())
      {
        rb.linearVelocity = Vector2.zero;
        return;
      }

      Vector2 toPlayer = GetVectorToPlayer();
      rb.linearVelocity = Vector2.zero;
      UpdateFacingTowardsPlayer(toPlayer.x, true);
    }



    Vector2 GetVectorToPlayer()
    {
      return cachedPlayerTransform.position - transform.position;
    }

    void ApplyFollowVelocity(Vector2 toPlayer)
    {
      float stopDistance = Mathf.Max(0f, lookAtPlayerStopDistance);
      float stopDistanceSqr = stopDistance * stopDistance;

      if (toPlayer.sqrMagnitude <= stopDistanceSqr)
      {
        rb.linearVelocity = Vector2.zero;
        return;
      }

      float followSpeed = ResolveLookAtPlayerFollowSpeed();
      rb.linearVelocity = followSpeed > 0f
        ? toPlayer.normalized * followSpeed
        : Vector2.zero;
    }

    void UpdateFacingTowardsPlayer(float deltaX, bool forceIdleAnimation)
    {
      if (Mathf.Abs(deltaX) <= Mathf.Epsilon)
      {
        return;
      }

      bool shouldFaceRight = deltaX > 0f;
      if (forceIdleAnimation && movementAnimatorController != null)
      {
        movementAnimatorController.ForceIdleHorizontalFacing(shouldFaceRight);
        return;
      }

      Vector3 currentScale = transform.localScale;
      float absScaleX = Mathf.Max(Mathf.Abs(currentScale.x), 0.0001f);

      if (shouldFaceRight)
      {
        currentScale.x = absScaleX;
      }
      else
      {
        currentScale.x = -absScaleX;
      }

      transform.localScale = currentScale;
    }

    float ResolveLookAtPlayerFollowSpeed()
    {
      float patrolSpeed = baseSpeed;
      if (route != null && route.WaypointCount > 0)
      {
        int safeIndex = Mathf.Clamp(currentWaypointIndex, 0, route.WaypointCount - 1);
        float waypointMultiplier = route.GetWaypointSpeedMultiplier(safeIndex);
        patrolSpeed *= waypointMultiplier;
      }

      return Mathf.Max(0f, patrolSpeed * lookAtPlayerSpeedMultiplier);
    }

    bool TryResolvePlayerTransform()
    {
      if (cachedPlayerTransform != null)
      {
        return true;
      }

      if (cachedPlayerManager == null)
      {
        cachedPlayerManager = GameManager.Instance != null
          ? GameManager.Instance.GetPlayerManager()
          : null;
      }

      if (cachedPlayerManager == null)
      {
        return false;
      }

      cachedPlayerTransform = cachedPlayerManager.transform;
      return cachedPlayerTransform != null;
    }

    IEnumerator StartPatrolAfterStartupAnimation()
    {
      isMoving = false;
      rb.linearVelocity = Vector2.zero;

      SetMovementAnimationBlocked(true);
      if (startMovementSpriteAnimator != null)
      {
        startMovementSpriteAnimator.SetPlayLocked(true);
      }

      yield return PlayAnimationAndWait(startMovementAnimationName, startMovementAnimationDurationOverride, "inicio de movimiento");

      if (startMovementSpriteAnimator != null)
      {
        startMovementSpriteAnimator.SetPlayLocked(false);
      }

      SetMovementAnimationBlocked(false);
      _startPatrolRoutine = null;
      BeginMovement();
    }

    IEnumerator StartLookAtPlayerFollowAfterStartupAnimation()
    {
      isMoving = false;
      rb.linearVelocity = Vector2.zero;

      SetMovementAnimationBlocked(true);
      if (startMovementSpriteAnimator != null)
      {
        startMovementSpriteAnimator.SetPlayLocked(true);
      }

      yield return PlayAnimationAndWait(startMovementAnimationName, startMovementAnimationDurationOverride, "inicio de seguimiento del jugador");

      if (startMovementSpriteAnimator != null)
      {
        startMovementSpriteAnimator.SetPlayLocked(false);
      }

      SetMovementAnimationBlocked(false);
      _lookAtPlayerFollowRoutine = null;
      BeginFollowMovement();
    }

    void BeginFollowMovement()
    {
      isWaitingAtWaypoint = false;
      isMoving = true;
      isFollowingPlayer = true;
      LogPatrol("Seguimiento del jugador iniciado.");
    }



    void BeginMovement()
    {
      isWaitingAtWaypoint = false;
      SetMovementAnimationBlocked(false);
      isMoving = true;
      LogPatrol($"Patrulla iniciada. Waypoints: {route.WaypointCount}. Loop: {loopRoute}. Inicio en índice: {currentWaypointIndex}.");
      EmitJourneyStarted();
      EmitWaypointStarted(currentWaypointIndex);
    }

    void EnsureStartMovementAudioSource()
    {
      if (startMovementAudioSource != null)
      {
        return;
      }

      startMovementAudioSource = GetComponent<AudioSource>();
      if (startMovementAudioSource == null)
      {
        startMovementAudioSource = gameObject.AddComponent<AudioSource>();
      }

      startMovementAudioSource.playOnAwake = false;
      startMovementAudioSource.loop = false;
      startMovementAudioSource.spatialBlend = 1f;
      startMovementAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
      startMovementAudioSource.minDistance = 1f;
      startMovementAudioSource.maxDistance = 20f;
    }

    void PlayStartMovementSound()
    {
      if (startMovementSound == null)
      {
        return;
      }

      EnsureStartMovementAudioSource();
      startMovementAudioSource.PlayOneShot(startMovementSound);
    }

    void PlayIdleFromAwake()
    {
      if (startMovementSpriteAnimator == null || string.IsNullOrWhiteSpace(preStartIdleAnimationName))
      {
        return;
      }

      startMovementSpriteAnimator.PlayForced(preStartIdleAnimationName);
      LogPatrol($"Idle inicial desde Awake: '{preStartIdleAnimationName}'.");
    }

    bool ShouldPlayStartMovementAnimation()
    {
      if (!playStartAnimationBeforeMoving)
      {
        return false;
      }

      if (string.IsNullOrWhiteSpace(startMovementAnimationName))
      {
        LogPatrol("Animación inicial habilitada pero no se definió nombre. Se inicia movimiento sin espera.");
        return false;
      }

      ResolveAnimationDependencies();

      if (startMovementSpriteAnimator == null)
      {
        LogPatrol("Animación inicial habilitada pero no se encontró SpriteAnimator. Se inicia movimiento sin espera.");
        return false;
      }

      return true;
    }

    void ResolveAnimationDependencies()
    {
      if (startMovementSpriteAnimator == null)
      {
        startMovementSpriteAnimator = GetComponentInChildren<SpriteAnimator>();
      }

      if (movementAnimatorController == null)
      {
        movementAnimatorController = GetComponent<CharacterMovementAnimatorController>();
      }
    }

    float ResolveStartupAnimationDuration()
    {
      if (startMovementAnimationDurationOverride > 0f)
      {
        return startMovementAnimationDurationOverride;
      }

      if (startMovementSpriteAnimator != null &&
          startMovementSpriteAnimator.TryGetAnimationDuration(startMovementAnimationName, out float duration))
      {
        return duration;
      }

      return 0f;
    }

    float ResolveAnimationDuration(string animationName, float durationOverride)
    {
      if (durationOverride > 0f)
      {
        return durationOverride;
      }

      if (startMovementSpriteAnimator != null &&
          startMovementSpriteAnimator.TryGetAnimationDuration(animationName, out float duration))
      {
        return duration;
      }

      return Mathf.Max(0f, unresolvedAnimationFallbackWait);
    }

    IEnumerator PlayAnimationAndWait(string animationName, float durationOverride, string stageLabel)
    {
      if (startMovementSpriteAnimator == null || string.IsNullOrWhiteSpace(animationName))
      {
        yield break;
      }

      startMovementSpriteAnimator.PlayForced(animationName);
      float waitSeconds = ResolveAnimationDuration(animationName, durationOverride);

      LogPatrol($"Reproduciendo {stageLabel} '{animationName}' por {waitSeconds:0.###}s.");

      if (waitSeconds > 0f)
      {
        yield return new WaitForSeconds(waitSeconds);
      }
      else
      {
        yield return null;
      }
    }

    void SetMovementAnimationBlocked(bool blocked)
    {
      if (movementAnimatorController == null)
      {
        return;
      }

      movementAnimatorController.SetAnimationControlBlocked(blocked);

      if (!blocked)
      {
        movementAnimatorController.ClearCurrentAnimationCache();
      }
    }

    void ReachCurrentWaypoint(Vector2 currentPosition)
    {
      LogPatrol($"Waypoint alcanzado: {currentWaypointIndex}.");
      EmitWaypointReached(currentWaypointIndex);

      int nextWaypointIndex = ResolveNextWaypointIndex();
      bool hasNextWaypoint = nextWaypointIndex >= 0;

      if (!hasNextWaypoint)
      {
        isMoving = false;
        rb.linearVelocity = Vector2.zero;
        LogPatrol("Ruta completada. Fin de patrulla.");
        EmitJourneyFinished();
        return;
      }

      float waitSeconds = route.GetWaypointWaitSeconds(currentWaypointIndex);
      if (waitSeconds > 0f)
      {
        StartWaypointWait(nextWaypointIndex, waitSeconds);
        return;
      }

      AdvanceToWaypoint(nextWaypointIndex);
      ApplyVelocityTowardsWaypoint(currentPosition);
    }

    int ResolveNextWaypointIndex()
    {
      bool hasMoreWaypoints = currentWaypointIndex + 1 < route.WaypointCount;
      if (hasMoreWaypoints)
      {
        return currentWaypointIndex + 1;
      }

      if (loopRoute && route.WaypointCount > 0)
      {
        LogPatrol("Ruta completada. Reiniciando loop en waypoint 0.");
        EmitLoopCompleted();
        return 0;
      }

      return -1;
    }

    void AdvanceToWaypoint(int nextWaypointIndex)
    {
      currentWaypointIndex = nextWaypointIndex;
      LogPatrol($"Avanzando al siguiente waypoint: {currentWaypointIndex}.");
      EmitWaypointStarted(currentWaypointIndex);
    }

    void StartWaypointWait(int nextWaypointIndex, float waitSeconds)
    {
      StopWaypointWaitRoutine();
      _waypointWaitRoutine = StartCoroutine(WaitAtWaypoint(nextWaypointIndex, waitSeconds));
    }

    IEnumerator WaitAtWaypoint(int nextWaypointIndex, float waitSeconds)
    {
      isWaitingAtWaypoint = true;
      rb.linearVelocity = Vector2.zero;
      LogPatrol($"Esperando {waitSeconds:0.###}s en waypoint {currentWaypointIndex}.");

      yield return new WaitForSeconds(waitSeconds);

      isWaitingAtWaypoint = false;
      _waypointWaitRoutine = null;

      if (!isMoving || route == null || route.WaypointCount == 0)
      {
        yield break;
      }

      AdvanceToWaypoint(nextWaypointIndex);
    }

    void StopWaypointWaitRoutine()
    {
      if (_waypointWaitRoutine == null)
      {
        return;
      }

      StopCoroutine(_waypointWaitRoutine);
      _waypointWaitRoutine = null;
    }

    void ApplyVelocityTowardsWaypoint(Vector2 currentPosition)
    {
      Vector2 target = route.GetWaypointPosition(currentWaypointIndex);
      Vector2 delta = target - currentPosition;

      if (delta.sqrMagnitude <= Mathf.Epsilon)
      {
        rb.linearVelocity = Vector2.zero;
        return;
      }

      float speedMultiplier = route.GetWaypointSpeedMultiplier(currentWaypointIndex);
      float speed = Mathf.Max(0f, baseSpeed * speedMultiplier);
      if (speed <= 0f)
      {
        rb.linearVelocity = Vector2.zero;
        return;
      }

      rb.linearVelocity = delta.normalized * speed;
    }

    void EmitJourneyStarted()
    {
      LogPatrol("Callback: JourneyStarted.");
      onJourneyStarted?.Invoke();
      JourneyStarted?.Invoke();
    }

    void EmitJourneyFinished()
    {
      LogPatrol("Callback: JourneyFinished.");
      onJourneyFinished?.Invoke();
      JourneyFinished?.Invoke();
    }

    void EmitLoopCompleted()
    {
      LogPatrol("Callback: LoopCompleted.");
      onLoopCompleted?.Invoke();
      LoopCompleted?.Invoke();
    }

    void EmitWaypointReached(int waypointIndex)
    {
      LogPatrol($"Callback: WaypointReached({waypointIndex}).");
      onWaypointReached?.Invoke(waypointIndex);
      WaypointReached?.Invoke(waypointIndex);
    }

    void EmitWaypointStarted(int waypointIndex)
    {
      LogPatrol($"Callback: WaypointStarted({waypointIndex}).");
      onWaypointStarted?.Invoke(waypointIndex);
      WaypointStarted?.Invoke(waypointIndex);
    }

    void LogPatrol(string message)
    {
      if (!enablePatrolLogs)
      {
        return;
      }

      Debug.Log($"[EnemyWaypointPatrol][{name}] {message}", this);
    }

    void OnValidate()
    {
      baseSpeed = Mathf.Max(0f, baseSpeed);
      waypointReachDistance = Mathf.Max(0.01f, waypointReachDistance);
      lookAtPlayerSpeedMultiplier = Mathf.Max(0f, lookAtPlayerSpeedMultiplier);
      lookAtPlayerStopDistance = Mathf.Max(0f, lookAtPlayerStopDistance);
      startMovementAnimationDurationOverride = Mathf.Max(0f, startMovementAnimationDurationOverride);
      unresolvedAnimationFallbackWait = Mathf.Max(0f, unresolvedAnimationFallbackWait);
    }

    void OnDisable()
    {
      if (_startPatrolRoutine != null)
      {
        StopCoroutine(_startPatrolRoutine);
        _startPatrolRoutine = null;
      }

      if (_lookAtPlayerFollowRoutine != null)
      {
        StopCoroutine(_lookAtPlayerFollowRoutine);
        _lookAtPlayerFollowRoutine = null;
      }

      if (startMovementSpriteAnimator != null)
      {
        startMovementSpriteAnimator.SetPlayLocked(false);
      }

      SetMovementAnimationBlocked(false);
    }
  }
}
