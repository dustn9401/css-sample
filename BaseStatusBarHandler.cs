using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 상단 재화 UI 기본 클래스
/// </summary>
public abstract class BaseStatusBarHandler : CustomUIBehaviour {
    [SerializeField] protected CustomButton btn;
    [SerializeField] protected Transform iconTf;
    [SerializeField] protected TMP_Text countText;
    [SerializeField] protected GameObject plusIcon;
    [SerializeField] private Transform scaleAnimationTf; //획득연출 시 스케일업다운 하는 트랜스폼
    
    public virtual string ItemId { get; protected set; }
    protected bool IsInteractable => btn && btn.interactable;

    public Transform GetIconTransform() {
        return iconTf;
    }

    public TMP_Text GetCountText() {
        return countText;
    }

    public virtual void SetInteractable(bool interactable) {
        btn.interactable = interactable;
        TogglePlusIcon(interactable);
    }

    public virtual void TogglePlusIcon(bool isOn) {
        if (plusIcon) plusIcon.SetActive(isOn);
    }
    
    public void SetCountText(int count) {
        countText.text = count.ToString("D", CultureInfo.InvariantCulture);
    }

    protected const string CoinSoundName = "common_getcoin";
    protected const string ItemSoundName = "common_getitem";
    
    protected abstract string SoundName { get; }

    /// <summary>
    /// 현재 카운트 텍스트에 표시된 개수에서 wallet에 저장된 값 까지 획득 연출을 한다. 
    /// </summary>
    /// <param name="startTf"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="afterDelay"></param>
    /// <param name="reThrow"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public UniTask AnimateGain(Transform startTf, CancellationToken cancellationToken = default, int afterDelay = 1500, bool reThrow = false) {
        if (string.IsNullOrEmpty(ItemId)) throw new Exception("ItemId == null!!");
        return AnimateGain(startTf, Wallet.GetItemCount(ItemId), cancellationToken, afterDelay, reThrow);
    }
    
    /// <summary>
    /// 현재 카운트 텍스트에 표시된 개수에서 wallet에 저장된 값 까지 획득 연출을 한다. 
    /// </summary>
    /// <param name="startWorldPosition"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="afterDelay"></param>
    /// <param name="reThrow"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public UniTask AnimateGain(Vector3 startWorldPosition, CancellationToken cancellationToken = default, int afterDelay = 1500, bool reThrow = false) {
        if (string.IsNullOrEmpty(ItemId)) throw new Exception("ItemId == null!!");
        return AnimateGain(startWorldPosition, Wallet.GetItemCount(ItemId), cancellationToken, afterDelay, reThrow);
    }

    /// <summary>
    /// 현재 카운트 텍스트에 표시된 개수에서 endValue까지 획득 연출을 한다.
    /// </summary>
    /// <param name="startTf"></param>
    /// <param name="endValue"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="afterDelay"></param>
    /// <param name="reThrow"></param>
    public UniTask AnimateGain(Transform startTf, int endValue, CancellationToken cancellationToken = default, int afterDelay = 1500, bool reThrow = false) {
        return AnimateGain(startTf.position, endValue, cancellationToken, afterDelay, reThrow);
    }
    
    public async UniTask AnimateGain(Vector3 startWorldPosition, int endValue, CancellationToken cancellationToken = default, int afterDelay = 1500, bool reThrow = false) {
        if (!int.TryParse(countText.text, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var startValue))
            startValue = 0;
                
        var seq = DOTween.Sequence();
        
        var path = new Vector3[3];
        var flyCount = Mathf.Clamp(endValue - startValue, 1, 5);
        var gainedItems = new List<Transform>(flyCount);

        seq.Insert(1f, DOTween.To(() => startValue, SetCountText, endValue, .5f));

        for (var i = 0; i < flyCount; i++) {
            var gainedItem = Instantiate(iconTf, transform);
            gainedItems.Add(gainedItem);
            gainedItem.position = startWorldPosition;
            path[0] = startWorldPosition;
            path[2] = iconTf.position;
            path[1] = new Vector3((path[0].x + path[2].x) / 2f - 2F, (path[0].y + path[2].y) / 2f);
            var _i = i;
            seq.Insert(.1f * i,
                DOTween.Sequence()
                    .Append(gainedItem.DOPath(path, 1f, PathType.CatmullRom))
                    .Join(gainedItem.DOScale(iconTf.localScale, 1f))
                    .OnComplete(() => {
                        if (!string.IsNullOrEmpty(SoundName)) {
                            SoundWrapper.Play(SoundName);
                        }

                        gainedItem.gameObject.SetActive(false);
                    }));
        }

        try {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.GetCancellationTokenOnDestroy());
            await seq.WaitForCompletionAsync(cancellationToken: cts.Token, killWithComplete: false);
            await UniTask.Delay(afterDelay, cancellationToken: cts.Token);
        }
        catch (OperationCanceledException) {
            Debug.Log("AnimateGain() Canceled");
            if (reThrow) throw;
        }
        finally {
            foreach (var g in gainedItems) Destroy(g.gameObject);
        }
    }
    
    public void ShowGainEffect() {
        SoundWrapper.Play(CoinSoundName);
        var scaleTf = scaleAnimationTf ? scaleAnimationTf : RectTf;
        if (DOTween.IsTweening(scaleTf)) DOTween.Kill(scaleTf);
        scaleTf.CustomPunchScale(Vector3.one, force: .5f).SetId(scaleTf);
        var eff = EffectHandler.CreateEffect("CoinEnd", RectTf);
        eff.transform.position = iconTf.position;
    }
}
