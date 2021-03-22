using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandObjectManager : MonoBehaviour
{
	public GameObject handObjectPrefab;

	private const float ERROR_MARGIN = 1;// 移動したと判定するかの許容誤差

	private List<Vector3> detectedHandPositions = new List<Vector3>();
	private List<Vector3> generatedHandPositions = new List<Vector3>();
	private List<GameObject> handObjects = new List<GameObject>();

	/*
	 * 初期化処理
	 */
	private void Start()
	{
		/* 値の初期化	*/
		detectedHandPositions.Clear();
		generatedHandPositions.Clear();
		handObjects.Clear();
	}

	/*
	 * 手検出開始通知
	 */
	public void NotifyStartDetectHand()
	{
		detectedHandPositions.Clear();
	}

	/*
	 * 手検出通知
	 */
	public void NotifyDetectedHand(int handId, int posX, int posY)
	{
		/* 座標変換処理(retMatの1280x720⇒world座標)	*/
		float convertedPosX = (posX * (1000.0f / 1280)) - 500;// Planeの左端のx座標が-500、右端のx座標が500なので。
		float convertedPosY = -(posY * (1000.0f / 720)) + 500;// Planeの下端のx座標が-500、上端のx座標が500なので。

		/* 検出した座標をストックする	*/
		detectedHandPositions.Add(new Vector3(convertedPosX, convertedPosY, 0));
	}

	/*
	 * 手検出終了通知
	 */
	public void NotifyEndDetectHand()
	{
		ManageHandObject();
	}

	/*
	 * HandObject管理処理
	 */
	private void ManageHandObject()
	{
		/* 検出されたとき	*/
		if(detectedHandPositions.Count > 0)
		{
			/* すでにHandObjectを生成しているとき	*/
			if (handObjects.Count > 0)
			{
				UpdateHandObject();// 移動
				AddHandObject();// 追加
			}
			/* まだHandObjectを生成してないとき(=新たに検出された)	*/
			else
			{
				AddHandObject();
			}
		}
		/* 何も検出されなかったとき	*/
		else
		{
			/* すでにHandObjectを生成しているとき(=消えた)	*/
			if (handObjects.Count > 0)
			{
				AllDeleteHandObject();// 削除
			}
			/* まだHandObjectを生成してないとき	*/
			else
			{
				// 何もしなくていい
			}
		}
	}

	/*
	 * 座標更新処理
	 */
	private void UpdateHandObject()
	{
		/* 移動したHnadObjectの座標を更新する	*/
		for (int i= handObjects.Count-1; i>=0; i--)
		{
			bool isMoved = false;// 移動したか

			for (int j=detectedHandPositions.Count-1; j>=0; j--)
			{
				float deltaPosX = handObjects[i].transform.position.x - detectedHandPositions[j].x;
				float deltaPosY = handObjects[i].transform.position.y - detectedHandPositions[j].y;

				/* 移動したなら	*/
				if ((Mathf.Abs(deltaPosX) < ERROR_MARGIN) && (Mathf.Abs(deltaPosY) < ERROR_MARGIN))
				{
					/* 座標を更新する	*/
					handObjects[i].GetComponent<Rigidbody>().MovePosition(detectedHandPositions[j]);
					detectedHandPositions.RemoveAt(j);

					isMoved = true;
					break;
				}
			}

			/* 移動したと判定できないなら(=消えた)	*/
			if(!isMoved)
			{
				Destroy(handObjects[i]);
				handObjects.RemoveAt(i);
			}
		}
	}

	/*
	 * HandObject追加処理
	 */
	private void AddHandObject()
	{
		/* 検出された数だけ追加する	*/
		for (int i=detectedHandPositions.Count-1; i>=0; i--)
		{
			handObjects.Add(CreateHandObject(detectedHandPositions[i]));
			detectedHandPositions.RemoveAt(i);
		}
	}

	/*
	 * 削除(何も検出されなかったとき)
	 */
	private void AllDeleteHandObject()
	{
		for(int i=handObjects.Count-1; i>=0; i--)
		{
			Destroy(handObjects[i]);
			handObjects.RemoveAt(i);
		}
	}

	/*
	 * HandObject生成処理
	 */
	private GameObject CreateHandObject(Vector3 createPos)
	{
		return Instantiate(handObjectPrefab, createPos, Quaternion.identity);
	}
}
