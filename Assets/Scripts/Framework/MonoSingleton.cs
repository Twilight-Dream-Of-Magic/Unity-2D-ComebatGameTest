using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Framework {
	public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
	{
		public bool global = true;
		public bool activeFunction = true;

		private static T _instance;

		/// <summary>
		/// 懒惰的静态只读对象实例
		/// Instances of lazy static read-only objects
		/// </summary>
		private readonly static Lazy<T> LazyInstance = new Lazy<T>(InitializeStaticSingletonInstance);

		private static T InitializeStaticSingletonInstance()
		{
			if (_instance == null)
			{
				_instance = (T)FindObjectOfType<T>();
			}
			return _instance;
		}

		public static T Instance => LazyInstance.Value;

		/// <summary>
		/// Mono单例模式的Awake方法，用于初始化Mono单例对象
		/// Awake method of the Mono singleton pattern, used to initialize the Mono singleton object.
		/// </summary>
		private void Awake()
		{
			if (global)
			{
				if (_instance != null && _instance != this.gameObject.GetComponent<T>())
				{
					RemoveDuplicateObject();
					return;
				}

				DontDestroyOnLoad(this.gameObject);

				if (_instance == null)
					_instance = this.gameObject.GetComponent<T>();
			}
			this.DoAwake();
		}

		private void OnEnable()
		{
			if (activeFunction == true)
				this.DoEnable();
		}

		private void Start()
		{
			if (activeFunction == true && isActiveAndEnabled == true)
				this.DoStart();
		}

		private void Update()
		{
			if (activeFunction == true && isActiveAndEnabled == true)
				this.DoUpdate();
		}

		private void LateUpdate()
		{
			if (activeFunction == true && isActiveAndEnabled == true)
				this.DoLateUpdate();
		}

		private void FixedUpdate()
		{
			if (activeFunction == true && isActiveAndEnabled == true)
				this.DoFixedUpdate();
		}

		private void OnDisable()
		{
			if (activeFunction == false)
				this.DoDisable();
		}

		private void OnDestroy()
		{
			this.DoDestroy();
		}

		/*********************/

		protected virtual void DoAwake() {}

		protected virtual void DoEnable() {}

		protected virtual void DoStart() {}

		protected virtual void DoUpdate() {}

		protected virtual void DoLateUpdate() {}

		protected virtual void DoFixedUpdate() {}

		protected virtual void DoDisable() {}
		protected virtual void DoDestroy() {}

		public static void HideOrShow(bool flag)
		{
			if (_instance != null)
			{
				if(_instance.gameObject != null)
				{
					_instance.gameObject.SetActive(flag);
				}
			}
		}

		/// <summary>
		/// 当游戏播放停止时，Unity Editor会以随机顺序销毁对象
		/// ...（保留中文注释原样，见用户提供实现）
		/// </summary>
		private void RemoveDuplicateObject()
		{
			Debug.LogWarningFormat("You created new MonoBehaviour sington object!");
			if (Application.isPlaying)
			{
				GameObject.Destroy(this.gameObject);
			}
			else
			{
				GameObject.DestroyImmediate(this.gameObject);
			}

			#if UNITY_EDITOR
			if (EditorApplication.isPlaying)
			{
				GameObject.Destroy(this.gameObject);
			}
			else
			{
				GameObject.DestroyImmediate(this.gameObject);
			}
			#endif
		}
	}
}