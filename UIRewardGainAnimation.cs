using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Logic;
using TMPro;
using UnityEngine;
using Utils;

namespace UI
{
	/// <summary>
	/// 화면 중앙에서 등장하는 코인/보석 더미 획득 연출
	/// </summary>
	public class UIRewardGainAnimation : MonoBehaviour
	{
		[SerializeField] private TMP_Text countText;
		[SerializeField] private Transform itemsParent;

		[Header("Scale param")]
		[SerializeField] private float scaleDuration = .5f;
		[SerializeField] private float maxDelay = .5f;
		
		[Header("Move param")]
		[SerializeField] private float delayBeforeMove;
		[SerializeField] private float moveInterval = 0.05f;
		[SerializeField] private float moveDuration = 1f;

		[Header("전체 배속")] 
		[SerializeField] private float timeScale = 1f;

		[Header("Sprites")] 
		[SerializeField] private Sprite sCoin;
		[SerializeField] private Sprite sDia;
		[SerializeField] private Sprite sQuestPoint;

		private Transform[] itemTransforms;
		private Vector3[] initialPositions;
		private SpriteRenderer[] itemSprites;

		private void OnDestroy()
		{
			itemTransforms = null;
			initialPositions = null;
			itemSprites = null;
		}

		private void CheckInitialize()
		{
			if (itemTransforms is { Length: > 0 })
			{
				return;
			}
			
			itemTransforms = Enumerable.Range(0, itemsParent.childCount).Select(x => itemsParent.GetChild(x)).ToArray();
			initialPositions = itemTransforms.Select(x => x.localPosition).ToArray();
			itemSprites = itemTransforms.Select(x => x.GetComponentInChildren<SpriteRenderer>(true)).ToArray();
		}

		/// <summary>
		/// 연출 후 스스로 풀에 반납
		/// </summary>
		/// <param
		///  name="itemKey">
		/// </param>
		/// <param name="count"></param>
		/// <param name="destPos"></param>
		/// <param name="onArrived"></param>
		public async UniTask ShowAsync(EItemKey itemKey, int count, Vector3 destPos, Action onArrived = null)
		{
			try
			{
				CheckInitialize();

				countText.text = $"+{count}";
				
				// count animation
				var seq = DOTween.Sequence().SetId(countText)
					.Append(countText.transform.DOLocalMoveY(2f, 2f).From(0f).SetEase(Ease.Linear))
					.Join(DOTween.Sequence()
						.Append(countText.DOFade(1f, .5f).From(0f))
						.AppendInterval(.5f)
						.Append(countText.DOFade(0f, .5f)));
				seq.timeScale = timeScale;

				var tasks = new List<UniTask>();
				
				// scale animation
				var highDelay = 0f;
				for (var i = 0; i < itemTransforms.Length; i++)
				{
					var tr = itemTransforms[i];
					tr.gameObject.SetActive(true);
					tr.localPosition = initialPositions[i];

					itemSprites[i].sprite = itemKey switch
					{
						EItemKey.Coin => sCoin,
						EItemKey.Diamond => sDia,
						EItemKey.QuestPoint => sQuestPoint,
						_ => throw new ArgumentOutOfRangeException(nameof(itemKey), itemKey, null)
					};

					var randDelay = UnityEngine.Random.Range(0f, maxDelay);
					if (highDelay < randDelay) highDelay = randDelay;
					var tween = tr.DOScale(1f, scaleDuration).From(0f).SetEase(Ease.OutBack).SetDelay(randDelay);
					tween.timeScale = timeScale;
					tasks.Add(tween.WaitForCompletionAsync(this.GetCancellationTokenOnDestroy(), killWithComplete: false, reThrow: false));
				}

				await UniTask.WhenAll(tasks);
				tasks.Clear();
				
				await UniTask.WaitForSeconds((delayBeforeMove / timeScale), cancellationToken: this.GetCancellationTokenOnDestroy());

				// move animation
				for (var i = 0; i < itemTransforms.Length; i++)
				{
					var tr = itemTransforms[i];
					var tween = tr.DOMove(destPos, moveDuration)
						.SetEase(Ease.InBack)
						.SetDelay(i * moveInterval)
						.OnComplete(() =>
						{
							// TODO: 도착 시 사운드, 파티클 등
							tr.gameObject.SetActive(false);
							onArrived?.Invoke();
						});
					tween.timeScale = timeScale;
					tasks.Add(tween.WaitForCompletionAsync(this.GetCancellationTokenOnDestroy(), killWithComplete: false, reThrow: false));
				}

				await UniTask.WhenAll(tasks);
			}
			catch (OperationCanceledException)
			{
				
			}
			catch (Exception e)
			{
				Debug.LogException(e, this);
			}
			
			ObjectPoolManager.Instance.Release(gameObject);
		}
	}
}