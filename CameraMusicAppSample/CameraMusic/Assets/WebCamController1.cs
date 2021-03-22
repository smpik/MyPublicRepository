namespace OpenCvSharp
{

	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.UI;

	using OpenCvSharp.Tracking;

	public class WebCamController1 : MonoBehaviour
	{
		private int width = 1920;
		private int height = 1080;
		private int fps = 60;
		private Texture2D cap_tex;
		private Texture2D out_tex;
		private WebCamTexture webCamTexture;
		private Color32[] colors = null;

		private Tracker tracker;
		private bool isCaribrated;
		private bool isInitedTracking;

		/* HSVのパラメータは(色相、彩度、明度)	*/
		private readonly static Scalar SKIN_LOWER = new Scalar(130, 10, 60);//(0, 30, 60);
		private readonly static Scalar SKIN_UPPER = new Scalar(255, 50, 160);//(10, 160, 240);

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

			StartCoroutine(Init());

			tracker = Tracker.Create(TrackerTypes.MIL);
			isCaribrated = false;
			isInitedTracking = false;
		}

		/*
		 * 周期処理
		 */
		void Update()
		{
			if (colors != null)
			{
				webCamTexture.GetPixels32(colors);

				int width = webCamTexture.width;
				int height = webCamTexture.height;

				cap_tex.SetPixels32(this.colors);
				cap_tex.Apply();

				Mat outMat = Unity.TextureToMat(cap_tex);
				Rect2d obj = new Rect2d(new Point2d((outMat.Width / 2) - 400, 200), new Size2d(200, 200));
				/* キャリブレーション用の処理	*/
				if (!isInitedTracking)
				{
					Cv2.Rectangle(outMat, new Rect((outMat.Width / 2) - 400, 200, 200, 200), new Scalar(0, 0, 255), 5);// キャリブレーション用の矩形を表示
					Cv2.Rectangle(outMat, new Rect((outMat.Width / 2) + 400, 200, 200, 200), new Scalar(0, 0, 255), 5);// キャリブレーション用の矩形を表示
					// キャリブレーション判定
				}

				/* トラッキング開始(指定領域に肌色がたくさんある状態で一定時間経ったら)	*/
				if (isCaribrated)
				{
					tracker.Init(Unity.TextureToMat(cap_tex), obj);
					isInitedTracking = true;
				}
				/* トラッキング中	*/
				if(isCaribrated && isInitedTracking)
				{
					tracker.Update(Unity.TextureToMat(cap_tex), ref obj) ;
				}

				out_tex = Unity.MatToTexture(outMat);
			}
			
			GetComponent<RawImage>().texture = out_tex;
		}

		/*
		 * ボタン押下時の処理
		 */
		public void OnPushButton()
		{
			isCaribrated = true;
		}
	}
}