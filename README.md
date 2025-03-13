# css-sample
c# 샘플코드

1. BaseStatusBarHandler: 재화 UI를 담당하는 추상클래스. 기본 연출 코드가 포함되어있음.
    - CoinStatusBarHandler, HeartStatusBarHandler 등 다양한 재화 클래스로 상속하여 사용
    - 프리팹도 기본 ui 프리팹을 만들어 놓고, 상속받은 재화 클래스마다 각각 Prefab Variant를 만들어 사용
    - DOTween, UniTask 사용, CancellationToken을 이용한 연출 중 취소 처리
  
2. CsvTableGenerator: Csv 데이터 테이블을 기반으로 데이터 클래스 스크립트를 생성하는 코드입니다.

3. FileVersionHandling: 특정 파일을 네트워크를 통해 업데이트를 받아야 할 때, 버전 비교를 하여 패치가 필요한 파일 목록을 반환하는 함수
    - 버전 비교, 테스트 유저 구분 등 많은 조건 분기
    - 로컬 함수 사용
    - 튜플 사용
    - Nullable타입 사용
    - IEnumerable<T> 함수 반환형 사용
    - Linq(LastOrDefault) 사용

4. JsonParse: json 문자열을 Newtonsoft.Json.Linq 를 사용하여 Deserialize하는 과정을 담은 코드

5. PlayerPrefsHelper
    - 시간을 Timestamp 로 저장
    - 유저가 특정 기간 내에 어떤 행동을 했는지 여부 등 날짜와 관련된 유틸리티 함수들
    - PlayerPrefs.Save 함수가 동시에 여러번 호출되면 부하가 크기 때문에, Throttle 방식으로 최적화 (SavePlayerPrefsThrottleAction)

6. UIRewardGainAnimation
   아래 획득 연출에 대한 코드 입니다.
   ![Movie_051](https://github.com/user-attachments/assets/2bbad317-3124-4b18-8e2e-44de36ed28c2)

7. TextSpriteUtilityWindow
   여러 텍스쳐를 하나로 합치고, Multiple 타입 스프라이트로 임포트 하는 유틸리티 윈도우 입니다.


