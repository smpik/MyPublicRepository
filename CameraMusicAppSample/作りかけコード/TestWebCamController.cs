namespace OpenCvSharp
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.UI;

	public class TestWebCamController : MonoBehaviour
	{
		/* カメラ映像を表示するために使う変数	*/
		private int width = 1920;
		private int height = 1080;
		private int fps = 60;
		private Texture2D cap_tex;  // カメラから取得したままのテクスチャ
		private Texture2D out_tex;  // Planeに出力させるテクスチャ
		private WebCamTexture webCamTexture;
		private Color32[] colors = null;

		/* 動体検知のために使う変数	*/
		private Mat prevMat;	// 前回の画像

		/* 画像処理するときのパラメータ	*/
		private const int DILATE_NUM = 10;  // 膨張処理をかける回数

		public DrawController drawController;
		public RawImage processImage0;

		IEnumerator Init()
		{
			while (true)
			{
				if (webCamTexture.width > 16 && webCamTexture.height > 16)
				{
					colors = new Color32[webCamTexture.width * webCamTexture.height];
					cap_tex = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
					break;
				}
				yield return null;
			}
		}

		/*
		 * 初期化処理
		 */
		void Start()
		{
			/* もろもろ初期化	*/
			WebCamDevice[] devices = WebCamTexture.devices;
			webCamTexture = new WebCamTexture(devices[0].name, this.width, this.height, this.fps);
			webCamTexture.Play();

			StartCoroutine(Init()); // カメラが起動するまで待たせる

			/* 描画開始	*/
			drawController.StartDrawController(webCamTexture.width, webCamTexture.height);
		}

		/*
		 * 周期処理
		 */
		void Update()
		{
			/* カメラが起動しているなら	*/
			if (colors != null)
			{
				/* カメラ映像を取得する	*/
				webCamTexture.GetPixels32(colors);

				int width = webCamTexture.width;
				int height = webCamTexture.height;

				cap_tex.SetPixels32(this.colors);
				cap_tex.Apply();

				/* 線描画画像を取得	*/
				Mat drawMat = drawController.GetDrawMat();

				/* 動体を検出	*/
				out_tex = detectMoveObject(cap_tex);

				/* 衝突判定	*/
				judgeHit(out_tex,drawMat);

				/* ユーザー用の映像を出力	*/
				Mat outMat = Unity.TextureToMat(cap_tex).Clone();
				Cv2.BitwiseOr(Unity.TextureToMat(cap_tex), drawMat, outMat);
				//GetComponent<Renderer>().material.mainTexture = out_tex;
				GetComponent<RawImage>().texture = Unity.MatToTexture(outMat);
			}
		}

		/*
		 * 動体検出処理
		 */
		private Texture2D detectMoveObject(Texture2D tex)
		{
			/* 出力用(最後にTexture2Dに戻す)	*/
			Mat retMat = Unity.TextureToMat(tex);

			/* グレースケールに変換	*/
			Mat grayMat = retMat.CvtColor(ColorConversionCodes.BGR2GRAY);

			/* 検出	*/
			if (prevMat != null)
			{
				/* 差分を検出	*/
				Cv2.Absdiff(grayMat, prevMat, retMat);

				/* ノイズ除去	*/
				Cv2.MedianBlur(retMat, retMat, 3);

				/* 二値化	*/
				Cv2.Threshold(retMat, retMat, 50, 255, ThresholdTypes.Binary);// Otsuだとノイズが入る

				/* 膨張処理	*/
				Cv2.Dilate(retMat, retMat, new Mat(), new Point2d(-1, -1), DILATE_NUM);// 第3、4引数はデフォルト値。第5引数は膨張を掛ける回数
			}

			/* 前回値更新	*/
			prevMat = grayMat.Clone();

			return Unity.MatToTexture(retMat);  // 手の検出を終えたテクスチャを返す
		}

		/*
		 * 衝突判定処理
		 */
		private void judgeHit(Texture2D tex, Mat drawMat)
		{
			Mat processMat = Unity.TextureToMat(tex);

			/* 引いた線と照らし合わせる	*/
			Cv2.BitwiseAnd(processMat, drawMat, processMat);// これで、衝突していた場合に動体と線が重なっている箇所だけ残る

			/* 衝突している個所を探す	*/
			Cv2.InRange(processMat, new Scalar(0, 254, 0), new Scalar(0, 255, 0), processMat);// 特定の色でマスク(白の数しか数えられないので)
			processImage0.texture = Unity.MatToTexture(drawMat);
			if (processMat.CountNonZero() > 0)
			{
				Debug.Log("緑と衝突");
			}
			else
			{
				//Debug.Log("衝突なし");
			}
		}
	}
}