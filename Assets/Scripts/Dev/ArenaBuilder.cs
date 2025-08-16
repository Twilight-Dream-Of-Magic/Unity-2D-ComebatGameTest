using UnityEngine;

namespace Dev {
	/// <summary>
	/// Builds a simple arena: flat ground and two vertical walls. 仅用于开发白盒场景。
	/// </summary>
	public static class ArenaBuilder {
		public static void CreateGround(Vector2 arenaHalfExtents) {
			// Ground
			var groundObject = new GameObject("Ground");
			var groundCollider = groundObject.AddComponent<BoxCollider2D>();
			groundCollider.size = new Vector2(arenaHalfExtents.x * 2f, 0.5f);
			groundObject.transform.position = new Vector3(0f, -1.8f, 0f);
			groundObject.layer = LayerMask.NameToLayer("Default");

			// Visual strip for ground
			var visualObject = new GameObject("Visual");
			visualObject.transform.SetParent(groundObject.transform, false);
			var spriteRenderer = visualObject.AddComponent<SpriteRenderer>();
			spriteRenderer.sprite = CreateSolidSprite(new Color(0.15f, 0.6f, 0.15f, 1f));
			visualObject.transform.localScale = new Vector3(groundCollider.size.x, groundCollider.size.y, 1f);

			// Side walls (keep players in bounds)
			float wallX = arenaHalfExtents.x + 2f;
			float wallHeight = 7f;
			float wallWidth = 0.5f;

			var leftWall = new GameObject("WallLeft");
			leftWall.transform.position = new Vector3(-wallX - wallWidth * 0.5f, -1.2f, 0f);
			var leftCollider = leftWall.AddComponent<BoxCollider2D>();
			leftCollider.size = new Vector2(wallWidth, wallHeight);

			var rightWall = new GameObject("WallRight");
			rightWall.transform.position = new Vector3(wallX + wallWidth * 0.5f, -1.2f, 0f);
			var rightCollider = rightWall.AddComponent<BoxCollider2D>();
			rightCollider.size = new Vector2(wallWidth, wallHeight);
		}

		static Sprite CreateSolidSprite(Color color) {
			var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
			{
				filterMode = FilterMode.Point
			};
			texture.SetPixel(0, 0, color);
			texture.Apply();
			return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
		}
	}
}