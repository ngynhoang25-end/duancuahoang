using UnityEngine;

public class AudioManager : MonoBehaviour
{
	private const string MuteKey = "BubbleShooterPro_Mute";

	public static AudioManager instance;
	public Sound[] sounds;
	public bool mute = false;
	
	public void ToggleMute()
	{
		if (mute)
		{
			StartAllSounds();
			mute = false;
		}
		else
		{
			StopAllSounds();
			mute = true;
		}

		PlayerPrefs.SetInt(MuteKey, mute ? 1 : 0);
		PlayerPrefs.Save();
	}

	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else
		{
			Destroy(gameObject);
			return;
		}

		foreach (Sound sound in sounds)
		{
			sound.audioSource = gameObject.AddComponent<AudioSource>();
			sound.audioSource.clip = sound.clip;
			sound.audioSource.volume = sound.volume;
			sound.audioSource.pitch = sound.pitch;
			sound.audioSource.loop = sound.loop;
		}

		mute = PlayerPrefs.GetInt(MuteKey, 0) == 1;
		if (mute)
			StopAllSounds();

		PlaySound("background");
		DontDestroyOnLoad(gameObject);
	}

	public void PlaySound(string name)
	{
		foreach (Sound sound in sounds)
		{
			if (sound.name == name)
			{
				sound.audioSource.Play();
			}
		}
	}

	public void StopAllSounds()
	{
		foreach (Sound sound in sounds)
		{
			sound.audioSource.volume = 0;
		}
	}

	public void StartAllSounds()
	{
		foreach (Sound sound in sounds)
		{
			sound.audioSource.volume = sound.volume;
		}
	}

}
