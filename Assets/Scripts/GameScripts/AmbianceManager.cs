using UnityEngine;

public class AmbianceManager : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource _audioSource;

    [Header("Ambiance Clips")]
    [SerializeField] private AudioClip _forestAmbiance;
    [SerializeField] private AudioClip _shoreAmbiance;

    [Header("Settings")]
    [SerializeField] private bool _useRandomAmbiance = false;
    [SerializeField] private AmbianceType _selectedAmbiance = AmbianceType.Forest;

    public enum AmbianceType
    {
        Forest,
        Shore
    }

    private void Start()
    {
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Configure audio source defaults for 2D looping background ambiance
        _audioSource.loop = true;
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f; // 2D sound

        AudioClip clipToPlay = null;

        if (_useRandomAmbiance)
        {
            clipToPlay = Random.value > 0.5f ? _forestAmbiance : _shoreAmbiance;
        }
        else
        {
            clipToPlay = _selectedAmbiance == AmbianceType.Forest ? _forestAmbiance : _shoreAmbiance;
        }

        if (clipToPlay != null)
        {
            _audioSource.clip = clipToPlay;
            _audioSource.Play();
            Debug.Log($"[AmbianceManager] Playing ambiance: {clipToPlay.name}");
        }
        else
        {
            Debug.LogWarning("[AmbianceManager] No ambiance clip assigned!");
        }
    }
}
