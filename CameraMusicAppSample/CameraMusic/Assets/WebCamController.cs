namespace OpenCvSharp
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.UI;

	public class WebCamController : MonoBehaviour
	{
		#region 変える必要のない変数、定数
		/* 手のカスケードファイルのパス	*/
		private const string CASCADE_FILE_PATH0 = "/palm_cascade.xml";	// パーを認識する用っぽい
		private const string CASCADE_FILE_PATH1 = "/palm_cascade1.xml";	// パーを認識する用っぽい
		private const string CASCADE_FILE_PATH2 = "/palm_cascade2.xml";	// グーを認識する用っぽい

		/* アクセスするクラス	*/
		public HandObjectManager handObjectManager;	// 手を検出した位置に当たり判定用のオブジェクトを表示するクラス

		/* カメラ映像を表示するために使う変数	*/
		private int width = 1920;
		private int height = 1080;
		private int fps = 60;
		private Texture2D cap_tex;	// カメラから取得したままのテクスチャ
		private Texture2D out_tex;	// Planeに出力させるテクスチャ
		private WebCamTexture webCamTexture;
		private Color32[] colors = null;

		/* 使用するカスケードファイルを覚える	*/
		private CascadeClassifier handCascade;
		#endregion

		#region 調整可能な変数、定数
		/* 使用するカスケードファイルのパス	*/
		private const string CASCADE_FILE_PATH = CASCADE_FILE_PATH2;

		/* HSVのパラメータは(色相、彩度、明度)	*/
		private readonly static Scalar SKIN_LOWER = new Scalar(130, 10, 60);	// 肌色っぽい色と認識するための下限の色
		private readonly static Scalar SKIN_UPPER = new Scalar(255, 50, 160);	// 肌色っぽい色と認識するための上限の色

		/* 手を検出した際に表示する矩形のパラメータ	*/
		private readonly static Scalar RECT_COLOR = new Scalar(0, 255, 255);	// 色
		private const int RECT_THICKNESS = 2;   // 太さ

		/* 画像処理するときのパラメータ	*/
		private readonly static Size BLUR_SIZE = new Size(30, 30);	// 平均化するときのサイズ
		private const int DILATE_NUM = 10;	// 膨張処理をかける回数

		#endregion

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
			/* カスケードファイルを読み込ませる	*/
			string handCascadePath = Application.streamingAssetsPath + CASCADE_FILE_PATH;
			handCascade = new CascadeClassifier(handCascadePath);

			/* もろもろ初期化	*/
			WebCamDevice[] devices = WebCamTexture.devices;
			webCamTexture = new WebCamTexture(devices[0].name, this.width, this.height, this.fps);
			webCamTexture.Play();

			StartCoroutine(Init());	// カメラが起動するまで待たせる
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

				/* 手を検出	*/
				out_tex = detectHand(cap_tex);

				/* Planeに処理した画像を出力	*/
				GetComponent<Renderer>().material.mainTexture = out_tex;
			}
		}

		/*
		 * 手検出処理
		 */
		private Texture2D detectHand(Texture2D tex)
		{
			/* 出力用(最後にTexture2Dに戻す)	*/
			Mat retMat = Unity.TextureToMat(tex);

			/* TextureをMatに変換	*/
			Mat defaultMat = Unity.TextureToMat(tex);

			/* 肌色っぽい色だけ残るようマスクをかける(誤検出を減らすため)	*/
			Mat maskedMat = onlySkinColor(defaultMat);

			/* グレースケールに変換(カスケード分類器にかけるため)	*/
			Mat grayMat = maskedMat.CvtColor(ColorConversionCodes.BGR2GRAY);

			/* ヒストグラムに変換(カスケード分類器にかけるため)	*/
			Mat histMat = grayMat.EqualizeHist();

			/* 手を検出(カスケード分類器にかける)	*/
			handObjectManager.NotifyStartDetectHand();	// 手の検出を開始することを通知
			Rect[] hands = handCascade.DetectMultiScale(histMat);	// 検出する

			int i = 0;	// 検出した手の番号をつけるため
			foreach (Rect hand in hands)
			{
				Cv2.Rectangle(retMat, new Rect(hand.X, hand.Y, hand.Width, hand.Height), RECT_COLOR, RECT_THICKNESS);	// 検出個所に円を描画
				handObjectManager.NotifyDetectedHand(i, hand.X, hand.Y);	// 検出したことを通知
				i++;
			}
			handObjectManager.NotifyEndDetectHand();	// 手の検出を終えたことを通知

			return Unity.MatToTexture(retMat);	// 手の検出を終えたテクスチャを返す
		}

		/*
		 * 肌色以外消す
		 */
		private Mat onlySkinColor(Mat inputMat)
		{
			Mat retMat = inputMat.Clone();// MatをコピーしたいときはCloneを使う(普通に=を使うと同じものを指してしまうっぽい)
			Mat orgMat = inputMat.Clone();

			/* HSVに変換(InRangeするときはHSVのほうがいいらしいので)	*/
			Cv2.CvtColor(retMat, retMat, ColorConversionCodes.BGR2HSV);
			Cv2.CvtColor(orgMat, orgMat, ColorConversionCodes.BGR2HSV);
			Cv2.ExtractChannel(orgMat, retMat, 0);// これやらないとうまく肌色を検出しない

			/* 平均化	*/
			retMat.Blur(BLUR_SIZE);

			/* マスク生成(retMatに対してSKIN_LOWER~SKIN_UPPERの色だけを抽出)	*/
			Cv2.InRange(retMat, SKIN_LOWER, SKIN_UPPER, retMat);
			
			/* 膨張処理	*/
			Cv2.Dilate(retMat, retMat, new Mat(), new Point2d(-1,-1),DILATE_NUM);// 第3、4引数はデフォルト値。第5引数は膨張を掛ける回数
			
			/* グレースケールできる形式に戻す	*/
			Cv2.CvtColor(retMat, retMat, ColorConversionCodes.GRAY2BGR);

			/* マスクかける	*/
			Cv2.BitwiseAnd(inputMat, retMat, retMat);

			return retMat;
		}
	}
}