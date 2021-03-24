namespace OpenCvSharp
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.UI;

	public class DrawController : MonoBehaviour
	{
		public RawImage drawImage;

		private bool enableDraw;
		private Mat drawMat;

		/*
		 * 初期化処理(WebCamControllerのWebカメラ映像取得開始時に呼ばれる)
		 */
		public void StartDrawController(int webCamTexWidth, int webCamTexHeight)
		{
			/* 値の初期化	*/
			enableDraw = true;

			/* 描画用画像の作成	*/
			Texture2D drawTex = new Texture2D(webCamTexWidth, webCamTexHeight, TextureFormat.RGBA32, false);
			drawMat = Unity.TextureToMat(drawTex);
			for (int y = 0; y < drawMat.Height; y++)
			{
				for (int x = 0; x < drawMat.Width; x++)
				{
					drawMat.Set(y, x, new Vec3b(0, 0, 0));
				}
			}
		}

		/*
		 * 初期化処理
		 */
		private void Update()
		{
			/* 描画許可があったら	*/
			if (enableDraw)
			{
				/* 入力があるとき	*/
				if(Input.GetMouseButton(0))
				{
					Vector3 mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);// 画面の押された場所
					Vector3 drawPosition = Camera.main.ScreenToWorldPoint(mousePosition);// UnityのWorld座標に変換
					DrawRectangle((int)drawPosition.x,(int)drawPosition.y);// 小さい矩形を描画
					Debug.Log((int)drawPosition.x+ ","+ (int)drawPosition.y);
				}
				drawImage.texture = Unity.MatToTexture(drawMat);
			}
		}

		/*
		 * 線描画処理
		 */
		private void DrawRectangle(int posX, int posY)
		{
			Cv2.Rectangle(drawMat,new Rect(posX,posY,10,10), new Scalar(0, 255, 0), 3);
		}

		/*
		 * 線描画画像のGetter
		 */
		public Mat GetDrawMat()
		{
			return drawMat;
		}
	}
}