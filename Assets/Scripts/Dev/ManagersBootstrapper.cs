using UnityEngine;
using Systems;

namespace Dev {
	/// <summary>
	/// Ensures global managers and camera framing exist for a dev scene.
	/// 只做“存在性保障”，不承担遊戲邏輯。
	/// </summary>
	public static class ManagersBootstrapper {
		public static void EnsureManagers(Vector2 arenaHalfExtents) {
			if (!Object.FindObjectOfType<GameManager>())
			{
				new GameObject("GameManager").AddComponent<GameManager>();
			}
			if (!Object.FindObjectOfType<FrameClock>())
			{
				new GameObject("FrameClock").AddComponent<FrameClock>();
			}
			if (!Object.FindObjectOfType<AudioManager>())
			{
				var audioManagerObject = new GameObject("AudioManager");
				var audioManager = audioManagerObject.AddComponent<AudioManager>();
				audioManager.bgmSource = audioManagerObject.AddComponent<AudioSource>();
				audioManager.sfxSource = audioManagerObject.AddComponent<AudioSource>();
			}
			if (!Object.FindObjectOfType<CameraShaker>())
			{
				var cameraObject = Camera.main ? Camera.main.gameObject : new GameObject("Main Camera", typeof(Camera));
				cameraObject.tag = "MainCamera";
				if (!cameraObject.GetComponent<CameraShaker>())
				{
					cameraObject.AddComponent<CameraShaker>();
				}
				var cameraComponent = cameraObject.GetComponent<Camera>();
				cameraComponent.orthographic = true;
				cameraComponent.orthographicSize = 3.5f;
				cameraObject.transform.position = new Vector3(0, 0, -10);
				var cameraFramer = cameraObject.GetComponent<Systems.CameraFramer>();
				if (!cameraFramer)
				{
					cameraFramer = cameraObject.AddComponent<Systems.CameraFramer>();
				}
				cameraFramer.arenaHalfExtents = arenaHalfExtents;
			}
			if (!Object.FindObjectOfType<HitEffectManager>())
			{
				new GameObject("HitEffectManager").AddComponent<HitEffectManager>();
			}
			if (!Object.FindObjectOfType<ComboCounter>())
			{
				new GameObject("ComboCounter").AddComponent<ComboCounter>();
			}
			if (!Object.FindObjectOfType<RuntimeConfig>())
			{
				new GameObject("RuntimeConfig").AddComponent<RuntimeConfig>();
			}
			// Ensure RoundManager exists early so UI presenters can bind in OnEnable
			if (!Object.FindObjectOfType<Systems.RoundManager>())
			{
				new GameObject("RoundManager").AddComponent<Systems.RoundManager>().roundTime = 300f;
			}
		}
	}
}