using UnityEngine;

/// <summary>
/// Controls engine audio based on vehicle speed.
/// Adjusts pitch and volume to simulate engine revving.
/// </summary>
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(VehicleController))]
public class EngineAudio : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private float minPitch = 0.8f;      // Pitch at zero speed
    [SerializeField] private float maxPitch = 2.0f;      // Pitch at max speed
    [SerializeField] private float minVolume = 0.3f;     // Volume at zero speed
    [SerializeField] private float maxVolume = 1.0f;     // Volume at max speed
    
    [Header("Smoothing")]
    [SerializeField] private float pitchLerpSpeed = 5f;  // How quickly pitch changes
    [SerializeField] private float volumeLerpSpeed = 5f; // How quickly volume changes
    
    private AudioSource _audioSource;
    private VehicleController _vehicle;
    private float _currentPitch;
    private float _currentVolume;
    
    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _vehicle = GetComponent<VehicleController>();
        
        // Set up audio source for looping
        _audioSource.loop = true;
        _audioSource.playOnAwake = true;
        
        // Initialize values
        _currentPitch = minPitch;
        _currentVolume = minVolume;
        _audioSource.pitch = _currentPitch;
        _audioSource.volume = _currentVolume;
        
        // Start playing if not already
        if (!_audioSource.isPlaying)
        {
            _audioSource.Play();
        }
    }
    
    void Update()
    {
        if (_vehicle == null || _audioSource == null) return;
        
        // Get normalized speed (0-1 range)
        float speedNormalized = _vehicle.GetNormalizedSpeed();
        
        // Calculate target pitch and volume based on speed
        float targetPitch = Mathf.Lerp(minPitch, maxPitch, speedNormalized);
        float targetVolume = Mathf.Lerp(minVolume, maxVolume, speedNormalized);
        
        // Smoothly lerp to target values
        _currentPitch = Mathf.Lerp(_currentPitch, targetPitch, pitchLerpSpeed * Time.deltaTime);
        _currentVolume = Mathf.Lerp(_currentVolume, targetVolume, volumeLerpSpeed * Time.deltaTime);
        
        // Apply to audio source
        _audioSource.pitch = _currentPitch;
        _audioSource.volume = _currentVolume;
    }
}
