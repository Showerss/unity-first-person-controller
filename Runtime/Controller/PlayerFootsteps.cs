using UnityEngine;

/// <summary>
/// Footstep, jump and landing sounds for the FirstPersonController.
/// Steps are distance-based, so cadence follows actual speed; crouching is slower and quieter, sprinting louder.
/// Plays 2D on its own AudioSource. Ground tagged with <see cref="FootstepSurfaceTag"/> swaps in that surface's clips;
/// anything else uses the default clips below.
/// </summary>
[RequireComponent(typeof(FirstPersonController))]
public sealed class PlayerFootsteps : MonoBehaviour
{
    [Header("Clips (default surface)")]
    [Tooltip("Picked at random, never the same one twice in a row.")]
    [SerializeField] AudioClip[] _stepClips = new AudioClip[0];
    [SerializeField] AudioClip _jumpClip;
    [SerializeField] AudioClip _landClip;

    [Header("Cadence (metres per step)")]
    [SerializeField, Min(0.1f)] float _walkStride = 1.9f;
    [SerializeField, Min(0.1f)] float _sprintStride = 2.6f;
    [SerializeField, Min(0.1f)] float _crouchStride = 1.2f;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] float _walkVolume = 0.55f;
    [SerializeField, Range(0f, 1f)] float _sprintVolume = 0.75f;
    [SerializeField, Range(0f, 1f)] float _crouchVolume = 0.2f;
    [SerializeField, Range(0f, 1f)] float _jumpVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] float _landVolume = 0.8f;

    [Tooltip("Random pitch spread around 1 so repeated steps don't sound mechanical.")]
    [SerializeField, Range(0f, 0.3f)] float _pitchJitter = 0.08f;

    [Header("Surface detection")]
    [Tooltip("Layers the downward surface probe can hit.")]
    [SerializeField] LayerMask _groundMask = ~0;

    [Header("Landing")]
    [Tooltip("Ignore landings after shorter airtime (stairs, small bumps).")]
    [SerializeField, Min(0f)] float _minAirTime = 0.15f;
    [Tooltip("Fall speed (m/s) that plays the landing at full volume.")]
    [SerializeField, Min(0.1f)] float _hardLandingSpeed = 8f;

    FirstPersonController _controller;
    CharacterController _cc;
    AudioSource _source;
    float _distance;
    float _airTime;
    float _fallSpeed;
    bool _wasGrounded = true;
    int _lastStep = -1;
    Collider _lastGround;
    FootstepSurface _surface;

    void Awake()
    {
        _controller = GetComponent<FirstPersonController>();
        _cc = GetComponent<CharacterController>();
        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.spatialBlend = 0f;
    }

    void Update()
    {
        bool grounded = _controller.IsGrounded && !_controller.IsClimbing;
        Vector3 velocity = _cc.velocity;

        if (grounded && !_wasGrounded && _airTime >= _minAirTime)
        {
            FootstepSurface surface = CurrentSurface();
            AudioClip land = surface != null && surface.LandClip != null ? surface.LandClip : _landClip;
            float surfaceVolume = surface != null ? surface.Volume : 1f;
            Play(land, _landVolume * surfaceVolume * Mathf.Clamp01(_fallSpeed / _hardLandingSpeed + 0.3f));
            _distance = 0f; // next step a full stride after landing
        }
        else if (!grounded && _wasGrounded && velocity.y > 0.5f && !_controller.IsClimbing)
        {
            Play(_jumpClip, _jumpVolume);
        }

        if (grounded)
        {
            _airTime = 0f;
            _fallSpeed = 0f;
            TickSteps(velocity);
        }
        else
        {
            _airTime += Time.deltaTime;
            _fallSpeed = Mathf.Max(_fallSpeed, -velocity.y);
        }

        _wasGrounded = grounded;
    }

    void TickSteps(Vector3 velocity)
    {
        if (_controller.IsSliding) return;

        float speed = new Vector2(velocity.x, velocity.z).magnitude;
        if (speed < 0.3f)
        {
            _distance = 0f;
            return;
        }

        float stride = _controller.IsCrouching ? _crouchStride : _controller.IsSprinting ? _sprintStride : _walkStride;
        _distance += speed * Time.deltaTime;
        if (_distance < stride) return;
        _distance -= stride;

        float volume = _controller.IsCrouching ? _crouchVolume : _controller.IsSprinting ? _sprintVolume : _walkVolume;
        FootstepSurface surface = CurrentSurface();
        AudioClip[] clips = surface != null && surface.StepClips.Length > 0 ? surface.StepClips : _stepClips;
        Play(NextStepClip(clips), volume * (surface != null ? surface.Volume : 1f));
    }

    /// <summary>Surface under the player's feet, or null for the default set. Cached per ground collider.</summary>
    FootstepSurface CurrentSurface()
    {
        Vector3 origin = transform.TransformPoint(_cc.center);
        float reach = _cc.height * 0.5f + _cc.skinWidth + 0.3f;
        if (!Physics.SphereCast(origin, _cc.radius * 0.5f, Vector3.down, out RaycastHit hit, reach, _groundMask, QueryTriggerInteraction.Ignore))
            return null;

        if (hit.collider != _lastGround)
        {
            _lastGround = hit.collider;
            FootstepSurfaceTag tag = hit.collider.GetComponentInParent<FootstepSurfaceTag>();
            _surface = tag != null ? tag.Surface : null;
            _lastStep = -1; // clip index belongs to the previous set
        }
        return _surface;
    }

    AudioClip NextStepClip(AudioClip[] clips)
    {
        if (clips.Length == 0) return null;
        if (clips.Length == 1) return clips[0];
        if (_lastStep < 0 || _lastStep >= clips.Length) return clips[_lastStep = Random.Range(0, clips.Length)];
        int i = Random.Range(0, clips.Length - 1);
        if (i >= _lastStep) i++; // skip the previous clip
        _lastStep = i;
        return clips[i];
    }

    void Play(AudioClip clip, float volume)
    {
        if (clip == null || volume <= 0f) return;
        _source.pitch = 1f + Random.Range(-_pitchJitter, _pitchJitter);
        _source.PlayOneShot(clip, volume);
    }
}
